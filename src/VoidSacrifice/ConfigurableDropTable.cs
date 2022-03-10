using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;
using BepInEx.Configuration;

namespace VoidSacrifice
{
    public static class ConfigurableDropTable
    {
        public static BasicPickupDropTable LoadDropTable(this ConfigFile config, string section, string key, BasicPickupDropTable value)
        {
            var dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            dropTable.tier1Weight = config.Bind(section, key + "_tier1Weight", value.tier1Weight).Value;
            dropTable.tier2Weight = config.Bind(section, key + "_tier2Weight", value.tier2Weight).Value;
            dropTable.tier3Weight = config.Bind(section, key + "_tier3Weight", value.tier3Weight).Value;
            dropTable.bossWeight = config.Bind(section, key + "_bossWeight", value.bossWeight).Value;
            dropTable.lunarEquipmentWeight = config.Bind(section, key + "_lunarEquipmentWeight", value.lunarEquipmentWeight).Value;
            dropTable.lunarItemWeight = config.Bind(section, key + "_lunarItemWeight", value.lunarItemWeight).Value;
            dropTable.lunarCombinedWeight = config.Bind(section, key + "_lunarCombinedWeight", value.lunarCombinedWeight).Value;
            dropTable.equipmentWeight = config.Bind(section, key + "_equipmentWeight", value.equipmentWeight).Value;
            dropTable.voidTier1Weight = config.Bind(section, key + "_voidTier1Weight", value.voidTier1Weight).Value;
            dropTable.voidTier2Weight = config.Bind(section, key + "_voidTier2Weight", value.voidTier2Weight).Value;
            dropTable.voidTier3Weight = config.Bind(section, key + "_voidTier3Weight", value.voidTier3Weight).Value;
            dropTable.voidBossWeight = config.Bind(section, key + "_voidBossWeight", value.voidBossWeight).Value;
            return dropTable;
        }
    }
}
