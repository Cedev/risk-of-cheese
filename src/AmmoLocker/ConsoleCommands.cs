using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AmmoLocker
{
    public static class ConsoleCommands
    {
        [ConCommand(commandName = "cheeseboard_ammolocker_dropitems", flags = ConVarFlags.ExecuteOnServer, helpText = "Drop items useful for debugging the ammo locker")]
        public static void DropDebugItems(ConCommandArgs args)
        {
            //Get the player body to use a position:	
            var body = PlayerCharacterMasterController.instances[0].master.GetBody();
            var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(AmmoLocker.ammoLockerDef.equipmentIndex), transform.position, transform.forward * 20f);
            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.Firework.itemIndex), transform.position, transform.forward * 40f);
            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.Squid.itemIndex), transform.position, transform.forward * 60f);
            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.DLC1Content.Items.EquipmentMagazineVoid.itemIndex), transform.position, transform.forward * 80f);

        }


        [ConCommand(commandName = "cheeseboard_ammolocker_listcrosshairs", flags = ConVarFlags.ExecuteOnServer, helpText = "List all crosshair controllers and their stock rules")]
        public static void ListCrosshairs(ConCommandArgs args)
        {
            foreach (var crosshairManager in CrosshairManager.instancesList)
            {
                Log.LogMessage(string.Format("Found CrosshairManager {0} attached to {1}", crosshairManager.name, crosshairManager.gameObject.name));
                if (crosshairManager.crosshairController != null)
                {
                    var crosshairController = crosshairManager.crosshairController;
                    Log.LogMessage(string.Format("Found CrosshairController {0} attached to {1}", crosshairController.name, crosshairController.gameObject.name));
                    foreach(var sprite in crosshairController.skillStockSpriteDisplays)
                    {
                        Log.LogMessage(string.Format(
                            "SkillStockSpriteDisplay skillSlot: {0}, requiredSkillDef: {1}, minStock: {2}, maxStock: {3}",
                            sprite.skillSlot,
                            sprite.requiredSkillDef?.skillName,
                            sprite.minimumStockCountToBeValid,
                            sprite.maximumStockCountToBeValid));
                    }
                }
            }

        }
    }
}
