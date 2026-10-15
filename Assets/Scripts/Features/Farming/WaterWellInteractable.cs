using System;
using UnityEngine;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;
using FarmBeware.Data.Runtime;

namespace FeaturesFarming
{
    /// <summary>
    /// Garden Well Interactable:
    /// Station 'Place & Collect' refill system for the agricultural Plant Waterer (Watering Can 100L).
    /// Follows the same robust architecture as KitchenSink (Place -> Refill -> Collect / Cancel).
    /// Features:
    /// - Physical placement of Plant Waterer at the well trough
    /// - Continuous visual rotation of the well crank axle
    /// - Screen-space radial progress indicator with countdown
    /// - Proportional quota calculation (cancelling early or filling partial volume only consumes proportional quota)
    /// - Safe inventory handling and full Save/Load persistence
    /// - Procedural water trickling and collection sound effects
    /// </summary>
    [RequireComponent(typeof(Highlightable))]
    [DisallowMultipleComponent]
    public class WaterWellInteractable : MonoBehaviour, IInteractable, IObstructionExempt
    {
        public static WaterWellInteractable Instance { get; private set; }

        [Header("Interaction Prompt")]
        [Tooltip("The base label shown on screen when player approaches the well.")]
        [SerializeField] private string promptLabel = "Garden Well";

        [Header("Refill Settings")]
        [Tooltip("Time required to refill water in seconds (default 10s).")]
        [SerializeField] private float refillDuration = 10f;

        [Tooltip("Maximum full refills permitted per in-game day (default 2 full refills = 200L).")]
        [SerializeField] private float maxRefillsPerDay = 2f;

        [Header("Audio Feedback")]
        [SerializeField] private float sfxVolume = 0.5f;

        // ── Station State Machine ──
        private bool hasWatererInWell;
        private bool isRefillingWater;
        private bool isWatererReadyToCollect;
        private float refillWaterProgress;
        private float watererStartAmount;
        private float refillsUsedToday;

        public bool HasWatererInWell => hasWatererInWell;
        public bool IsRefillingWater => isRefillingWater;
        public bool IsWatererReadyToCollect => isWatererReadyToCollect;
        public float RefillWaterProgress => refillWaterProgress;
        public float WatererStartAmount => watererStartAmount;
        public float RefillsUsedToday => refillsUsedToday;
        public float MaxRefillsPerDay => maxRefillsPerDay;
        public float RefillDuration => refillDuration;

        // ── Components & Visuals ──
        private WorldLabel worldLabel;
        private WaterRefillProgressUI refillProgressUI;
        private Transform crankTransform;
        private GameObject watererVisualGO;
        private AudioSource audioSource;

        private static AudioClip trickleClip;
        private static AudioClip chimeClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            refillProgressUI = GetComponent<WaterRefillProgressUI>() ?? gameObject.AddComponent<WaterRefillProgressUI>();

            worldLabel = GetComponent<WorldLabel>() ?? GetComponentInChildren<WorldLabel>() ?? gameObject.AddComponent<WorldLabel>();
            worldLabel.displayName = promptLabel;

            // Ensure collider is active so PlayerInteractor raycast/overlap detects the well
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(2.6f, 2.4f, 2.6f);
                box.center = new Vector3(0f, 1.2f, 0f);
            }

            // Find crank/axle to rotate during water drawing
            FindCrankTransform();

            // Setup 3D visual watering can in trough
            SetupWatererVisual();

            // Audio Source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 2f;
                audioSource.maxDistance = 15f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            EnsureProceduralAudioClips();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            UpdateVisuals();
            UpdatePromptLabel();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            UpdateVisuals();
            UpdatePromptLabel();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;

            StopAudio();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;

