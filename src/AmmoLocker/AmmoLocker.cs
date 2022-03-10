using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Linq.Expressions;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using RoR2.ContentManagement;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using UnityEngine.Networking;
using BepInEx.Configuration;
using UnityEngine.Events;

namespace AmmoLocker
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 3 modules from R2API: ItemAPI to add our item, ItemDropAPI to have our item drop ingame, and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(
        nameof(DeployableAPI),
        nameof(LanguageAPI)
        )]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class AmmoLocker : BaseUnityPlugin
	{
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Cedev";
        public const string PluginName = "AmmoLocker";
        public const string PluginVersion = "1.0.0";

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static BuffDef shoringDef;
        public static BuffDef overchargeDef;
        public static EquipmentDef ammoLockerDef;
        public static DeployableSlot ammoLockerSlot;
        public static float ammoLockerCooldown;
        public static float ammoLockerBuffDuration;
        public static float ammoLockerRange;
        public static float ammoLockerBarrier;
        public static float ammoLockerScale;
        public static float ammoLockerSpacing;
        public static int ammoLockerMaxDeployed;
        public static int ammoLockerMaxLockers;
        public static GameObject ammoLockerPrefab;
        public static Texture2D defaultSkinSwatch;
        public static Texture2D skinSwatches;
        public static Texture2D mysteryIcon;
        public static System.Random random;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            ammoLockerCooldown = Config.Bind("Ammo Locker", "ammoLockerCooldown", 60f).Value;
            ammoLockerBuffDuration = Config.Bind("Ammo Locker", "ammoLockerBuffDuration", 20f).Value;
            ammoLockerRange = Config.Bind("Ammo Locker", "ammoLockerRange", 1000f).Value;
            ammoLockerBarrier = Config.Bind("Ammo Locker", "ammoLockerBarrier", 0.25f).Value;
            ammoLockerSpacing = Config.Bind("Ammo Locker", "ammoLockerSpacing", 1f).Value;
            ammoLockerScale = Config.Bind("Ammo Locker", "ammoLockerScale", 1.05f).Value;
            ammoLockerMaxDeployed = Config.Bind("Ammo Locker", "ammoLockerMaxDeployed", 4).Value;
            ammoLockerMaxLockers = Config.Bind("Ammo Locker", "ammoLockerMaxLockers", 8).Value;

            PluginContent.LoadContentAsync(PluginGUID, async (contentPack, progress) =>
            {

                var assetBundle = progress.Step(PluginContent.LoadAssetBundle(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "assets")));

                var texDoubleMag = progress.Step(PluginContent.LoadLegacyAsync<Sprite>("Textures/ItemIcons/texDoubleMagIcon"));
                var prefabDoubleMag = progress.Step(PluginContent.LoadLegacyAsync<GameObject>("Prefabs/PickupModels/PickupDoubleMag"));


                var texMysteryIcon = progress.After(assetBundle, ab => ab.LoadAsync<Texture2D>("Assets/Icons/texMysteryIcon.png"));
                var texShoringBuff = progress.After(assetBundle, ab => ab.LoadAsync<Texture2D>("Assets/Icons/texBuffShoring.png"));
                var texOverchargeBuff = progress.After(assetBundle, ab => ab.LoadAsync<Texture2D>("Assets/Icons/texBuffOvercharge.png"));
                var ammoLockerPrefabStep = progress.After(assetBundle, ab => ab.LoadAsync<GameObject>("Assets/Prefabs/locker.prefab"));
                var texDefaultSkin = progress.After(assetBundle, ab => ab.LoadAsync<Texture2D>("Assets/Icons/texDefaultSkin.png"));
                var texSkinSwatches = progress.After(assetBundle, ab => ab.LoadAsync<Texture2D>("Assets/Icons/texSkinSwatches.png"));

                mysteryIcon = await texMysteryIcon;

                shoringDef = ScriptableObject.CreateInstance<BuffDef>();
                shoringDef.name = "CHEEESYBUFFS_SHORING_NAME";
                shoringDef.canStack = false;
                shoringDef.iconSprite = (await texShoringBuff).ToSprite();
                contentPack.buffDefs.Add(shoringDef);

                overchargeDef = ScriptableObject.CreateInstance<BuffDef>();
                overchargeDef.name = "CHEEESYBUFFS_OVERCHARGE_NAME";
                overchargeDef.canStack = false;
                overchargeDef.iconSprite = (await texOverchargeBuff).ToSprite();
                contentPack.buffDefs.Add(overchargeDef);

                ammoLockerDef = ScriptableObject.CreateInstance<EquipmentDef>();
                ammoLockerDef.name = "CHEESEBOARD_AMMOLOCKER_EQUIPMENT_NAME";
                ammoLockerDef.nameToken = ammoLockerDef.name;
                ammoLockerDef.pickupToken = "CHEESEBOARD_AMMOLOCKER_EQUIPMENT_PICKUP";
                ammoLockerDef.descriptionToken = "CHEESEBOARD_AMMOLOCKER_EQUIPMENT_DESC";
                ammoLockerDef.loreToken = "CHEESEBOARD_AMMOLOCKER_EQUIPMENT_LORE";
                ammoLockerDef.pickupIconSprite = await texDoubleMag;
                ammoLockerDef.pickupModelPrefab = await prefabDoubleMag;
                ammoLockerDef.canDrop = true;
                ammoLockerDef.cooldown = ammoLockerCooldown;
                contentPack.equipmentDefs.Add(ammoLockerDef);

                ammoLockerPrefab = await ammoLockerPrefabStep;
                ammoLockerPrefab.AddComponent<AmmoLockerInteraction>();
                ammoLockerPrefab.AddComponent<InteractionProcFilter>();
                Log.LogInfo(string.Format("Ammo locker prefab NetworkInstance asset ID: {0}", ammoLockerPrefab.GetComponent<NetworkIdentity>().assetId));
                contentPack.networkedObjectPrefabs.Add(ammoLockerPrefab);

                defaultSkinSwatch = await texDefaultSkin;
                skinSwatches = await texSkinSwatches;
            });

            ammoLockerSlot = DeployableAPI.RegisterDeployableSlot((master, multiplier) => ammoLockerMaxDeployed);
            random = new System.Random();


            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self.HasBuff(shoringDef))
                {
                    self.barrierDecayRate = 0;
                }
            };

            On.RoR2.CharacterBody.OnBuffFirstStackGained += (orig, self, buff) =>
            {
                orig(self, buff);
                if (buff == overchargeDef)
                {
                    RecalulateSkillStocks(self);
                }
            };
            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buff) =>
            {
                orig(self, buff);
                if (buff == overchargeDef)
                {
                    RecalulateSkillStocks(self);
                }
            };

            On.RoR2.GenericSkill.RecalculateMaxStock += (orig, self) =>
            {
                orig(self);
                if (self.characterBody != null && self.characterBody.HasBuff(overchargeDef))
                {
                    self.maxStock = self.maxStock + Math.Max(1, self.rechargeStock);
                }
            };

            // Fix reload skills not using stock from generic skill
            On.RoR2.Skills.ReloadSkillDef.OnFixedUpdate += (orig, self, genericSkill) =>
            {
                Log.LogInfo(string.Format("Swapping {0} max stock for {1}", self.baseMaxStock, genericSkill.maxStock));
                var tmpBaseMaxStock = self.baseMaxStock;
                self.baseMaxStock = genericSkill.maxStock;
                orig(self, genericSkill);
                self.baseMaxStock = tmpBaseMaxStock;
            };

            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (equipmentDef == ammoLockerDef)
                {
                    FireAmmoLocker(self, equipmentDef);
                    return true;
                }
                else
                {
                    return orig(self, equipmentDef);
                }
            };

