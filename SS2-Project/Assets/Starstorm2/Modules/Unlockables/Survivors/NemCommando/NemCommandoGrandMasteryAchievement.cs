﻿using RoR2;
using UnityEngine;
namespace SS2.Unlocks.NemCommando
{

    public sealed class NemCommandoGrandMasteryAchievement : GenericMasteryAchievement
    {
        public override float RequiredDifficultyCoefficient => 3.5f;

        public override BodyIndex LookUpRequiredBodyIndex()
        {
            return BodyCatalog.FindBodyIndex("NemCommandoBody");
        }
    }
    
}