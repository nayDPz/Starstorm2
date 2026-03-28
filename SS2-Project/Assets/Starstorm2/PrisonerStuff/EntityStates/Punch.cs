using EntityStates;
using R2API;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using SS2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Prisoner.Weapon
{
    public class Punch : BaseSkillState, SteppedSkillDef.IStepSetter
    {
        private static float animDuration = 0.55f;
        private static float baseDuration = 0.55f;
        private static float attackStartTime = 0.4f;
        private static float attackEndTime = 0.55f;
        private static float earlyExitTime = 1f;

        private static float chargeOnHit = 2.5f;

        private static string hitboxGroupName = "Punch";
        private static DamageType damageType = DamageType.Generic;       
        private static float damageCoefficient = 2.5f;
        private static float procCoefficient = 1f;
        private static float pushForce = 300f;
        private static Vector3 bonusForce = Vector3.zero;
        private static float hitStopDuration = 0.012f;
        private static float attackRecoil = 0.75f;
        private static float hitHopVelocity = 4f;
        
        private static string swingSoundString = "NemmandoSwing";
        private static string hitSoundString = "";
        
        private static string playbackRateParam = "Primary.playbackRate";
        private static float crossfadeDuration = 0.2f;
        
        public static GameObject swingEffectPrefab;
        
        public static GameObject hitEffectPrefab;
        
        public static NetworkSoundEventDef impactSound;

        public int swingIndex;
        private string muzzleString;

        protected OverlapAttack attack;
        protected float duration;
        protected bool hasFired;
        protected Animator animator;
        protected bool inHitPause;
        protected float stopwatch;

        private float hitPauseTimer;
        private bool hasHopped;
        private HitStopCachedState hitStopCachedState;
        private Vector3 storedVelocity;

        private GameObject swingEffectInstance;
        private EffectManagerHelper swingEffectInstanceHelper;
        private ScaleParticleSystemDuration swingEffectParticleScaler;
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (stopwatch >= duration * earlyExitTime)
            {
                return InterruptPriority.Any;
            }
            return InterruptPriority.Skill;
        }
        public override void OnEnter()
        {
            base.OnEnter();

            duration = baseDuration / attackSpeedStat;
            animator = GetModelAnimator();
            StartAimMode(2f + duration, false);

            PlayAttackAnimation();

            if (string.IsNullOrEmpty(hitboxGroupName))
                return;

            attack = new OverlapAttack();
            attack.damageType = damageType;
            attack.attacker = gameObject;
            attack.inflictor = gameObject;
            attack.teamIndex = GetTeam();
            attack.damage = damageCoefficient * damageStat;
            attack.procCoefficient = procCoefficient;
            attack.hitEffectPrefab = hitEffectPrefab;
            attack.forceVector = bonusForce;
            attack.pushAwayForce = pushForce;
            attack.hitBoxGroup = FindHitBoxGroup(hitboxGroupName);
            attack.isCrit = RollCrit();
            attack.maximumOverlapTargets = 1000;
            if (impactSound != null)
            {
                attack.impactSound = impactSound.index;
            }
        }

        public void PlayAttackAnimation()
        {
            // need fb anims
            string stateName = "Punch1";
            switch (swingIndex)
            {
                case 0:
                    stateName = "Punch1";
                    break;
                case 1:
                    stateName = "Punch2";
                    break;
            }
            PlayCrossfade("Gesture, Override", stateName, playbackRateParam, animDuration / attackSpeedStat, crossfadeDuration);
            muzzleString = stateName;
        }

        public override void OnExit()
        {
            if (inHitPause)
            {
                RemoveHitstop();
            }
            base.OnExit();
        }

        protected virtual void PlaySwingEffect()
        {
            Transform transform = base.FindModelChild(muzzleString);
            if (transform)
            {
                if (!EffectManager.ShouldUsePooledEffect(swingEffectPrefab))
                {
                    this.swingEffectInstance = UnityEngine.Object.Instantiate<GameObject>(swingEffectPrefab, transform);
                }
                else
                {
                    this.swingEffectInstanceHelper = EffectManager.GetAndActivatePooledEffect(swingEffectPrefab, transform, true);
                    this.swingEffectInstance = this.swingEffectInstanceHelper.gameObject;
                }

                swingEffectParticleScaler = this.swingEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                if (swingEffectParticleScaler)
                {
                    swingEffectParticleScaler.newDuration = swingEffectParticleScaler.initialDuration;
                }
            }
        }

        protected virtual void OnHitEnemyAuthority()
        {
            Util.PlaySound(hitSoundString, gameObject);

            if (!hasHopped)
            {
                if (characterMotor && !characterMotor.isGrounded && hitHopVelocity > 0f)
                {
                    SmallHop(characterMotor, hitHopVelocity / (attackSpeedStat * attackSpeedStat));
                }

                hasHopped = true;
            }

            ApplyHitstop();
        }

        protected void ApplyHitstop()
        {
            if (!inHitPause && hitStopDuration > 0f)
            {
                storedVelocity = characterMotor.velocity;
                hitStopCachedState = CreateHitStopCachedState(characterMotor, animator, playbackRateParam);
                hitPauseTimer = hitStopDuration / attackSpeedStat;
                inHitPause = true;
            }
        }

        protected virtual void FireAttack()
        {
            if (isAuthority && attack != null)
            {
                if (attack.Fire())
                {
                    OnHitEnemyAuthority();
                }
            }
        }

        private void EnterAttack()
        {
            hasFired = true;
            Util.PlayAttackSpeedSound(swingSoundString, gameObject, attackSpeedStat);

            PlaySwingEffect();

            if (isAuthority)
            {
                AddRecoil(-1f * attackRecoil, -2f * attackRecoil, -0.5f * attackRecoil, 0.5f * attackRecoil);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            hitPauseTimer -= Time.fixedDeltaTime;

            if (hitPauseTimer <= 0f && inHitPause)
            {
                RemoveHitstop();
            }

            if (!inHitPause)
            {
                stopwatch += Time.fixedDeltaTime;
            }
            else
            {
                if (characterMotor) characterMotor.velocity = Vector3.zero;
                if (animator) animator.SetFloat(playbackRateParam, 0f);
            }
            bool fireStarted = stopwatch >= duration * attackStartTime;
            bool fireEnded = stopwatch >= duration * attackEndTime;

            //to guarantee attack comes out if at high attack speed the stopwatch skips past the firing duration between frames
            if (fireStarted && !fireEnded || fireStarted && fireEnded && !hasFired)
            {
                if (!hasFired)
                {
                    EnterAttack();
                }
                FireAttack();
            }

            if (stopwatch >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
        }


        private void RemoveHitstop()
        {
            ConsumeHitStopCachedState(hitStopCachedState, characterMotor, animator);
            inHitPause = false;
            characterMotor.velocity = storedVelocity;

            if (this.swingEffectInstance)
            {
                if (swingEffectParticleScaler)
                {
                    swingEffectParticleScaler.newDuration = swingEffectParticleScaler.initialDuration;
                }
            }
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(swingIndex);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            swingIndex = reader.ReadInt32();
        }

        public void SetStep(int i)
        {
            swingIndex = i;
        }
    }
}
