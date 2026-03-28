using RoR2.Skills;
using UnityEngine;
using JetBrains.Annotations;
using RoR2;
using SS2.Components;

namespace SS2
{
    [CreateAssetMenu(menuName = "Starstorm2/SkillDef/PrisonerCuffSkillDef")]
    public class PrisonerCuffSkillDef : SkillDef
    {
        public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
        {
            return new InstanceData
            {
                pc = skillSlot.GetComponent<PrisonerController>()
            };
        }

        public override bool CanExecute([NotNull] GenericSkill skillSlot)
        {
            PrisonerController pc = ((InstanceData)skillSlot.skillInstanceData).pc;
            return pc.isUncuffed && base.CanExecute(skillSlot);
        }

        public override bool IsReady([NotNull] GenericSkill skillSlot)
        {
            PrisonerController pc = ((InstanceData)skillSlot.skillInstanceData).pc;
            return pc.isUncuffed && base.IsReady(skillSlot);
        }
        protected class InstanceData : SkillDef.BaseSkillInstanceData
        {
            public PrisonerController pc;
        }
    }
}
