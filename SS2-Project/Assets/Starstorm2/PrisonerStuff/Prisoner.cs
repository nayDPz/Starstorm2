using RoR2;
using UnityEngine;
using RoR2.Skills;
using System.Runtime.CompilerServices;
using UnityEngine.AddressableAssets;
using R2API;
using MSU;
using System.Collections;
using RoR2.ContentManagement;
using RoR2.Orbs;

namespace SS2.Survivors
{
    public sealed class Prisoner : SS2Survivor, IContentPackModifier
    {
        public override SS2AssetRequest<SurvivorAssetCollection> AssetRequest => SS2Assets.LoadAssetAsync<SurvivorAssetCollection>("acPrisoner", SS2Bundle.Prisoner);
        public override void Initialize()
        {
            ModifyPrefab();

            Sacrifice = ProcTypeAPI.ReserveProcType();
            RecalculateStatsAPI.GetStatCoefficients += GetStatCoefficients;
            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;

            sacrificeBuffOrbEffectPrefab = SS2Assets.LoadAsset<GameObject>("SacrificeBuffOrbEffect", SS2Bundle.Prisoner);
            boostEffect = SS2Assets.LoadAsset<GameObject>("PrisonerBoostEffect", SS2Bundle.Prisoner);
        }

        public static ModdedProcType Sacrifice;
        private static GameObject sacrificeBuffOrbEffectPrefab;
        private static GameObject boostEffect;
        private static float sacrificeBuffOrbSpeed = 80f;
        private static float sacrificeBuffDuration = 30f;
        private static float movespeedPerBuff = 0.15f;
        private static float attackSpeedPerBuff = 0.15f;
        private static int jumpsPerBuff = 1;

        private void OnCharacterDeathGlobal(DamageReport damageReport)
        {
            if (damageReport.attackerBody && damageReport.damageInfo.procChainMask.HasModdedProc(Sacrifice))
            {
                OrbManager.instance.AddOrb(new SacrificeBuffOrb
                {
                    origin = damageReport.victimBody.corePosition,
                    target = damageReport.attackerBody.mainHurtBox,
                });
            }
        }
        public class SacrificeBuffOrb : Orb
        {
            public override void Begin()
            {
                if (target)
                {
                    duration = distanceToTarget / sacrificeBuffOrbSpeed;
                    EffectData effectData = new EffectData { origin = origin, genericFloat = duration };
                    effectData.SetHurtBoxReference(target);
                    EffectManager.SpawnEffect(sacrificeBuffOrbEffectPrefab, effectData, true);
                }
            }
            public override void OnArrival()
            {
                if (target && target.healthComponent)
                {
                    var body = target.healthComponent.body;
                    body.AddTimedBuff(SS2Content.Buffs.bdPrisonerBoost, sacrificeBuffDuration);

                    Transform targetTransform = body.mainHurtBox ? body.mainHurtBox.transform : body.coreTransform;
                    EffectData effectData = new EffectData
                    {
                        origin = targetTransform.position
                    };
                    if (body.mainHurtBox)
                    {
                        effectData.SetHurtBoxReference(body.gameObject);
                    }
                    EffectManager.SpawnEffect(boostEffect, effectData, true);
                }
            }
        }

        
        private static float blockArmor = 200f;
        private void GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(SS2Content.Buffs.bdPrisonerBlock))
            {
                args.armorAdd += blockArmor; 
            }

            int boostCount = sender.GetBuffCount(SS2Content.Buffs.bdPrisonerBoost);
            if (boostCount > 0)
            {
                args.attackSpeedMultAdd += attackSpeedPerBuff * boostCount;
                args.moveSpeedMultAdd += movespeedPerBuff * boostCount;
                args.jumpCountAdd += jumpsPerBuff * boostCount;
            }
        }

        public void ModifyPrefab()
        {
            var cb = CharacterPrefab.GetComponent<CharacterBody>();
            cb.preferredPodPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SurvivorPod/SurvivorPod.prefab").WaitForCompletion();
            cb.GetComponent<ModelLocator>().modelTransform.GetComponent<FootstepHandler>().footstepDustPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/GenericFootstepDust.prefab").WaitForCompletion();
        }
    }
}
