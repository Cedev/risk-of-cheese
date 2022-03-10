using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AmmoLocker
{
    public class PluginContentPack : IContentPackProvider
    {
        public ContentPack contentPack;
        private Func<ContentPack, IProgress, Task> loadContent;

        public PluginContentPack(string identifier, Func<ContentPack, IProgress, Task> loadContent)
        {
            this.identifier = identifier;
            this.contentPack = new ContentPack();
            this.loadContent = loadContent;
            RoR2.ContentManagement.ContentManager.collectContentPackProviders += addContentPack => addContentPack(this);
        }

        public string identifier {get; set;}

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break; 
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            contentPack.identifier = identifier;
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            var progress = new Progress();
            progress.OnProgress += x => args.ReportProgress((float) x);
            var task = Task.Run(async () => await loadContent(contentPack, progress));
            while (!task.IsCompleted) { yield return null; }
            // Raising an exception here hardlock crashes the game
            if (task.IsFaulted)
            {
                Log.LogError(task.Exception);
            }
            yield break;
        }
    }

    
    public class UnityTask<T> : INotifyCompletion where T : AsyncOperation
    {
        private readonly T unityAsyncOperation;
        public UnityTask(T unityAsyncOperation)
        {
            this.unityAsyncOperation = unityAsyncOperation;
        }

        public bool IsCompleted { get { return unityAsyncOperation.isDone; } }      

        public T GetResult()
        {
            return unityAsyncOperation;
        }

        public void OnCompleted(Action continuation)
        {
            unityAsyncOperation.completed += _ => continuation();
        }

        public UnityTask<T> GetAwaiter()
        {
            return this;
        }
    }

    public static class PluginContent
    {
        public static void LoadContentAsync(string identifier, Func<ContentPack, IProgress, Task> loadContent)
        {
            new PluginContentPack(identifier, loadContent);

        }

        public static void Add<T>(this NamedAssetCollection<T> assetCollection, params T[] items)
        {
            assetCollection.Add(items);
        }


        public static Task<T> LoadAsync<T>(string key)
        {
            Log.LogInfo(string.Format("Loading addressable asset {0}", key));
            var handle = Addressables.LoadAssetAsync<T>(key);
            return handle.Task;
        }

        public static Task<T> LoadLegacyAsync<T>(string name)
        {
            Log.LogInfo(string.Format("Loading legacy asset {0}", name));
            var key = RoR2.LegacyResourcesAPI.GetPathGuidString(name);
            return LoadAsync<T>(key);
        }

        public static UnityTask<T> Awaitable<T>(this T asyncOperation) where T : AsyncOperation
        {
            return new UnityTask<T>(asyncOperation);
        }

        public static async Task<T> LoadAsync<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
        {
            Log.LogInfo(string.Format("Loading {0} from bundle", name));
            var abr = await bundle.LoadAssetAsync(name).Awaitable();
            Log.LogInfo(string.Format("Loaded {0} from bundle: {1}", name, abr.asset));
            return (T) abr.asset;
        }

        public static async Task<AssetBundle> LoadAssetBundle(string path)
        {
            Log.LogInfo(string.Format("Loading AssetBundle {0}", path));
            var request = await AssetBundle.LoadFromFileAsync(path).Awaitable();
            Log.LogInfo(string.Format("Loaded AssetBundle {0}: {1}", path, request.assetBundle));
            return request.assetBundle;
        }

        public static Sprite ToSprite(this Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        public static int Round(this float x)
        {
            return (int) Math.Round(x);
        }

        public static Texture2D ToTexture2D(this Sprite sprite, params Texture2D[] fallbackSources)
        {
            if (sprite.rect.x.Round() == 0 &&
                sprite.rect.y.Round() == 0 &&
                sprite.rect.width.Round() == sprite.texture.width &&
                sprite.rect.height.Round() == sprite.texture.height
            )
            {
                return sprite.texture;
            }
            Texture2D texture = new Texture2D(sprite.rect.width.Round(), sprite.rect.height.Round());
            texture.name = sprite.name;
            var sources = new List<Texture2D>() { sprite.texture };
            sources.AddRange(fallbackSources);
            foreach (var source in sources) {
                try
                {
                    Color[] pixels = source.GetPixels(
                        sprite.textureRect.x.Round(),
                        sprite.textureRect.y.Round(),
                        sprite.textureRect.width.Round(),
                        sprite.textureRect.height.Round());
                    texture.SetPixels(pixels);
                    texture.Apply();
                    return texture;
                } catch (UnityException e)
                {
                    // Texture 'texSkinSwatches' is not readable, the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings.
                    Log.LogDebug(e);
                }
            }
            return null;
        }
    }
}