#if DEBUG
            Tracer.Instance.Format<GenericSkill>(gs => string.Format("{0} {{stacks: {1}/{2}}}", gs, gs.stock, gs.maxStock));
            Tracer.Instance.Filter<GenericSkill>(gs => gs.characterBody && gs.characterBody.isPlayerControlled);

            On.RoR2.GenericSkill.AddOneStock += LogStock<On.RoR2.GenericSkill.hook_AddOneStock>();
            On.RoR2.GenericSkill.ApplyAmmoPack += LogStock<On.RoR2.GenericSkill.hook_ApplyAmmoPack>();
            On.RoR2.GenericSkill.AssignSkill += LogStock<On.RoR2.GenericSkill.hook_AssignSkill>();
            On.RoR2.GenericSkill.DeductStock += LogStock<On.RoR2.GenericSkill.hook_DeductStock>();
            On.RoR2.GenericSkill.RemoveAllStocks += LogStock<On.RoR2.GenericSkill.hook_RemoveAllStocks>();
            On.RoR2.GenericSkill.Reset += LogStock<On.RoR2.GenericSkill.hook_Reset>();
            On.RoR2.GenericSkill.RestockContinuous += LogStock<On.RoR2.GenericSkill.hook_RestockContinuous>();
            On.RoR2.GenericSkill.RestockSteplike += LogStock<On.RoR2.GenericSkill.hook_RestockSteplike>();
            On.RoR2.GenericSkill.Start += LogStock<On.RoR2.GenericSkill.hook_Start>();
            On.RoR2.GenericSkill.OnExecute += LogStock<On.RoR2.GenericSkill.hook_OnExecute>();

            On.RoR2.Skills.SkillDef.OnExecute += LogStock<On.RoR2.Skills.SkillDef.hook_OnExecute>();

            On.RoR2.GenericSkill.RecalculateMaxStock += LogStock<On.RoR2.GenericSkill.hook_RecalculateMaxStock>();
