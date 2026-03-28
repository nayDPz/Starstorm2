using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using SS2;
using RoR2.Skills;

namespace EntityStates.Prisoner
{
    // States run on body to handle transition animations
    public class CuffTransitionBase : BaseState
    {
        protected PrisonerController controller;
        public override void OnEnter()
        {
            base.OnEnter();
            controller = GetComponent<PrisonerController>();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }

    public class UncuffTransition : CuffTransitionBase
    {
        private static float duration = 1.25f;
        private static float crossfadeDuration = 0.3f;
        private static string enterSoundString = "Play_voidman_transform";
        public static GameObject chargeEffectPrefab;
        public static GameObject completionEffectPrefab;
        private static string effectMuzzleString = "Chest";

        private static float moveSpeedCoefficient = 0.4f;
        private static float dampingCoefficient = 0.1f;
        private static float jumpPowerOverride = 7f;

        private float baseJumpPower;
        private GameObject chargeEffectInstance;
        private Transform effectMuzzle;
        public override void OnEnter()
        {
            base.OnEnter();

            Util.PlaySound(enterSoundString, gameObject);
            PlayCrossfade("Gesture, Override", "Uncuff", "Cuff.playbackRate", duration, crossfadeDuration);

            var animator = GetModelAnimator();
            if (animator)
            {
                var layerIndex = animator.GetLayerIndex("Body, Uncuffed");
                animator.SetLayerWeight(layerIndex, 1f);
            }


            if (NetworkServer.active)
            {
                controller.AddCharge(100f);
            }

            effectMuzzle = FindModelChild(effectMuzzleString);
            if (effectMuzzle)
            {
                if (chargeEffectPrefab)
                {
                    chargeEffectInstance = UnityEngine.Object.Instantiate<GameObject>(chargeEffectPrefab, transform.position, transform.rotation);
                    chargeEffectInstance.transform.parent = transform;
                    ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                    if (component)
                    {
                        component.newDuration = duration;
                    }
                }
            }

            baseJumpPower = characterBody.baseJumpPower;
            characterBody.baseJumpPower = jumpPowerOverride;
            characterMotor.walkSpeedPenaltyCoefficient = moveSpeedCoefficient;
            characterBody.MarkAllStatsDirty();
        }

        public override void OnExit()
        {
            base.OnExit();

            if (chargeEffectInstance)
            {
                Destroy(chargeEffectInstance);
            }

            controller.isUncuffed = true; //////////////////////////////////////////////////////////////////////

            characterBody.baseJumpPower = baseJumpPower;
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterBody.MarkAllStatsDirty();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                characterMotor.velocity -= characterMotor.velocity * dampingCoefficient;

                if (fixedAge >= duration)
                {
                    if (completionEffectPrefab)
                    {
                        EffectManager.SimpleMuzzleFlash(completionEffectPrefab, gameObject, effectMuzzleString, true);
                    }
                    outer.SetNextStateToMain();

                    if (controller.cuffModeStateMachine)
                    {
                        controller.cuffModeStateMachine.SetNextState(new UncuffedMode());
                    }
                }
                
            }
        }
    }

    public class CuffTransition : CuffTransitionBase
    {
        private static float duration = 1.25f;
        private static float crossfadeDuration = 0.3f;
        private static string enterSoundString = "Play_voidman_transform_return";
        public static GameObject chargeEffectPrefab;
        public static GameObject completionEffectPrefab;
        private static string effectMuzzleString = "Chest";

        private static float moveSpeedCoefficient = 0.4f;
        private static float dampingCoefficient = 0.1f;
        private static float jumpPowerOverride = 7f;

        private float baseJumpPower;
        private GameObject chargeEffectInstance;
        private Transform effectMuzzle;
        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active)
            {
                controller.AddCharge(-100f);
            }

            Util.PlaySound(enterSoundString, gameObject);
            PlayCrossfade("Gesture, Override", "Cuff", "Cuff.playbackRate", duration, crossfadeDuration);

            var animator = GetModelAnimator();
            if (animator)
            {
                var layerIndex = animator.GetLayerIndex("Body, Uncuffed");
                animator.SetLayerWeight(layerIndex, 0f);
            }


            effectMuzzle = FindModelChild(effectMuzzleString);
            if (effectMuzzle)
            {
                if (chargeEffectPrefab)
                {
                    chargeEffectInstance = UnityEngine.Object.Instantiate<GameObject>(chargeEffectPrefab, transform.position, transform.rotation);
                    chargeEffectInstance.transform.parent = transform;
                    ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                    if (component)
                    {
                        component.newDuration = duration;
                    }
                }
            }

            baseJumpPower = characterBody.baseJumpPower;
            characterBody.baseJumpPower = jumpPowerOverride;
            characterMotor.walkSpeedPenaltyCoefficient = moveSpeedCoefficient;
        }

        public override void OnExit()
        {
            base.OnExit();

            if (chargeEffectInstance)
            {
                Destroy(chargeEffectInstance);
            }

            controller.isUncuffed = false;///////////////////////////////////////////////////////////////////////////////////////////

            characterBody.baseJumpPower = baseJumpPower;
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                characterMotor.velocity -= characterMotor.velocity * dampingCoefficient;

                if (fixedAge >= duration)
                {
                    if (completionEffectPrefab)
                    {
                        EffectManager.SimpleMuzzleFlash(completionEffectPrefab, gameObject, effectMuzzleString, true);
                    }
                    outer.SetNextStateToMain();
                    
                    if (controller.cuffModeStateMachine)
                    {
                        controller.cuffModeStateMachine.SetNextState(new CuffedMode());
                    }
                }
            }
        }
    }
}
