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
    public class JumpAttack : BaseSkillState
    {
        private static float baseDuration = 0.67f;
        private static float punchAttackStartTime = 0.5f;
        private static float punchAttackEndTime = 0.6f;
        private static float slamAttackStartTime = 0.5f;
        private static float slamAttackEndTime = 0.6f;
        private static float earlyExitTime = 1.0f;

        private static float chargeOnHit = 20f;


        private static float minY = -1f;
        private static float maxY = 0.3f;
        private static float aimVelocity = 7f;
        private static float forwardVelocity = 7f;
        private static float upwardVelocity = 3f;
        private static float airControl = 0.8f;
        private static float accelerationOverride = 200f;
        private static float swingHopVelocity = 9f;

        public static AnimationCurve forwardSpeedCurve;
        private static float dodgeFOV;

        private static string hitboxGroupName = "Slam";
        private static DamageType damageType = DamageType.Stun1s;
        private static float damageCoefficient = 5f;
        private static float procCoefficient = 1f;
        private static float pushForce = 300f;
        private static Vector3 bonusForce = Vector3.zero;
        private static float hitStopDuration = 0.012f;
        private static float attackRecoil = 0.75f;
        private static float hitHopVelocity = 12f;

        private static string swingSoundString = "NemmandoSwing";
        private static string hitSoundString = "";

        private static string playbackRateParam = "Primary.playbackRate";
        private static float crossfadeDuration = 0.2f;

        public static GameObject punchEffectPrefab;
        public static GameObject punchHitEffectPrefab;

        public static GameObject slamEffectPrefab;
        public static GameObject slamHitEffectPrefab;

        public static NetworkSoundEventDef punchImpactSound;
        public static NetworkSoundEventDef slamImpactSound;

        private string muzzleString;

        protected OverlapAttack attack;
        protected float duration;
        protected bool hasFired;
        protected Animator animator;
        protected bool inHitPause;
        protected float stopwatch;

        private float hitPauseTimer;
        private bool hitOnceAuthority;
        private HitStopCachedState hitStopCachedState;
        private Vector3 storedVelocity;
        
        private GameObject swingEffectInstance;
        private EffectManagerHelper swingEffectInstanceHelper;
        private ScaleParticleSystemDuration swingEffectParticleScaler;

        private PrisonerController controller;

        private bool isCuffed;

        private float attackStartTime;
        private float attackEndTime;
        private GameObject swingEffectPrefab;
        private GameObject hitEffectPrefab;
        private NetworkSoundEventDef impactSound;
        private float originalAirControl;
        private float originalAcceleration;

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (stopwatch >= duration * earlyExitTime)
            {
                return InterruptPriority.Skill;
            }
            return InterruptPriority.PrioritySkill;
        }
        public override void OnEnter()
        {
            base.OnEnter();
            controller = GetComponent<PrisonerController>();
            if (controller)
            {
                isCuffed = controller.isUncuffed == false;
                controller.UnsetPrimaryOverride();
            }
            attackStartTime = isCuffed ? slamAttackStartTime : punchAttackStartTime;
            attackEndTime = isCuffed ? slamAttackEndTime : punchAttackEndTime;
            swingEffectPrefab = isCuffed ? slamEffectPrefab : punchEffectPrefab;
            hitEffectPrefab = isCuffed ? slamHitEffectPrefab : punchHitEffectPrefab;
            impactSound = isCuffed ? slamImpactSound : punchImpactSound;

            duration = baseDuration / attackSpeedStat;
            animator = GetModelAnimator();
            StartAimMode(2f + duration, false);

            PlayAttackAnimation();

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

            if (isAuthority)
            {
                characterBody.isSprinting = true;

                Vector3 direction = inputBank.aimDirection;
                direction.y = Mathf.Max(direction.y, minY);
                Vector3 aim = direction.normalized * aimVelocity;
                Vector3 up = Vector3.up * upwardVelocity;
                Vector3 forward = new Vector3(direction.x, 0f, direction.z).normalized * forwardVelocity;
                characterMotor.Motor.ForceUnground(0.1f);
                characterMotor.velocity = (aim + forward) * attackSpeedStat + up;

                originalAirControl = characterMotor.airControl;
                characterMotor.airControl = airControl;
            }

            characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            originalAcceleration = characterBody.baseAcceleration;
            characterBody.baseAcceleration = accelerationOverride;
            characterBody.MarkAllStatsDirty();
        }

        public void PlayAttackAnimation()
        {
            // need fb anims
            string stateName = isCuffed ? "DashSlam" : "DashPunch" + UnityEngine.Random.Range(1,3); // 1 or 2
            PlayCrossfade("FullBody, Override", stateName, playbackRateParam, duration / attackSpeedStat, crossfadeDuration);
            muzzleString = stateName;
        }

        public override void OnExit()
        {
            if (inHitPause)
            {
                RemoveHitstop();
            }
            if (isAuthority)
            {
                characterMotor.airControl = originalAirControl;
            }

            characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            characterBody.baseAcceleration = originalAcceleration;
            characterBody.MarkAllStatsDirty();

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

            if (!hitOnceAuthority)
            {
                if (isCuffed)
                {
                    controller.AddCharge(chargeOnHit); // NOT NETWORKED STUPDI FUCK!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                }
                if (characterMotor && !characterMotor.isGrounded && hitHopVelocity > 0f)
                {
                    SmallHop(characterMotor, hitHopVelocity / (attackSpeedStat * attackSpeedStat));
                }

                hitOnceAuthority = true;
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
                characterMotor.velocity = Vector3.zero;
                SmallHop(characterMotor, swingHopVelocity);
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
    }
}
