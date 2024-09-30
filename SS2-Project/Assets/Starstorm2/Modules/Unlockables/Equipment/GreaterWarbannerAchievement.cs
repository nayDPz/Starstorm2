﻿using RoR2;
using RoR2.Achievements;
namespace SS2.Unlocks.Pickups
{
    public sealed class GreaterWarbannerAchievement : BaseAchievement
    {
        private Inventory currentInventory;
        private PlayerCharacterMasterController currentMasterController;
        public override void OnInstall()
        {
            base.OnInstall();
            localUser.onMasterChanged += OnMasterChanged;
            SetMasterController(localUser.cachedMasterController);
        }

        public override void OnUninstall()
        {
            SetMasterController(null);
            localUser.onMasterChanged -= OnMasterChanged;
            base.OnUninstall();
        }
        private void SetMasterController(PlayerCharacterMasterController newMasterController)
        {
            if ((object)currentMasterController != newMasterController)
            {
                if ((object)currentInventory != null)
                {
                    currentInventory.onInventoryChanged -= OnInventoryChanged;
                }
                currentMasterController = newMasterController;
                currentInventory = currentMasterController?.master?.inventory;
                if ((object)currentInventory != null)
                {
                    currentInventory.onInventoryChanged += OnInventoryChanged;
                }
            }
        }
        private void OnInventoryChanged()
        {
            if ((bool)currentInventory)
            {
                ItemDef warbannerDef = RoR2Content.Items.WardOnLevel;
                int warbannerCount = currentInventory.GetItemCount(warbannerDef);
                if (warbannerCount >= 4)
                {
                    Grant();
                }
            }
        }
        private void OnMasterChanged()
        {
            SetMasterController(localUser.cachedMasterController);
        }
    }
}