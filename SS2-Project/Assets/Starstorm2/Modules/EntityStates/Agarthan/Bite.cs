﻿namespace EntityStates.Agarthan
{
    class Bite : BasicMeleeAttack
    {
        public override void OnEnter()
        {
            base.OnEnter();
            swingEffectPrefab = LemurianMonster.Bite.biteEffectPrefab;
            hitEffectPrefab = LemurianMonster.Bite.hitEffectPrefab;
            PlayCrossfade("Gesture", "Bite", "Bite.playbackRate", duration, 0.05f);
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}