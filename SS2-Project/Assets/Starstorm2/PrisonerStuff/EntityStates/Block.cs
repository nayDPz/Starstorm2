using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using SS2;
namespace EntityStates.Prisoner.Weapon
{
    public class Block : BaseSkillState
    {
        private static float camEntryDuration = 0.2f;
        private static float camExitDuration = 0.4f;

        private static float moveSpeedCoefficient = 0.4f;
        private static float jumpPowerOverride = 9f;
        private static float massOverride = 400f;

        private static string enterSoundString = "ExecutionerAimSecondary";
        private static string exitSoundString = "ExecutionerExitSecondary";

        private static float animDuration;
        private PrisonerController controller;

        public CameraTargetParams.CameraParamsOverrideHandle camOverrideHandle;
        private CharacterCameraParamsData chargeCameraParams = new CharacterCameraParamsData
        {
            maxPitch = 70f,
            minPitch = -70f,
            pivotVerticalOffset = cameraPivotVerticalOffset,
            idealLocalCameraPos = cameraPosition,
            wallCushion = 0.1f,
        };
        private static float cameraPivotVerticalOffset = 1.37f;
        private static Vector3 cameraPosition = new Vector3(1.2f, -0.75f, -6.1f);
        private Transform hurtboxTransform;
        private float baseJumpPower;
        private float baseMass;

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;

        }
        public override void OnEnter()
        {
            base.OnEnter();
            controller = GetComponent<PrisonerController>();

            string stateName = controller && controller.isUncuffed ? "BlockStartUncuffed" : "BlockStart";
            PlayAnimation("Gesture, Override", stateName, "Secondary.playbackRate", animDuration);
            Util.PlaySound(enterSoundString, gameObject);

            characterBody.SetAimTimer(2f);

            hurtboxTransform = FindModelChild("BlockHurtbox");
            if (hurtboxTransform)
            {
                hurtboxTransform.gameObject.SetActive(true);
            }

            CameraTargetParams.CameraParamsOverrideRequest request = new CameraTargetParams.CameraParamsOverrideRequest
            {
                cameraParamsData = chargeCameraParams,
                priority = 0f
            };
            camOverrideHandle = cameraTargetParams.AddParamsOverride(request, camEntryDuration);

            if (NetworkServer.active)
            {
                characterBody.AddBuff(SS2Content.Buffs.bdPrisonerBlock);
            }

            baseJumpPower = characterBody.baseJumpPower;
            characterBody.baseJumpPower = jumpPowerOverride;
            characterMotor.walkSpeedPenaltyCoefficient = moveSpeedCoefficient;
            baseMass = characterMotor.mass;
            characterMotor.mass = massOverride;
            characterBody.MarkAllStatsDirty();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            characterBody.isSprinting = false;
            characterBody.aimTimer = 2f;

            if (isAuthority)
            {
                if (!inputBank.skill2.down)
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            string stateName = controller && controller.isUncuffed ? "BlockEndUncuffed" : "BlockEnd";
            PlayAnimation("Gesture, Override", stateName, "Secondary.playbackRate", animDuration);
            Util.PlaySound(exitSoundString, gameObject);

            if (hurtboxTransform)
            {
                hurtboxTransform.gameObject.SetActive(false);
            }

            if (cameraTargetParams)
            {
                cameraTargetParams.RemoveParamsOverride(camOverrideHandle, camExitDuration);
            }

            if (NetworkServer.active)
            {
                characterBody.RemoveBuff(SS2Content.Buffs.bdPrisonerBlock);
            }

            characterBody.baseJumpPower = baseJumpPower;
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterMotor.mass = baseMass;
            characterBody.MarkAllStatsDirty();
        }
    }
}