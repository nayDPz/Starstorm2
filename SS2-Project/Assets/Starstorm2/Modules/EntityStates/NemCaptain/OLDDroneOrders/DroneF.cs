﻿using UnityEngine;

namespace EntityStates.NemCaptain.Weapon
{
    public class DroneF : CallDroneBase
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("drone f!");
        }
    }
}
