using EntityStates;
using JetBrains.Annotations;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AmmoLocker
{
    public class AmmoLockerInteraction : NetworkBehaviour, IInteractable, IDisplayNameProvider
    {
        private void Awake()
        {
            foreach (var collider in gameObject.GetComponentsInChildren<Collider>())
            {
                var entityLocator = collider.gameObject.GetComponent<EntityLocator>() ?? collider.gameObject.AddComponent<EntityLocator>();
                entityLocator.entity = gameObject;
            }
        }

        [Server]
        public void SetCharacterBodies(CharacterBody owner, CharacterBody body)
        {
            Log.LogDebug(string.Format("SetCharacterBodies {0}, {1}", owner, body));
            characterBodyNetId = body.netId;
            RpcSetPortrait(owner.netId, body.netId);
        }


        public static CharacterBody GetCharacterBody(NetworkInstanceId netId)
        {
            return ClientScene.FindLocalObject(netId)?.GetComponent<CharacterBody>();
        }


        [ClientRpc]
        public void RpcSetPortrait(NetworkInstanceId ownerNetId, NetworkInstanceId characterNetId)
        {
            Log.LogDebug(string.Format("RpcSetPortrait {0}, {1}", ownerNetId, characterNetId));
            var ownerBody = GetCharacterBody(ownerNetId);
            var skinTexture = SkinCatalog.FindCurrentSkinDefForBodyInstance(ownerBody.gameObject)?.icon?.ToTexture2D(AmmoLocker.skinSwatches);
            var body = GetCharacterBody(characterNetId);
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                Log.LogDebug(string.Format("Renderer on {0}", renderer.gameObject));
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    Log.LogDebug(string.Format("Material {0}", material.name));
                    if (body.portraitIcon != null && material.name.StartsWith("Portrait"))
                    {
                        Log.LogDebug(string.Format("Setting portrait mainTexture to {0}", body.portraitIcon));
                        material.mainTexture = body.portraitIcon;
                    } else if (skinTexture != null && material.name.StartsWith("Skin"))
                    {
                        Log.LogDebug(string.Format("Setting skin mainTexture to {0}", skinTexture));
                        material.mainTexture = skinTexture;
                    }
                }
                renderer.materials = materials;
            }
        }

        public string GetContextString([NotNull] Interactor activator)
        {
            return Language.GetString("CHEESEBOARD_AMMOLOCKER_CONTEXT");
        }

        public Interactability GetInteractability([NotNull] Interactor activator)
        {
            if (available && activator.GetComponent<CharacterBody>()?.netId == characterBodyNetId)
            {
                return Interactability.Available;
            }
            return Interactability.Disabled;
        }

        public void OnInteractionBegin([NotNull] Interactor activator)
        {
            if (available)
            {
                var characterBody = activator.GetComponent<CharacterBody>();
                if (characterBody != null)
                {
                    available = false;

                    characterBody.AddTimedBuffAuthority(AmmoLocker.overchargeDef.buffIndex, AmmoLocker.ammoLockerBuffDuration);
                    characterBody.AddTimedBuffAuthority(AmmoLocker.shoringDef.buffIndex, AmmoLocker.ammoLockerBuffDuration);
                    characterBody.healthComponent.AddBarrierAuthority(characterBody.healthComponent.fullBarrier * AmmoLocker.ammoLockerBarrier);

                    RpcOnInteraction(characterBody.netId);
                }
            }
        }

        [ClientRpc]
        public void RpcOnInteraction(NetworkInstanceId bodyId)
        {
            Log.LogDebug("RpcOnInteraction");
            var skillLocator = GetCharacterBody(bodyId)?.skillLocator;
            if (skillLocator != null)
            {
                foreach (var skill in skillLocator.allSkills)
                {
                    skill.Reset();
                }
            }
            gameObject.GetComponentInChildren<Animator>().Play("Base Layer.Opening");
        }

        public bool ShouldIgnoreSpherecastForInteractibility([NotNull] Interactor activator)
        {
            return false;
        }

        public bool ShouldShowOnScanner()
        {
            return false; 
        }

        public string GetDisplayName()
        {
            return Language.GetString("CHEESEBOARD_AMMOLOCKER_NAME");
        }

        [SyncVar]
        public bool available = true;

        [SyncVar]
        public NetworkInstanceId characterBodyNetId;
    }
}
