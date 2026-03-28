using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.UI;
namespace EntityStates.Prisoner.Weapon
{
    public class AimSacrifice : BaseSkillState
    {
        public static GameObject indicatorPrefab;
        public static GameObject crosshairPrefab;

        private static string enterSoundString = "";
        private static string exitSoundString = "";

        private static float trackerUpdateFrequency = 20f;
        private static float searchDistance = 50f;
        private static float searchAngle = 24f;

        private float trackerUpdateStopwatch;
        private Indicator indicator;
        private HurtBox trackingTarget;

        private BullseyeSearch search;
        private CrosshairUtils.OverrideRequest crosshairOverrideRequest;
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            Util.PlaySound(enterSoundString, gameObject);

            search = new BullseyeSearch();
            indicator = new Indicator(gameObject, indicatorPrefab);
            indicator.active = true;

            if (crosshairPrefab)
            {
                crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(characterBody, crosshairPrefab, CrosshairUtils.OverridePriority.Skill);
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            indicator.active = false;

            Util.PlaySound(exitSoundString, gameObject);

            if (crosshairOverrideRequest != null)
            {
                crosshairOverrideRequest.Dispose();
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isAuthority)
            {
                FixedUpdateAuthority();
            }
        }
        private void FixedUpdateAuthority()
        {
            StartAimMode(2f);

            trackerUpdateStopwatch += Time.fixedDeltaTime;
            if (trackerUpdateStopwatch >= 1f / trackerUpdateFrequency)
            {
                trackerUpdateStopwatch -= 1f / trackerUpdateFrequency;
                Ray aimRay = new Ray(inputBank.aimOrigin, inputBank.aimDirection);
                SearchForTarget(aimRay);
                indicator.targetTransform = (trackingTarget ? trackingTarget.transform : null);
            }

            if (IsKeyDownAuthority() == false)
            {
                if (trackingTarget != null)
                {
                    outer.SetNextState(new Sacrifice { target = trackingTarget });
                }
                else
                {
                    activatorSkillSlot.stock++;
                    outer.SetNextStateToMain();
                }
            }
        }

        public virtual void SearchForTarget(Ray aimRay)
        {
            TeamMask filter = TeamMask.allButNeutral;
            filter.RemoveTeam(teamComponent.teamIndex);
            search.teamMaskFilter = filter;
            search.filterByLoS = true;
            search.searchOrigin = aimRay.origin;
            search.searchDirection = aimRay.direction;
            search.sortMode = BullseyeSearch.SortMode.Angle;
            search.maxDistanceFilter = searchDistance;
            search.maxAngleFilter = searchAngle;
            search.RefreshCandidates();
            search.FilterOutGameObject(base.gameObject);
            var hits = search.GetResults();
            foreach (HurtBox hurtBox in hits)
            {
                if (hurtBox && hurtBox.healthComponent)
                {
                    trackingTarget = hurtBox;
                    return;
                }
            }
        }
    }
}
