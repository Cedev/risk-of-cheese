using BepInEx;
using RoR2;
using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections.Generic;
using On.RoR2.Artifacts;

namespace VoidSacrifice
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class VoidSacrificePlugin : BaseUnityPlugin
	{
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Cedev";
        public const string PluginName = "VoidSacrifice";
        public const string PluginVersion = "1.0.0";

        private static PickupDropTable voidInfestedDropTable;

        public void Awake()
        {
            Log.Init(Logger);

            // BasicPickupDropTable GUID: 6e8e74923113aa840a2940b655b1b770
            var defaultDropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            defaultDropTable.tier1Weight = 7;
            defaultDropTable.tier2Weight = 3;
            defaultDropTable.tier3Weight = 0.1f;
            defaultDropTable.equipmentWeight = 1;
            defaultDropTable.voidTier1Weight = 6;
            defaultDropTable.voidTier2Weight = 3;
            defaultDropTable.voidTier3Weight = 1;
            voidInfestedDropTable = Config.LoadDropTable("Void Infested Drop Table", "voidInfestedDropTable", defaultDropTable);

            On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath += OnServerCharacterDeath;
        }

        private void OnServerCharacterDeath(SacrificeArtifactManager.orig_OnServerCharacterDeath orig, DamageReport damageReport)
        {
            Log.LogInfo(string.Format("Character death {0} on team {1}", damageReport.victimBody?.name, damageReport.victimTeamIndex));
            var originalDropTable = RoR2.Artifacts.SacrificeArtifactManager.dropTable;
            try
            {
                if (damageReport.victimTeamIndex == TeamIndex.Void)
                {
                    RoR2.Artifacts.SacrificeArtifactManager.dropTable = voidInfestedDropTable;
                }
                orig(damageReport);
            }
            finally
            {
                RoR2.Artifacts.SacrificeArtifactManager.dropTable = originalDropTable;
            }
        }
    }
}

