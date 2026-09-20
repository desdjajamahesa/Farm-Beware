using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesInteraction;

namespace FeaturesFarming
{
    public enum TileState
    {
        Untilled,        // Belum dicangkul (rumput/tanah mentah)
        Tilled,          // Sudah dicangkul (gembur kering), siap ditanam
        PlantedDry,      // Bibit tertanam, butuh disiram air
        PlantedWatered,  // Disiram air, timer pertumbuhan sedang berjalan
        ReadyToHarvest   // Tanaman matang rimbun, siap dipanen
    }

    /// <summary>
    /// Petak tanah kebun interaktif (IInteractable).
    /// Mengontrol alur siklus: Cangkul -> Tanam Benih -> Siram Air -> Panen Hasil Kotor (Dirty Crop).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(WorldLabel))]
    [RequireComponent(typeof(Highlightable))]
    public class FarmlandTile : MonoBehaviour, IInteractable
    {
        [Header("State Petak Tanah")]
        [SerializeField] private TileState currentState = TileState.Untilled;
        public TileState CurrentState => currentState;

        [Header("Data Tanaman")]
        [SerializeField] private SeedItemData plantedSeed;
        public SeedItemData PlantedSeed => plantedSeed;

        [SerializeField] private float growthProgress = 0f;
        public float GrowthProgress => growthProgress;

        [Tooltip("Durasi pertumbuhan tanaman dalam detik (diambil dari SeedItemData bila tersedia).")]
        [SerializeField] private float growthDuration = 30f;
        [SerializeField] private float currentTimer = 0f;

        [Header("Hasil Panen")]
        [Tooltip("Jika dicentang, panen otomatis mengutamakan varian kotor (Dirty) agar harus dicuci di wastafel.")]
        [SerializeField] private bool yieldDirtyVariant = true;

        [Header("Komponen Pendukung")]
        [SerializeField] private CropVisualController visualController;
        [SerializeField] private WorldLabel worldLabel;
        [SerializeField] private Highlightable highlightable;
        [SerializeField] private CropGrowthCountdownUI countdownUI;

        private bool isPlantingAction = false;
        private bool isHarvestingAction = false;

        private void Awake()
        {
            if (visualController == null)
                visualController = GetComponent<CropVisualController>();
            if (worldLabel == null)
                worldLabel = GetComponent<WorldLabel>();
            if (highlightable == null)
                highlightable = GetComponent<Highlightable>();
            if (countdownUI == null)
                countdownUI = GetComponent<CropGrowthCountdownUI>();
            if (countdownUI == null)
                countdownUI = gameObject.AddComponent<CropGrowthCountdownUI>();

            EnsureCollider();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(TimeManager.DayPhase newPhase)
        {
            UpdateLabelText(null);
        }

        private void Start()
        {
            UpdateVisuals();
            UpdateLabelText(null);
            UpdateCountdownUI();
        }

        private int lastVisualStage = -1;

        private void Update()
        {
            // 1. Logika pertumbuhan jika tanah dalam kondisi tersiram air
            if (currentState == TileState.PlantedWatered && plantedSeed != null)
            {
                currentTimer += Time.deltaTime;
                growthProgress = Mathf.Clamp01(currentTimer / Mathf.Max(1f, growthDuration));

                // Perbarui visual HANYA saat berpindah tahap pertumbuhan (hemat CPU/GC 99.9%)
                int currentStage = (growthProgress >= 1f) ? 2 : (growthProgress >= 0.5f ? 1 : 0);
                if (currentStage != lastVisualStage)
                {
                    lastVisualStage = currentStage;
                    if (visualController != null)
                        visualController.UpdateVisuals(currentState, plantedSeed, growthProgress);
                }

                // Perbarui UI countdown lingkaran di atas tanaman
                if (countdownUI != null)
                {
                    float remainingSec = Mathf.Max(0f, growthDuration - currentTimer);
                    countdownUI.UpdateTileState(currentState, growthProgress, remainingSec, plantedSeed.itemName);
                }

                if (currentTimer >= growthDuration)
                {
                    growthProgress = 1f;
                    SetState(TileState.ReadyToHarvest);
                }
            }

            // 2. Debug shortcut: Tekan tombol H untuk mempercepat panen secara instan (membantu testing)
            if (Application.isPlaying && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (currentState == TileState.PlantedDry || currentState == TileState.PlantedWatered)
                {
                    ForceInstantHarvestReady();
                }
            }
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;

            // Aturan gameplay: Bertani hanya dapat dilakukan saat siang hari (Day Phase).
            if (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                Debug.LogWarning("[FarmlandTile] Hanya bisa bertani di siang hari!");
                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 1.2f,
                        "Hanya bisa bertani di siang hari!",
                        new Color(1f, 0.4f, 0.4f));
                }
                return;
            }

