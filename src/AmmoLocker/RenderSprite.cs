using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

namespace AmmoLocker
{
    public class RenderWaiter : MonoBehaviour
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        public void OnPostRender()
        {
            semaphore.Release();
        }

        public async Task Wait()
        {
            await semaphore.WaitAsync();
        }
    }

    public static class RenderSprite
    {
        private static int position;

        public static async Task<RenderTexture> Render(this Sprite sprite)
        {
            int width = sprite.rect.width.Round();
            int height = sprite.rect.height.Round();

            var offset = Interlocked.Add(ref position, width + 10) - width / 2;

            var gameObject = new GameObject();
            try
            {
                gameObject.layer = 31;
                gameObject.transform.position = Vector3.left * offset;
                var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.transform.localScale = Vector3.one * sprite.pixelsPerUnit;

                var camera = gameObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 31;
                camera.orthographic = true;
                camera.aspect = spriteRenderer.bounds.size.x / spriteRenderer.bounds.size.y;
                camera.orthographicSize = spriteRenderer.bounds.size.y / 2;
                camera.transform.position = spriteRenderer.bounds.center + Vector3.forward;
                camera.transform.LookAt(spriteRenderer.bounds.center, Vector3.up);

                var renderTexture = new RenderTexture(width, height, 0);
                camera.targetTexture = renderTexture;

                var renderWaiter = gameObject.AddComponent<RenderWaiter>();
                camera.Render();
                await renderWaiter.Wait();

                return renderTexture;

             } finally {
                
                UnityEngine.Object.DestroyImmediate(gameObject); 
            }
        }
    }
}
