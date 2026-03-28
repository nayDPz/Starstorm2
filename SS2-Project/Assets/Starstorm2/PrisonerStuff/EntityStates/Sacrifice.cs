using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.UI;
using System;
using RoR2.Orbs;
using R2API;

namespace EntityStates.Prisoner.Weapon
{
    public class Sacrifice : BaseSkillState
    {
        private static float baseDuration = 1.4f;
        private static float fireTime = 0.7f;

        public static GameObject orbEffectPrefab;
        public static GameObject explosionEffectPrefab;
        public static GameObject healthOrbEffectPrefab;
        
        private static float damageCoefficient = 15f;
        private static float procCoefficient = 1f;
        private static int healthOrbCount = 3;
        private static float healthFractionPerOrb = .1f;
        private static float healthOrbMaxSpeed = 90f;
        private static float healthOrbMinSpeed = 120f;

        public HurtBox target;

        private float duration;
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            writer.Write(HurtBoxReference.FromHurtBox(target));
        }
        public override void OnDeserialize(NetworkReader reader)
        {
            target = reader.ReadHurtBoxReference().ResolveHurtBox();
        }

        public override void OnEnter()
        {
            base.OnEnter();

            duration = baseDuration / attackSpeedStat;
            PlayAnimation("Gesture, Override", "Sacrifice", "Special.playbackRate", duration);

            if (target && target.healthComponent && Util.HasEffectiveAuthority(target.healthComponent.gameObject))
            {
                if (target.healthComponent.TryGetComponent(out SetStateOnHurt setStateOnHurt) && setStateOnHurt.canBeFrozen)
                {
                    if (setStateOnHurt.targetStateMachine)
                    {
                        setStateOnHurt.targetStateMachine.SetInterruptState(new Raptured { duration = duration * fireTime, speedMultiplier = attackSpeedStat }, InterruptPriority.Vehicle);
                    }
                }
                
            }

            if (NetworkServer.active)
            {
                var damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special);

                var procChainMask = new ProcChainMask();
                procChainMask.AddModdedProc(SS2.Survivors.Prisoner.Sacrifice);

                OrbManager.instance.AddOrb(new DelayedHitOrb
                {
                    attacker = gameObject,
                    target = target,
                    damageColorIndex = DamageColorIndex.Default,
                    damageValue = damageCoefficient * damageStat,
                    damageType = damageType,
                    isCrit = RollCrit(),
                    procChainMask = procChainMask,
                    procCoefficient = procCoefficient,
                    delay = duration * fireTime,
                });
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority && fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public class DelayedHitOrb : GenericDamageOrb
        {
            public float delay;

            public override void Begin()
            {
                base.Begin();
                duration = delay;
            }

            public override GameObject GetOrbEffect()
            {
                return orbEffectPrefab;
            }

            public override void OnArrival()
            {
                if (target && target.transform)
                {
                    var effectData = new EffectData
                    {
                        origin = target.transform.position
                    };
                    EffectManager.SpawnEffect(explosionEffectPrefab, effectData, true);

                    base.OnArrival();

                    if (!healthOrbEffectPrefab)
                    {
                        healthOrbEffectPrefab = RoR2.Orbs.OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/HealthOrbEffect");
                    }

                    if (attacker && attacker.TryGetComponent(out CharacterBody attackerBody))
                    {
                        for (int i = 0; i < healthOrbCount; i++)
                        {
                            OrbManager.instance.AddOrb(new HealOrb
                            {
                                origin = target.transform.position,
                                target = attackerBody.mainHurtBox,
                            });
                        }
                    }
                    
                }
            }
        }

        public class HealOrb : Orb
        {
            public override void Begin()
            {
                if (target)
                {
                    float speed = UnityEngine.Random.Range(healthOrbMinSpeed, healthOrbMaxSpeed);
                    duration = distanceToTarget / speed;
                    EffectData effectData = new EffectData { origin = origin, genericFloat = duration };
                    effectData.SetHurtBoxReference(target);
                    EffectManager.SpawnEffect(healthOrbEffectPrefab, effectData, true);
                }
            }
            public override void OnArrival()
            {
                if (target && target.healthComponent)
                {
                    target.healthComponent.HealFraction(healthFractionPerOrb, default(ProcChainMask));
                }
            }
        }
    }

    public class Raptured : BaseState
    {
        private static float upSpeed = 15f;
        public static AnimationCurve upSpeedCurve;

        public float speedMultiplier;
        public float duration;

        private IDisplacementReceiver motor;
        private bool hasMotor;
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
        public override void OnEnter()
        {
            base.OnEnter();

            if (TryGetComponent(out IDisplacementReceiver displacementReceiver))
            {
                hasMotor = true;
                motor = displacementReceiver;
            }
            if (characterMotor)
            {
                var grav = characterMotor.gravityParameters;
                grav.channeledAntiGravityGranterCount++;
                characterMotor.gravityParameters = grav;
                characterMotor.Motor.ForceUnground(duration);
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            if (characterMotor)
            {
                var grav = characterMotor.gravityParameters;
                grav.channeledAntiGravityGranterCount--;
                characterMotor.gravityParameters = grav;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                if (hasMotor)
                {
                    if (characterMotor)
                    {
                        characterMotor.velocity = Vector3.zero;
                    }
                    if (rigidbodyMotor)
                    {
                        rigidbodyMotor.moveVector = Vector3.zero;
                    }
                    float t = fixedAge / duration;
                    Vector3 displacement = Vector3.up * upSpeedCurve.Evaluate(t) * upSpeed * GetDeltaTime() * speedMultiplier;
                    motor.AddDisplacement(displacement);
                }
                if (fixedAge >= duration)
                {
                    outer.SetNextStateToMain();
                }
            }
            
        }
    }
}