            InventoryComponent playerInventory = interactor.GetComponent<InventoryComponent>();
            PlayerControl playerControl = interactor.GetComponent<PlayerControl>();

            switch (currentState)
            {
                case TileState.Untilled:
                    TillSoil();
                    break;

                case TileState.Tilled:
                    TryPlantSeed(playerInventory, playerControl);
                    break;

                case TileState.PlantedDry:
                    WaterCrop();
                    break;

                case TileState.PlantedWatered:
                    float remainingSec = Mathf.Max(0f, growthDuration - currentTimer);
                    Debug.Log($"[FarmlandTile] Crop is growing... ({Mathf.CeilToInt(remainingSec)}s remaining)");
                    break;

                case TileState.ReadyToHarvest:
                    HarvestCrop(playerInventory, playerControl);
                    break;
            }

            UpdateLabelText(interactor);
        }

        private void TillSoil()
        {
            SetState(TileState.Tilled);
            Debug.Log("[FarmlandTile] Soil tilled. Ready for seeds.");
        }

        private void TryPlantSeed(InventoryComponent inventory, PlayerControl playerControl)
        {
            if (inventory == null || isPlantingAction) return;

            int hotbarIdx = inventory.selectedHotbarIndex;
            InventorySlot activeSlot = (hotbarIdx >= 0 && hotbarIdx < inventory.slots.Count) ? inventory.slots[hotbarIdx] : null;

            if (activeSlot == null || activeSlot.IsEmpty || activeSlot.item is not SeedItemData seed)
            {
                Debug.LogWarning("[FarmlandTile] Please select a seed (Sweet Potato / Taro Seed) in your active hotbar slot!");
                return;
            }

            StartCoroutine(RoutinePlantSeedToTile(inventory, seed, playerControl));
        }

        private IEnumerator RoutinePlantSeedToTile(InventoryComponent inventory, SeedItemData seed, PlayerControl playerControl)
        {
            isPlantingAction = true;

            if (playerControl != null)
            {
                playerControl.TriggerPlantAnimation();
            }

            // Sinkronkan pemunculan bibit saat tangan pemain membungkuk ke tanah (~0.5s)
            yield return new WaitForSeconds(0.5f);

            if (inventory != null && seed != null)
            {
                inventory.RemoveItem(seed, 1);
                plantedSeed = seed;
                growthDuration = seed.growthDuration > 0f ? seed.growthDuration : 30f;
                currentTimer = 0f;
                growthProgress = 0f;

                SetState(TileState.PlantedDry);
                Debug.Log($"[FarmlandTile] Successfully planted {seed.itemName}. Requires watering!");
            }

            isPlantingAction = false;
        }

        private void WaterCrop()
        {
            SetState(TileState.PlantedWatered);
            Debug.Log("[FarmlandTile] Crop watered! Growth countdown started.");
        }

        private void HarvestCrop(InventoryComponent inventory, PlayerControl playerControl)
        {
            if (plantedSeed == null || inventory == null || isHarvestingAction) return;
            StartCoroutine(RoutineHarvestCrop(inventory, playerControl));
        }

        private IEnumerator RoutineHarvestCrop(InventoryComponent inventory, PlayerControl playerControl)
        {
            isHarvestingAction = true;

            if (playerControl != null)
            {
                playerControl.TriggerHarvestAnimation();
            }

            // Tunggu ~0.6 detik saat tangan pemain meraih tanaman di tanah
            yield return new WaitForSeconds(0.6f);

            if (plantedSeed != null)
            {
                ItemData dropItem = plantedSeed.cropYield;

                if (yieldDirtyVariant)
                {
                    ItemData dirtyVariant = ResolveDirtyVariant(dropItem);
                    if (dirtyVariant != null)
                    {
                        dropItem = dirtyVariant;
                    }
                }

                if (dropItem != null)
                {
                    int yieldCount = Random.Range(plantedSeed.minYield, plantedSeed.maxYield + 1);
                    if (yieldCount < 1) yieldCount = 1;

                    bool added = inventory.AddItem(dropItem, yieldCount);
                    if (added)
                    {
                        Debug.Log($"[FarmlandTile] Harvest successful! Obtained {yieldCount}x {dropItem.itemName}.");
                        if (PlayerUI.FloatingCombatTextManager.Instance != null && playerControl != null)
                        {
                            PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                                transform.position + Vector3.up * 1.2f,
                                $"+{yieldCount} {dropItem.itemName}",
                                new Color(0.25f, 0.90f, 0.35f));
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FarmlandTile] Inventory full! Could not collect harvest.");
                    }
                }
            }

