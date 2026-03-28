using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoR2;
using RoR2.UI;

namespace SS2.Components
{
    [RequireComponent(typeof(HudElement))]
    [RequireComponent(typeof(CrosshairController))]
    [RequireComponent(typeof(ImageFillController))]
    public class PrisonerCrosshairController : MonoBehaviour
    {
        private PrisonerController controller;
        private HudElement hudElement;
        private ImageFillController ifc;

        private void Awake()
        {
            hudElement = GetComponent<HudElement>();
            ifc = GetComponent<ImageFillController>();
        }

        private void Start()
        {
            if (hudElement.targetCharacterBody != null && hudElement.targetCharacterBody.TryGetComponent(out PrisonerController pc))
            {
                controller = pc;
            }
        }

        private void FixedUpdate()
        {
            if (controller)
            {
                ifc.SetTValue((PrisonerController.maxCharge - controller.charge) / PrisonerController.maxCharge);
            }
        }
    }
}
