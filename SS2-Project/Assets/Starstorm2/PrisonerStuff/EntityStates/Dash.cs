using SS2;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.Skills;
namespace EntityStates.Prisoner
{
    public class Dash : BaseSkillState
    {
        private static float duration = 0.35f;
        private static float initialSpeedCoefficient = 6f;
        private static float finalSpeedCoefficient = 1f;
        private static float upThing = 0.67f;
        private static float dodgeFOV = -1f;
        public static GameObject effectPrefab;
        private static string enterSoundString = "Play_commando_shift";
        public static SkillDef primaryOverride; 

        private float rollSpeed;
        private Vector3 dashVector;
        private Vector3 previousPosition;
        private Animator animator;
        private PrisonerController controller;
        private string skinNameToken;

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            controller = GetComponent<PrisonerController>();
            animator = GetModelAnimator();

 
            if (isAuthority && inputBank && characterDirection)
            {
                dashVector = ((inputBank.moveVector == Vector3.zero) ? characterDirection.forward : inputBank.moveVector).normalized;
            }

            RecalculateRollSpeed();
            if (characterMotor && characterDirection)
            {
                // min is 0, max is rollSpeed
                float y = Mathf.Min(Mathf.Max(characterMotor.velocity.y, 0), rollSpeed) * upThing;
                characterMotor.velocity = dashVector * rollSpeed;
                characterMotor.velocity.y = y;
            }

            Vector3 velocity = characterMotor ? characterMotor.velocity : Vector3.zero;
            previousPosition = transform.position - velocity;


            Util.PlaySound(enterSoundString, gameObject);
            string stateName = controller && controller.isUncuffed ? "DashUncuffed" : "Dash";
            PlayAnimation("FullBody, Override", "Dash", "Utility.playbackRate", duration);
            if (effectPrefab)
            {
                var effectData = new EffectData
                {
                    origin = transform.position,
                    rotation = Util.QuaternionSafeLookRotation(dashVector),
                };
                EffectManager.SpawnEffect(effectPrefab, effectData, false);
            }

            if (controller)
            {
                controller.SetPrimaryOverride();
            }
        }

        private void RecalculateRollSpeed()
        {
            rollSpeed = moveSpeedStat * Mathf.Lerp(initialSpeedCoefficient, finalSpeedCoefficient, fixedAge / duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            RecalculateRollSpeed();

            characterBody.isSprinting = true; // makes cryptic source work when not rolling forwards. sprintanydirection flag also works but this is easier
            StartAimMode(2f);

            if (isAuthority)
            {
                Vector3 normalized = (transform.position - previousPosition).normalized;
                if (characterMotor && characterDirection && normalized != Vector3.zero)
                {
                    Vector3 vector = normalized * rollSpeed;
                    float y = vector.y;
                    vector.y = 0f;
                    float d = Mathf.Max(Vector3.Dot(vector, dashVector), 0f);
                    vector = dashVector * d;
                    vector.y += Mathf.Max(y, 0f);
                    characterMotor.velocity = vector;

                    Vector3 rhs = inputBank ? characterDirection.forward : dashVector;
                    Vector3 rhs2 = Vector3.Cross(Vector3.up, rhs);
                    float num = Vector3.Dot(dashVector, rhs);
                    float num2 = Vector3.Dot(dashVector, rhs2);
                    animator.SetFloat("forwardSpeed", num);
                    animator.SetFloat("rightSpeed", num2);

                }

                previousPosition = transform.position;
                if (fixedAge >= duration && isAuthority)
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }
        }

        public override void OnExit()
        {
            if (cameraTargetParams)
                cameraTargetParams.fovOverride = -1f;
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(dashVector);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            dashVector = reader.ReadVector3();
        }

        
    }
}
