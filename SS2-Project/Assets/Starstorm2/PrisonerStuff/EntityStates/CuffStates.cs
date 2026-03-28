using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using SS2;
using RoR2.Skills;

namespace EntityStates.Prisoner
{
    public class CuffModeBase : BaseState
    {
        protected PrisonerController controller;
        public override void OnEnter()
        {
            base.OnEnter();
            controller = GetComponent<PrisonerController>();
        }
    }

    public class CuffedMode : CuffModeBase
    {
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority && controller && controller.charge >= PrisonerController.maxCharge)
            {
                if (controller.weaponStateMachine)
                {
                    controller.weaponStateMachine.SetInterruptState(new UncuffTransition(), InterruptPriority.PrioritySkill);
                }
            }
        }
    }

    public class UncuffedMode : CuffModeBase
    {
        private static float chargeLossPerSecond = 10f;
        private static float chargeLossInterval = 0.33f;
        public static SkillDef primaryOverride; 
        private float stopwatch;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority && skillLocator)
            {
                skillLocator.primary.SetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Upgrade);
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            if (isAuthority && skillLocator)
            {
                skillLocator.primary.UnsetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Upgrade);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (NetworkServer.active)
            {
                stopwatch += Time.fixedDeltaTime;
                if (stopwatch >= chargeLossInterval)
                {
                    stopwatch -= chargeLossInterval;
                    controller.AddCharge(-1f * chargeLossPerSecond * chargeLossInterval);
                }
            }
            

            if (isAuthority && controller && controller.charge <= 0f)
            {
                if (controller.weaponStateMachine)
                {
                    controller.weaponStateMachine.SetInterruptState(new CuffTransition(), InterruptPriority.Pain);
                }
            }
        }
    }
}
