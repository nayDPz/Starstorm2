﻿using R2API;
using RoR2;
using UnityEngine;

using MSU;
namespace SS2.Buffs
{
    //What even is this...? -N
    //For future reference, that interface only works on monobehaviours attached to a body, i should make it more clear next time. -N
    /*
    //[DisabledContent]
    public sealed class BloonTrap : BuffBase, IBodyStatArgModifier
    {
        public override BuffDef BuffDef { get; } = SS2Assets.LoadAsset<BuffDef>("BuffBloonTrap", SS2Bundle.Indev);

        private static float slowAmount = 0.5f;

        public override void Initialize()
        {
            base.Initialize();
            On.RoR2.HealthComponent.GetHealthBarValues += AddSuck;
        }

        public void ModifyStatArguments(RecalculateStatsAPI.StatHookEventArgs args)
        {
            args.moveSpeedMultAdd -= slowAmount;
        }

        private HealthComponent.HealthBarValues AddSuck(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues result = orig.Invoke(self);
            if (!self.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes) && self.body.HasBuff(SS2Content.Buffs.BuffBloonTrap))
            {
                result.cullFraction = Mathf.Max(result.cullFraction, 0.2f);
            }
            return result;
        }
    }*/
}
