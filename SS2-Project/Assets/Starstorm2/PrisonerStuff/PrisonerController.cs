using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using HG;
using RoR2.HudOverlay;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;

namespace SS2
{
    [RequireComponent(typeof(InputBankTest))]
    [RequireComponent(typeof(TeamComponent))]
    [RequireComponent(typeof(CharacterBody))]
    public class PrisonerController : NetworkBehaviour, IOnTakeDamageServerReceiver
    {
        public float charge
        {
            get
            {
                return _charge;
            }
        }
        public bool isUncuffed
        {
            get;
            set;
        }

        [Header("Cached Components")]
        public CharacterBody characterBody;
        public Animator characterAnimator;
        public EntityStateMachine cuffModeStateMachine;
        public EntityStateMachine bodyStateMachine;
        public EntityStateMachine weaponStateMachine;

        public static float maxCharge = 100f;
        public static float chargeForFullDamage = 500f;
        public static float flatChargeOnDamaged = 5f;
        public static float chargeDeltaThresholdToAnimate = 2f;

        [Header("UI")]
        public GameObject overlayPrefab;
        public string overlayChildLocatorEntry;

        private ChildLocator overlayInstanceChildLocator;
        private Animator overlayInstanceAnimator;
        private OverlayController overlayController;
        private List<ImageFillController> fillUiList = new List<ImageFillController>();
        private TextMeshProUGUI uiChargeText;

        [SyncVar(hook = nameof(OnChargeModified))]
        private float _charge;
        private static int isCorruptedParamHash = Animator.StringToHash("charge");
        private static int chargeParamHash = Animator.StringToHash("isCorrupted");

        private HealthComponent healthComponent;

        private void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
        }

        private void OnEnable()
        {
            if (overlayPrefab)
            {
                OverlayCreationParams overlayCreationParams = new OverlayCreationParams
                {
                    prefab = overlayPrefab,
                    childLocatorEntry = overlayChildLocatorEntry
                };
                overlayController = HudOverlayManager.AddOverlay(gameObject, overlayCreationParams);
                overlayController.onInstanceAdded += OnOverlayInstanceAdded;
                overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
            }
        }

        
        private void OnDisable()
        {
            if (overlayController != null)
            {
                overlayController.onInstanceAdded -= OnOverlayInstanceAdded;
                overlayController.onInstanceRemove -= OnOverlayInstanceRemoved;
                fillUiList.Clear();
                HudOverlayManager.RemoveOverlay(overlayController);
            }
        }

        
        private void FixedUpdate()
        {
            UpdateUI();
        }

        
        private void UpdateUI()
        {
            foreach (ImageFillController imageFillController in fillUiList)
            {
                imageFillController.SetTValue(charge / maxCharge);
            }
            if (overlayInstanceChildLocator)
            {
                overlayInstanceChildLocator.FindChild("ChargeThreshold").rotation = Quaternion.Euler(0f, 0f, Mathf.InverseLerp(0f, maxCharge, charge) * -360f);
            }
            if (overlayInstanceAnimator)
            {
                overlayInstanceAnimator.SetFloat(isCorruptedParamHash, charge);
                overlayInstanceAnimator.SetBool(chargeParamHash, isUncuffed);
            }
            if (uiChargeText)
            {
                StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
                stringBuilder.AppendInt(Mathf.FloorToInt(charge), 1U, 3U).Append("%");
                uiChargeText.SetText(stringBuilder);
                HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
            }
        }

        private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
        {
            fillUiList.Add(instance.GetComponent<ImageFillController>());
            uiChargeText = instance.GetComponentInChildren<TextMeshProUGUI>();
            overlayInstanceChildLocator = instance.GetComponent<ChildLocator>();
            overlayInstanceAnimator = instance.GetComponent<Animator>();
        }
        
        private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
        {
            fillUiList.Remove(instance.GetComponent<ImageFillController>());
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            float fullHealth = characterBody.baseMaxHealth + (characterBody.level - 1) * characterBody.levelMaxHealth;
            float percentDamageDealt = damageReport.damageDealt / fullHealth;
            if (characterBody.HasBuff(SS2Content.Buffs.bdPrisonerBlock))
            {
                AddCharge(percentDamageDealt * chargeForFullDamage + flatChargeOnDamaged);
            }
        }
        
        [Server]
        public void AddCharge(float amount)
        {
            _charge = Mathf.Clamp(charge + amount, 0, maxCharge);
        }

        
        private void OnChargeModified(float newCharge)
        {
            if (overlayInstanceAnimator)
            {
                if (Mathf.Abs(newCharge - charge) > chargeDeltaThresholdToAnimate)
                {
                    overlayInstanceAnimator.SetTrigger("chargeIncreased");
                }
            }
            _charge = newCharge;
        }



        public RoR2.Skills.SkillDef primaryOverride;
        private bool inOverride;
        public void SetPrimaryOverride()
        {
            if (!inOverride && primaryOverride && characterBody.skillLocator.primary)
            {
                inOverride = true;
                characterBody.skillLocator.primary.SetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Replacement);
            }
        }

        public void UnsetPrimaryOverride()
        {
            if (inOverride && primaryOverride && characterBody.skillLocator.primary)
            {
                inOverride = false;
                characterBody.skillLocator.primary.UnsetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Replacement);
            }
        }
    }
}
