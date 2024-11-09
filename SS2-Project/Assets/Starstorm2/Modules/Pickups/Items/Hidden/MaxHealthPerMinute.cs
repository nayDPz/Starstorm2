﻿using MSU;
using R2API;
using RoR2;
using System.Collections;
using UnityEngine;
using RoR2.ContentManagement;
using System.Collections.Generic;

namespace SS2.Items
{
    // gain 1% max health per minute, per stack
    public sealed class MaxHealthPerMinute : SS2Item
    {
        public override SS2AssetRequest AssetRequest => SS2Assets.LoadAssetAsync<ItemDef>("MaxHealthPerMinute", SS2Bundle.Items);

        public override void Initialize()
        {
            RecalculateStatsAPI.GetStatCoefficients += GetStatCoefficients;
        }

        private void GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            float itemCount = sender.inventory ? sender.inventory.GetItemCount(ItemDef) : 0;
            int minutes = Run.instance ? Mathf.FloorToInt(Run.instance.fixedTime) : 0;
            if (itemCount > 0)
            {
                float hp = minutes * itemCount * 0.01f;
                args.healthMultAdd += hp;
            }
        }

        public override bool IsAvailable(ContentPack contentPack)
        {
            return true;
        }
    }
}