#endif

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }
        private void RecalulateSkillStocks(CharacterBody self)
        {
            foreach (var skill in self.skillLocator.allSkills)
            {
                skill.RecalculateMaxStock();
            }
        }

        public GameObject MakeAmmoLockers(EquipmentSlot self, Vector3 position, Quaternion rotation)
        {
            var lockers = new GameObject();
            lockers.transform.position = position;
            lockers.transform.rotation = rotation;
            var lockersDeployable = lockers.AddComponent<Deployable>();
            lockersDeployable.onUndeploy = (lockersDeployable.onUndeploy ?? new UnityEvent());
            lockersDeployable.onUndeploy.AddListener(() => UnityEngine.Object.Destroy(lockers));

            self.characterBody.master.AddDeployable(lockersDeployable, ammoLockerSlot);

            var players = new List<CharacterBody>();
            var extras = new List<CharacterBody>();
            foreach (var teamMember in TeamComponent.GetTeamMembers(self.characterBody?.teamComponent?.teamIndex ?? TeamIndex.None)) {
                if (teamMember.body?.isPlayerControlled ?? false)
                {
                    players.Add(teamMember.body);
                } else if (teamMember.body)
                {
                    extras.Insert((int)(random.NextDouble() * (extras.Count + 1)), teamMember.body);
                }
            }
            players.AddRange(extras.Take(ammoLockerMaxLockers - players.Count));

            Log.LogInfo(string.Format("Making ammo lockers for {0} players", players.Count));
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i]; 
                var x = ammoLockerScale * ammoLockerSpacing * ((Vector3.right * (players.Count - 1)) / 2 + Vector3.left * i);
                var playerLocker = UnityEngine.Object.Instantiate<GameObject>(ammoLockerPrefab, position + rotation * x, rotation);
                playerLocker.transform.localScale = ammoLockerScale * Vector3.one;
                NetworkServer.Spawn(playerLocker);
                lockersDeployable.onUndeploy.AddListener(() => UnityEngine.Object.Destroy(playerLocker));
                var playerLockerInteraction = playerLocker.GetComponent<AmmoLockerInteraction>();
                playerLockerInteraction.SetCharacterBodies(self.characterBody, player);
            }

            return lockers;
        }

        public void FireAmmoLocker(EquipmentSlot self, EquipmentDef equipmentDef)
        {
            Log.LogInfo("Firing ammo locker");
            Ray aimRay = self.GetAimRay();
            RaycastHit raycastHit;
            if (Physics.Raycast(aimRay, out raycastHit, ammoLockerRange, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                Log.LogInfo("Making ammo lockers");
                MakeAmmoLockers(self, raycastHit.point, Util.QuaternionSafeLookRotation((self.characterBody.corePosition - raycastHit.point), raycastHit.normal));
            }
        }

#if DEBUG
        private T LogStock<T>()
        {
            return Tracing.Trace<T>(Tracer.Instance);
        }


        //The Update() method is run on every frame of the game.
        private void Update()
        {
            //This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Get the player body to use a position:	
                var body = PlayerCharacterMasterController.instances[0].master.GetBody();


                //Get the player body to use a position:	
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                //And then drop our defined item in front of the player.

                Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(ammoLockerDef.equipmentIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.Firework.itemIndex), transform.position, transform.forward * 30f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.Squid.itemIndex), transform.position, transform.forward * 40f);


            }   
        }
#endif
    }

    public static class Hooking<T>
    {

        public static Hook Get<V>(Expression<Func<T, V>> prop, Func<Func<T, V>, T, V> replacement)
        {
            return new Hook(Prop(prop).GetGetMethod(), replacement);
        }

        public static Hook Set<V>(Expression<Func<T, V>> prop, Action<Action<T, V>, T, V> replacement)
        {
            return new Hook(Prop(prop).GetSetMethod(), replacement);
        }

        public static PropertyInfo Prop<V>(Expression<Func<T,V>> prop)
        {
            return ((prop.Body as MemberExpression).Member as PropertyInfo);
        }
    }

}