            // Reset petak kembali ke kondisi Tilled
            plantedSeed = null;
            currentTimer = 0f;
            growthProgress = 0f;
            SetState(TileState.Tilled);

            isHarvestingAction = false;
        }

        private ItemData ResolveDirtyVariant(ItemData cleanOrDirtyItem)
        {
            if (cleanOrDirtyItem == null) return null;

            // Jika item sudah berstatus kotor, langsung gunakan
            if (cleanOrDirtyItem.isDirty) return cleanOrDirtyItem;

            // Jika clean, coba cari varian _dirty dari database
            var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db != null)
            {
                var found = db.GetItem(cleanOrDirtyItem.itemId + "_dirty");
                if (found != null) return found;
            }

            return cleanOrDirtyItem;
        }

        public void SetState(TileState newState)
        {
            currentState = newState;
            lastVisualStage = -1;
            UpdateVisuals();
            UpdateLabelText(null);
            UpdateCountdownUI();
        }

        private void UpdateCountdownUI()
        {
            if (countdownUI != null)
            {
                float remainingSec = Mathf.Max(0f, growthDuration - currentTimer);
                countdownUI.UpdateTileState(currentState, growthProgress, remainingSec, plantedSeed != null ? plantedSeed.itemName : "");
            }
        }

        public void ForceInstantHarvestReady()
        {
            if (plantedSeed == null) return;
            currentTimer = growthDuration;
            growthProgress = 1f;
            SetState(TileState.ReadyToHarvest);
            Debug.Log("[FarmlandTile] [DEBUG] Crop forced to ready for harvest.");
        }

        /// <summary>
        /// Dipanggil saat transisi hari baru (AdvanceToNextDay) oleh TimeManager / FarmingManager.
        /// </summary>
        public void AdvanceDay()
        {
            if (currentState == TileState.PlantedWatered)
            {
                // Hari berganti setelah disiram -> langsung matang atau maju drastis
                currentTimer += growthDuration;
                growthProgress = 1f;
                SetState(TileState.ReadyToHarvest);
            }
            else if (currentState == TileState.PlantedDry)
            {
                // Belum disiram, tidak bertumbuh
                Debug.Log("[FarmlandTile] Dry crops did not grow overnight because they were not watered.");
            }
        }

        public void UpdateLabelText(GameObject interactor)
        {
            if (worldLabel == null) return;

            // Saat malam hari, tampilkan keterangan bahwa aktivitas bertani sedang tidak diizinkan
            if (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                worldLabel.displayName = "🌙 Hanya bisa bertani di siang hari";
                return;
            }

            switch (currentState)
            {
                case TileState.Untilled:
                    worldLabel.displayName = "Till Soil";
                    break;

                case TileState.Tilled:
                    SeedItemData heldSeed = GetHeldSeed(interactor);
                    if (heldSeed != null)
                        worldLabel.displayName = $"Plant {heldSeed.itemName}";
                    else
                        worldLabel.displayName = "Tilled Soil (Select Seed in Hotbar)";
                    break;

                case TileState.PlantedDry:
                    worldLabel.displayName = $"Water {plantedSeed?.itemName ?? "Crop"}";
                    break;

                case TileState.PlantedWatered:
                    float remaining = Mathf.Max(0f, growthDuration - currentTimer);
                    worldLabel.displayName = $"{plantedSeed?.itemName ?? "Crop"} ({Mathf.CeilToInt(remaining)}s)";
                    break;

                case TileState.ReadyToHarvest:
                    worldLabel.displayName = $"Harvest {plantedSeed?.itemName ?? "Crop"}!";
                    break;
            }
        }

        private SeedItemData GetHeldSeed(GameObject interactor)
        {
            if (interactor == null)
            {
                // Fallback: cari player aktif di scene
                var player = FindFirstObjectByType<PlayerControl>();
                if (player != null) interactor = player.gameObject;
            }

            if (interactor == null) return null;
            var inv = interactor.GetComponent<InventoryComponent>();
            if (inv == null) return null;

            int idx = inv.selectedHotbarIndex;
            if (idx >= 0 && idx < inv.slots.Count)
            {
                var slot = inv.slots[idx];
                if (slot != null && !slot.IsEmpty && slot.item is SeedItemData seed)
                    return seed;
            }
            return null;
        }

        private void UpdateVisuals()
        {
            if (visualController != null)
            {
                visualController.UpdateVisuals(currentState, plantedSeed, growthProgress);
            }
        }

        private void EnsureCollider()
        {
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = false;
                // Ukuran collider petak tanah: 1.2 x 0.4 x 1.2
                if (col.size == Vector3.one)
                {
                    col.size = new Vector3(1.2f, 0.4f, 1.2f);
                    col.center = new Vector3(0f, 0.2f, 0f);
                }
            }
        }
    }
}
