﻿using RoR2;
using UnityEngine;

namespace EntityStates.Beastmaster.Weapon
{
    internal class FireTriScatter : BaseSkillState
    {
        private float duration
        {
            get
            {
                return baseTargetDuration / base.attackSpeedStat;
            }
        }

        private float delayBetweenVolleys
        {
            get
            {
                return duration / baseTargetAmountOfVolleys;
            }
        }

        [Tooltip("How much the duration should last.")]
        [SerializeField]
        public float baseTargetDuration;

        [Tooltip("Amount of volleys to do before finishing.")]
        [SerializeField]
        public float baseTargetAmountOfVolleys;

        [Tooltip("Amount of projectiles to spawn per volley.")]
        [SerializeField]
        public float baseAmountOfScatterPerVolley;

        [SerializeField]
        public float baseProjectileDamageCoefficient;

        [SerializeField]
        public float baseProjectileDesiredSpeed;

        [SerializeField]
        public float raycastMaxLength;

        [SerializeField]
        public GameObject projectilePrefab;

        [SerializeField]
        public GameObject muzzleEffectPrefab;

        [Tooltip("Transform names in the Child Locator to use in the FIRST volley, LEFT.")]
        [SerializeField]
        public string muzzleFirstVolleyLeft;

        [Tooltip("Transform names in the Child Locator to use in the FIRST volley, RIGHT.")]
        [SerializeField]
        public string muzzleFirstVolleyRight;

        [Tooltip("Transform names in the Child Locator to use in the SECOND volley, LEFT.")]
        [SerializeField]
        public string muzzleSecondVolleyLeft;

        [Tooltip("Transform names in the Child Locator to use in the SECOND volley, RIGHT.")]
        [SerializeField]
        public string muzzleSecondVolleyRight;

        [Tooltip("Transform names in the Child Locator to use in the THIRD volley, LEFT.")]
        [SerializeField]
        public string muzzleThirdVolleyLeft;

        [Tooltip("Transform names in the Child Locator to use in the THIRD volley, RIGHT.")]
        [SerializeField]
        public string muzzleThirdVolleyRight;

        [SerializeField]
        public SerializableEntityStateType comboFinalizerState;

        private ChildLocator childLocator;
        private float countdownSinceLastVolley;
        private int volleyCount;

        public override void OnEnter()
        {
            base.OnEnter();
            base.StartAimMode(duration, false);
            ModelLocator component = characterBody.GetComponent<ModelLocator>();
            if (component && component.modelTransform)
            {
                childLocator = component.modelTransform.GetComponent<ChildLocator>();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            countdownSinceLastVolley -= Time.fixedDeltaTime;
            if (volleyCount < baseTargetAmountOfVolleys && this.countdownSinceLastVolley <= 0f)
            {
                this.volleyCount++;
                this.countdownSinceLastVolley += this.delayBetweenVolleys;
                for (int i = 0; i < baseAmountOfScatterPerVolley; i++)
                {
                    this.FireScatterShot(volleyCount, i);
                }
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        private void FireScatterShot(int currentVolley, int currentShotInVolley)
        {
            //Was previously a transform...but SimpleMuzzleFlash only accepts strings and not transforms
            string muzzle = "";
            Ray aimRay = base.GetAimRay();
            if (childLocator)
            {
                switch (currentVolley % 3)
                {
                    case 2:
                        {
                            switch (currentShotInVolley % 2)
                            {
                                case 0:
                                    {
                                        muzzle = muzzleSecondVolleyLeft;
                                        break;
                                    }
                                default:
                                    {
                                        muzzle = muzzleSecondVolleyRight;
                                        break;
                                    }
                            }
                            break;
                        }
                    case 1:
                        {
                            switch (currentShotInVolley % 2)
                            {
                                case 0:
                                    {
                                        muzzle = muzzleFirstVolleyLeft;
                                        break;
                                    }
                                default:
                                    {
                                        muzzle = muzzleFirstVolleyRight;
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            switch (currentShotInVolley % 2)
                            {
                                case 0:
                                    {
                                        muzzle = muzzleThirdVolleyLeft;
                                        break;
                                    }
                                default:
                                    {
                                        muzzle = muzzleThirdVolleyRight;
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            if (muzzle.Length > 0)
            {
                Transform transform = childLocator.FindChild(muzzle);
                if (transform)
                {
                    aimRay.origin = transform.position;
                    if (muzzleEffectPrefab)
                    {
                        EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzle, false);
                    }
                    Ray defaultAimray = GetAimRay();
                    //Apparently this avoids hitting with yourself and... thats about it...?
                    if (Util.CharacterRaycast(base.gameObject, defaultAimray, out RaycastHit raycastHit, raycastMaxLength, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                    {
                        aimRay.direction = raycastHit.point - aimRay.origin;
                    }
                    else
                    {
                        aimRay.direction = (defaultAimray.direction * raycastMaxLength) - aimRay.origin;
                    }
                }
            }
        }
    }
}