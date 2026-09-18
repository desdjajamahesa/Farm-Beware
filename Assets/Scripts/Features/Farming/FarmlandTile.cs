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

        private void Awake()
        {
            if (visualController == null)
                visualController = GetComponent<CropVisualController>();
            if (worldLabel == null)
                worldLabel = GetComponent<WorldLabel>();
            if (highlightable == null)
                highlightable = GetComponent<Highlightable>();

            EnsureCollider();
        }

        private void Start()
        {
            UpdateVisuals();
            UpdateLabelText(null);
        }

        private void Update()
        {
            // 1. Logika pertumbuhan jika tanah dalam kondisi tersiram air
            if (currentState == TileState.PlantedWatered && plantedSeed != null)
            {
                currentTimer += Time.deltaTime;
                growthProgress = Mathf.Clamp01(currentTimer / Mathf.Max(1f, growthDuration));

                // Perbarui visual secara berkala
                if (visualController != null)
                    visualController.UpdateVisuals(currentState, plantedSeed, growthProgress);

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
            InventoryComponent playerInventory = interactor.GetComponent<InventoryComponent>();
            PlayerControl playerControl = interactor.GetComponent<PlayerControl>();

            switch (currentState)
            {
                case TileState.Untilled:
                    if (IsHoldingHoe(interactor))
                    {
                        TillSoil();
                    }
                    else
                    {
                        Debug.LogWarning("[FarmlandTile] Harap pegang Cangkul di slot hotbar aktif untuk mencangkul tanah!");
                        if (ItemDisplayUI.Instance != null)
                        {
                            ItemDisplayUI.Instance.ShowHotbarPopup("Butuh Cangkul di Hotbar!");
                        }
                    }
                    break;

                case TileState.Tilled:
                    TryPlantSeed(playerInventory, playerControl);
                    break;

                case TileState.PlantedDry:
                    WaterCrop();
                    break;

                case TileState.PlantedWatered:
                    // Sudah tersiram, berikan info status
                    float remainingSec = Mathf.Max(0f, growthDuration - currentTimer);
                    Debug.Log($"[FarmlandTile] Tanaman sedang bertumbuh... ({Mathf.CeilToInt(remainingSec)}s tersisa)");
                    break;

                case TileState.ReadyToHarvest:
                    HarvestCrop(playerInventory);
                    break;
            }

            UpdateLabelText(interactor);
        }

        private void TillSoil()
        {
            SetState(TileState.Tilled);
            Debug.Log("[FarmlandTile] Tanah berhasil dicangkul. Siap untuk ditanami benih.");
        }

        private void TryPlantSeed(InventoryComponent inventory, PlayerControl playerControl)
        {
            if (inventory == null) return;

            // Cek slot hotbar aktif pemain
            int hotbarIdx = inventory.selectedHotbarIndex;
            InventorySlot activeSlot = (hotbarIdx >= 0 && hotbarIdx < inventory.slots.Count) ? inventory.slots[hotbarIdx] : null;

            if (activeSlot == null || activeSlot.IsEmpty || activeSlot.item is not SeedItemData seed)
            {
                Debug.LogWarning("[FarmlandTile] Harap pilih benih tanaman (Sweet Potato / Taro Seed) pada slot hotbar aktif pemain!");
                return;
            }

            // Mainkan animasi menanam jika player control tersedia
            if (playerControl != null)
            {
                playerControl.TriggerPlantAnimation();
            }

            // Kurangi 1 kuantiti benih dari inventori
            inventory.RemoveItem(seed, 1);

            // Simpan bibit yang ditanam
            plantedSeed = seed;
            growthDuration = seed.growthDuration > 0f ? seed.growthDuration : 30f;
            currentTimer = 0f;
            growthProgress = 0f;

            SetState(TileState.PlantedDry);
            Debug.Log($"[FarmlandTile] Berhasil menanam {seed.itemName}. Petak butuh disiram air!");
        }

        private void WaterCrop()
        {
            SetState(TileState.PlantedWatered);
            Debug.Log("[FarmlandTile] Tanaman berhasil disiram! Pertumbuhan dimulai.");
        }

        private void HarvestCrop(InventoryComponent inventory)
        {
            if (plantedSeed == null || inventory == null) return;

            ItemData dropItem = plantedSeed.cropYield;

            // Jika mengutamakan varian kotor (Dirty Crop) untuk siklus cuci di wastafel:
            if (yieldDirtyVariant)
            {
                ItemData dirtyVariant = ResolveDirtyVariant(dropItem);
                if (dirtyVariant != null)
                {
                    dropItem = dirtyVariant;
                }
            }

            if (dropItem == null)
            {
                Debug.LogError("[FarmlandTile] CropYield pada benih ini bernilai null!");
                return;
            }

            int yieldCount = Random.Range(plantedSeed.minYield, plantedSeed.maxYield + 1);
            if (yieldCount < 1) yieldCount = 1;

            bool added = inventory.AddItem(dropItem, yieldCount);
            if (added)
            {
                Debug.Log($"[FarmlandTile] Panen berhasil! Mendapatkan {yieldCount}x {dropItem.itemName}. Bawa ke wastafel dapur untuk dicuci.");
            }
            else
            {
                Debug.LogWarning($"[FarmlandTile] Inventori penuh! Sebagian hasil panen tidak dapat masuk.");
            }

            // Reset petak kembali ke kondisi Tilled
            plantedSeed = null;
            currentTimer = 0f;
            growthProgress = 0f;
            SetState(TileState.Tilled);
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
            UpdateVisuals();
            UpdateLabelText(null);
        }

        public void ForceInstantHarvestReady()
        {
            if (plantedSeed == null) return;
            currentTimer = growthDuration;
            growthProgress = 1f;
            SetState(TileState.ReadyToHarvest);
            Debug.Log("[FarmlandTile] [DEBUG] Tanaman dipaksa matang seketika (Ready to Harvest).");
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
                Debug.Log("[FarmlandTile] Tanaman kering tidak bertumbuh semalam karena belum disiram.");
            }
        }

        public void UpdateLabelText(GameObject interactor)
        {
            if (worldLabel == null) return;

            switch (currentState)
            {
                case TileState.Untilled:
                    if (IsHoldingHoe(interactor))
                        worldLabel.displayName = "Cangkul Tanah";
                    else
                        worldLabel.displayName = "Tanah Liar (Butuh Cangkul)";
                    break;

                case TileState.Tilled:
                    SeedItemData heldSeed = GetHeldSeed(interactor);
                    if (heldSeed != null)
                        worldLabel.displayName = $"Tanam {heldSeed.itemName}";
                    else
                        worldLabel.displayName = "Petak Bersih (Pilih Benih di Hotbar)";
                    break;

                case TileState.PlantedDry:
                    worldLabel.displayName = $"Siram {plantedSeed?.itemName ?? "Tanaman"}";
                    break;

                case TileState.PlantedWatered:
                    float remaining = Mathf.Max(0f, growthDuration - currentTimer);
                    worldLabel.displayName = $"{plantedSeed?.itemName ?? "Tanaman"} ({Mathf.CeilToInt(remaining)}s)";
                    break;

                case TileState.ReadyToHarvest:
                    worldLabel.displayName = $"Panen {plantedSeed?.itemName ?? "Tanaman"}!";
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

        private bool IsHoldingHoe(GameObject interactor)
        {
            if (interactor == null)
            {
                // Fallback: cari player aktif di scene
                var player = FindFirstObjectByType<PlayerControl>();
                if (player != null) interactor = player.gameObject;
            }

            if (interactor == null) return false;
            var inv = interactor.GetComponent<InventoryComponent>();
            if (inv == null) return false;

            int idx = inv.selectedHotbarIndex;
            if (idx >= 0 && idx < inv.slots.Count)
            {
                var slot = inv.slots[idx];
                if (slot != null && !slot.IsEmpty && slot.item != null)
                {
                    if (slot.item is ToolItemData tool && tool.isHoe)
                        return true;
                    if (slot.item.itemId == "tool_hoe" || slot.item.name.ToLower().Contains("cangkul") || slot.item.name.ToLower().Contains("hoe"))
                        return true;
                }
            }
            return false;
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