            if (Instance == this)
                Instance = null;
        }

        private void HandleDayChanged(int newDay)
        {
            refillsUsedToday = 0f;
            UpdatePromptLabel();
        }

        private void Update()
        {
            float timeMultiplier = FeaturesTime.DayNightTimeManager.Instance != null
                ? FeaturesTime.DayNightTimeManager.Instance.FastForwardMultiplier
                : 1f;

            if (isRefillingWater)
            {
                refillWaterProgress += (Time.deltaTime * timeMultiplier) / Mathf.Max(0.1f, refillDuration);

                // Rotate crank handle for dynamic realism
                if (crankTransform != null)
                {
                    crankTransform.Rotate(Vector3.right, 120f * Time.deltaTime * timeMultiplier, Space.Self);
                }

                if (refillWaterProgress >= 1f)
                {
                    CompleteWaterRefill();
                }
                else
                {
                    UpdatePromptLabel();
                }
            }

            // Sync in-world Screen-Space Refill UI
            if (refillProgressUI != null)
            {
                float remSec = Mathf.Max(0f, refillDuration * (1f - refillWaterProgress));
                refillProgressUI.UpdateRefillState(isRefillingWater, refillWaterProgress, remSec, isWatererReadyToCollect, "WATERER");
            }
        }

        private void CompleteWaterRefill()
        {
            isRefillingWater = false;
            refillWaterProgress = 1f;
            isWatererReadyToCollect = true;

            // Deduct proportional quota for the refilled amount
            float maxWater = PlantWaterer.Instance != null ? PlantWaterer.Instance.MaxWater : 100f;
            float waterDelta = Mathf.Max(0f, maxWater - watererStartAmount);
            float quotaUsed = maxWater > 0f ? (waterDelta / maxWater) : 1f;
            refillsUsedToday = Mathf.Clamp(refillsUsedToday + quotaUsed, 0f, maxRefillsPerDay);

            StopAudio();
            PlayAudioOneShot(chimeClip, 0.7f);

            SpawnFloatingAlert("💧 Plant Waterer in Well is full! Ready to take [E].");
            UpdatePromptLabel();
            UpdateVisuals();
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (isWatererReadyToCollect)
            {
                CollectFilledWaterer(interactor);
                return;
            }

            if (hasWatererInWell)
            {
                CancelAndRetrieveWaterer(interactor);
                return;
            }

            StartRefillingWaterer(interactor);
        }

        public bool StartRefillingWaterer(GameObject interactor)
        {
            if (hasWatererInWell) return false;

            float remainingQuota = Mathf.Max(0f, maxRefillsPerDay - refillsUsedToday);
            if (remainingQuota <= 0.02f)
            {
                SpawnFloatingAlert($"⚠️ Garden Well depleted for today! (0/{maxRefillsPerDay:F0} refills left)");
                return false;
            }

            var pInv = GetPlayerInventory(interactor);
            var watererItem = FindPlantWatererItem(pInv);
            if (watererItem == null)
            {
                SpawnFloatingAlert("⚠️ Requires Plant Waterer to refill! (Drinking bottle is refilled at Kitchen Sink)");
                return false;
            }

            var watererInstance = PlantWaterer.Instance;
            if (watererInstance != null && watererInstance.CurrentWater >= watererInstance.MaxWater)
            {
                SpawnFloatingAlert("💧 Plant Waterer is already full (100/100 L)!");
                return false;
            }

            // Remove 1 Plant Waterer tool from player inventory and place into the well station
            if (pInv != null)
            {
                pInv.RemoveItem(watererItem, 1);
            }

            watererStartAmount = watererInstance != null ? watererInstance.CurrentWater : 0f;
            hasWatererInWell = true;
            isRefillingWater = true;
            isWatererReadyToCollect = false;
            refillWaterProgress = 0f;

            UpdateVisuals();
            UpdatePromptLabel();
            StartTrickleAudio();

            SpawnFloatingAlert("💧 Plant Waterer placed in Well! Refilling (10s)...");
            return true;
        }

        public bool CancelAndRetrieveWaterer(GameObject interactor)
        {
            if (!hasWatererInWell || isWatererReadyToCollect) return false;

            var pInv = GetPlayerInventory(interactor);
            var watererItem = GetWatererItemData();
            if (pInv == null || watererItem == null) return false;

            // Attempt to return tool to inventory
            int added = pInv.AddItemAmount(watererItem, 1);
            if (added <= 0)
            {
                SpawnFloatingAlert("❌ Inventory full! Free space to retrieve Plant Waterer.");
                return false;
            }

            // Calculate water gained proportionally
            float maxWater = PlantWaterer.Instance != null ? PlantWaterer.Instance.MaxWater : 100f;
            float potentialGain = Mathf.Max(0f, maxWater - watererStartAmount);
            float waterGained = potentialGain * Mathf.Clamp01(refillWaterProgress);
            float newWaterAmount = Mathf.Clamp(watererStartAmount + waterGained, 0f, maxWater);

            if (PlantWaterer.Instance != null)
            {
                PlantWaterer.Instance.SetWater(newWaterAmount);
            }

            // Deduct proportional quota
            float quotaUsed = maxWater > 0f ? (waterGained / maxWater) : 0f;
            refillsUsedToday = Mathf.Clamp(refillsUsedToday + quotaUsed, 0f, maxRefillsPerDay);

            hasWatererInWell = false;
            isRefillingWater = false;
            isWatererReadyToCollect = false;
            refillWaterProgress = 0f;

            StopAudio();
            UpdateVisuals();
            UpdatePromptLabel();

            float remainingQuota = Mathf.Max(0f, maxRefillsPerDay - refillsUsedToday);
            SpawnFloatingAlert($"💧 Waterer retrieved early! ({Mathf.FloorToInt(newWaterAmount)}/100L | {remainingQuota:F1}/{maxRefillsPerDay:F0} refills left)");
            return true;
        }

        public bool CollectFilledWaterer(GameObject interactor)
        {
            if (!hasWatererInWell || !isWatererReadyToCollect) return false;

            var pInv = GetPlayerInventory(interactor);
            var watererItem = GetWatererItemData();
            if (pInv == null || watererItem == null) return false;

            int added = pInv.AddItemAmount(watererItem, 1);
            if (added <= 0)
            {
                SpawnFloatingAlert("❌ Inventory full! Free space to take Plant Waterer.");
                return false;
            }

            if (PlantWaterer.Instance != null)
            {
                PlantWaterer.Instance.RefillWater(100f);
            }

            hasWatererInWell = false;
            isWatererReadyToCollect = false;
            isRefillingWater = false;
            refillWaterProgress = 0f;

            PlayAudioOneShot(chimeClip, 0.7f);
            UpdateVisuals();
            UpdatePromptLabel();

            float remainingQuota = Mathf.Max(0f, maxRefillsPerDay - refillsUsedToday);
            SpawnFloatingAlert($"💧 Taken Full Plant Waterer (100/100 L)! ({remainingQuota:F1}/{maxRefillsPerDay:F0} refills left today)");
            return true;
        }

        private void UpdatePromptLabel()
        {
            if (worldLabel == null) return;

            float remainingQuota = Mathf.Max(0f, maxRefillsPerDay - refillsUsedToday);

            if (isWatererReadyToCollect)
            {
                worldLabel.displayName = "Garden Well [E: Take Full Plant Waterer]";
            }
            else if (isRefillingWater)
            {
                float remSec = Mathf.Max(0f, refillDuration * (1f - refillWaterProgress));
                worldLabel.displayName = $"Refilling Waterer... ({remSec:F0}s) [E: Cancel & Take]";
            }
            else if (hasWatererInWell)
            {
                worldLabel.displayName = "Garden Well [E: Retrieve Plant Waterer]";
            }
            else if (remainingQuota <= 0.02f)
            {
                worldLabel.displayName = $"{promptLabel} (Depleted: 0/{maxRefillsPerDay:F0})";
            }
            else
            {
                worldLabel.displayName = $"{promptLabel} ({remainingQuota:F1}/{maxRefillsPerDay:F0} Left) [E: Refill]";
            }
        }

        private void UpdateVisuals()
        {
            if (watererVisualGO != null)
            {
                watererVisualGO.SetActive(hasWatererInWell);
            }
        }

        /// <summary>
        /// Restores well state when loading a saved game.
        /// </summary>
        public void RestoreWellState(bool hasWaterer, bool ready, float progress, float startAmount, float refillsUsed)
        {
            hasWatererInWell = hasWaterer;
            isWatererReadyToCollect = ready;
            refillWaterProgress = Mathf.Clamp01(progress);
            watererStartAmount = startAmount;
            refillsUsedToday = Mathf.Max(0f, refillsUsed);
            isRefillingWater = hasWaterer && !ready && progress < 1f;

            UpdateVisuals();

            if (isRefillingWater)
                StartTrickleAudio();
            else
                StopAudio();

            UpdatePromptLabel();
        }

        private InventoryComponent GetPlayerInventory(GameObject interactor)
        {
            if (interactor != null)
            {
                var inv = interactor.GetComponent<InventoryComponent>();
                if (inv != null) return inv;
            }

            var pContext = ServiceLocator.Resolve<IPlayerContext>();
            if (pContext != null && pContext.Transform != null)
            {
                return pContext.Transform.GetComponent<InventoryComponent>();
            }

            return null;
        }

        private ItemData FindPlantWatererItem(InventoryComponent inv)
        {
            if (inv == null || inv.slots == null) return null;

            // 1. Check selected active hotbar slot first
            int activeIdx = inv.selectedHotbarIndex;
            if (activeIdx >= 0 && activeIdx < inv.slots.Count)
            {
                var slot = inv.slots[activeIdx];
                if (slot != null && !slot.IsEmpty && IsWatererItem(slot.item))
                {
                    return slot.item;
                }
            }

            // 2. Fallback to any inventory slot
            foreach (var slot in inv.slots)
            {
                if (slot != null && !slot.IsEmpty && IsWatererItem(slot.item))
                {
                    return slot.item;
                }
            }

            return null;
        }

        private bool IsWatererItem(ItemData item)
        {
            if (item == null) return false;
            string id = (item.itemId ?? "").ToLower();
            string name = (item.itemName ?? "").ToLower();
            return id == "tool_plant_waterer" || name.Contains("plant waterer") || name.Contains("watering can") || name.Contains("waterer");
        }

        private ItemData GetWatererItemData()
        {
            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            var item = db != null ? db.GetItem("tool_plant_waterer") : null;
            if (item == null)
            {
                var registry = Resources.Load<ItemRegistrySO>("ItemRegistrySO");
                if (registry != null) item = registry.GetItem("tool_plant_waterer");
            }
            return item;
        }

        private void SpawnFloatingAlert(string text)
        {
            var floatText = ServiceLocator.Resolve<IFloatingTextService>();
            if (floatText != null)
            {
                var player = ServiceLocator.Resolve<IPlayerContext>();
                Vector3 pos = player != null ? player.Transform.position + Vector3.up * 1.5f : transform.position + Vector3.up * 1.5f;
                floatText.SpawnText(pos, text, new Color(0.25f, 0.85f, 1f));
            }
        }

        // ── 3D Visual & Crank Setup ──

        private void FindCrankTransform()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child == null) continue;
                string n = child.name.ToLower();
                if (n.Contains("axleextension") || n.Contains("handle_right") || n.Contains("crank"))
                {
                    crankTransform = child;
                    break;
                }
            }
        }

        private void SetupWatererVisual()
        {
            var existing = transform.Find("WatererVisual_Station");
            if (existing != null)
            {
                watererVisualGO = existing.gameObject;
                watererVisualGO.SetActive(hasWatererInWell);
                return;
            }

            // Look for WaterTrough child
            Transform trough = transform.Find("WaterTrough");
            Transform parent = trough != null ? trough : transform;
            Vector3 localPos = trough != null ? new Vector3(0f, 0.45f, 0f) : new Vector3(0f, 0.45f, -1.85f);

            watererVisualGO = new GameObject("WatererVisual_Station");
            watererVisualGO.transform.SetParent(parent, false);
            watererVisualGO.transform.localPosition = localPos;
            watererVisualGO.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            // Construct low-poly stylized watering can representation
            // 1. Can Body (Cylinder)
            GameObject bodyGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bodyGO.name = "CanBody";
            bodyGO.transform.SetParent(watererVisualGO.transform, false);
            bodyGO.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            bodyGO.transform.localScale = new Vector3(0.32f, 0.16f, 0.32f);
            StripCollider(bodyGO);

            // 2. Can Spout (Angled cylinder)
            GameObject spoutGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spoutGO.name = "CanSpout";
            spoutGO.transform.SetParent(watererVisualGO.transform, false);
            spoutGO.transform.localPosition = new Vector3(0.22f, 0.22f, 0f);
            spoutGO.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            spoutGO.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            StripCollider(spoutGO);

            // 3. Can Handle (Arched bar)
            GameObject handleGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleGO.name = "CanHandle";
            handleGO.transform.SetParent(watererVisualGO.transform, false);
            handleGO.transform.localPosition = new Vector3(-0.16f, 0.22f, 0f);
            handleGO.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            handleGO.transform.localScale = new Vector3(0.06f, 0.22f, 0.06f);
            StripCollider(handleGO);

            // Style with metallic teal-copper appearance
            Material mat = null;
#if UNITY_EDITOR
            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Environment/Mat_Water_Well.mat");
#endif
            if (mat != null)
            {
                var rBody = bodyGO.GetComponent<Renderer>();
                var rSpout = spoutGO.GetComponent<Renderer>();
                var rHandle = handleGO.GetComponent<Renderer>();
                if (rBody != null) rBody.sharedMaterial = mat;
                if (rSpout != null) rSpout.sharedMaterial = mat;
                if (rHandle != null) rHandle.sharedMaterial = mat;
            }

            watererVisualGO.SetActive(hasWatererInWell);
        }

        private static void StripCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }

        // ── Procedural Audio Feedback ──

        private void EnsureProceduralAudioClips()
        {
            if (trickleClip == null) trickleClip = CreateProceduralTrickleClip();
            if (chimeClip == null) chimeClip = CreateProceduralChimeClip();
        }

        private void StartTrickleAudio()
        {
            EnsureProceduralAudioClips();
            if (audioSource != null && trickleClip != null)
            {
                audioSource.clip = trickleClip;
                audioSource.loop = true;
                audioSource.volume = sfxVolume * 0.7f;
                audioSource.Play();
            }
        }

        private void StopAudio()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private void PlayAudioOneShot(AudioClip clip, float volMultiplier = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, sfxVolume * volMultiplier);
            }
        }

        private static AudioClip CreateProceduralTrickleClip()
        {
            int sampleRate = 22050;
            float duration = 1.2f;
            int count = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[count];

            // Pink-noise / bubbly filtered water stream synthesis
            float b0 = 0f, b1 = 0f, b2 = 0f;
            System.Random rand = new System.Random(42);

            for (int i = 0; i < count; i++)
            {
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                b0 = 0.99765f * b0 + white * 0.0990460f;
                b1 = 0.96300f * b1 + white * 0.2965164f;
                b2 = 0.57000f * b2 + white * 1.0526913f;
                float pink = b0 + b1 + b2 + white * 0.1848f;
                pink *= 0.11f;

                // Gentle low-frequency water wobble
                float wobble = Mathf.Sin(2f * Mathf.PI * 4f * ((float)i / sampleRate));
                samples[i] = pink * (0.8f + 0.2f * wobble);
            }

            var clip = AudioClip.Create("ProceduralWaterTrickle", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateProceduralChimeClip()
        {
            int sampleRate = 22050;
            float duration = 0.5f;
            int count = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[count];

            // Harmonic chime (fundamental 784 Hz [G5] + 1174 Hz [D6])
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-7f * t); // Decaying bell envelope
                float wave = Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.6f +
                             Mathf.Sin(2f * Mathf.PI * 1174.66f * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * 1568f * t) * 0.15f;
                samples[i] = wave * env * 0.4f;
            }

            var clip = AudioClip.Create("ProceduralWellChime", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
