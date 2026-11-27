using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual listener for Inventory 2 (Trophy Rack).
/// Purely event-driven: subscribes to rackInventory.OnInventoryChanged and
/// RENDERS/REMOVES 3D placeablePrefab models above SnapPoint anchors.
/// Holds no gameplay state and never modifies inventory data —
/// all mutations are handled by the backend (TrophySystemManager / InventoryComponent).
/// </summary>
public class TrophyRackVisuals : MonoBehaviour
{
    #region Fields & Properties

    [Tooltip("Inventori rak (Inventory 2) yang menjadi sumber data visual.")]
    [SerializeField] private InventoryComponent rackInventory;

    [Tooltip("Titik SnapPoint 3D (panjangnya harus sama dengan jumlah slot rack).")]
    [SerializeField] private Transform[] snapPoints;

    // rack slot index -> currently displayed 3D model (if any).
    private readonly Dictionary<int, GameObject> _spawnedModels = new Dictionary<int, GameObject>();

    // rack slot index -> ItemData of the currently rendered model (for swap detection).
    private readonly Dictionary<int, ItemData> _spawnedItemAt = new Dictionary<int, ItemData>();

    // Re-entrancy guard: OnInventoryChanged can fire nestedly during chained
    // transfer operations (AddItem -> RemoveItem -> transfer). Keep execution synchronous.
    private bool _refreshing;

    #endregion

    #region Lifecycle

    private void OnEnable()
    {
        if (rackInventory == null)
            return;

        rackInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (rackInventory != null)
            rackInventory.OnInventoryChanged -= Refresh;
    }

    private void OnDestroy()
    {
        DestroyAllModels();
    }

    #endregion

    #region Sync (Data-Driven Listener)

    /// <summary>
    /// Synchronizes all rack slots to 3D models. Idempotent & safe to call repeatedly.
    /// </summary>
    private void Refresh()
    {
        if (_refreshing || rackInventory == null)
            return;

        _refreshing = true;
        try
        {
            if (rackInventory.slots == null)
                return;

            for (int i = 0; i < rackInventory.slots.Count; i++)
                SyncSlot(i);
        }
        finally
        {
            _refreshing = false;
        }
    }

    /// <summary>
    /// Renders or removes the 3D model at a single slot index to match rack data.
    /// </summary>
    private void SyncSlot(int index)
    {
        InventorySlot slot = (index >= 0 && index < rackInventory.slots.Count) ? rackInventory.slots[index] : null;
        ItemData item = (slot != null && !slot.IsEmpty) ? slot.item : null;

        bool hasModel = _spawnedModels.TryGetValue(index, out GameObject model) && model != null;

        // Slot occupied by a world-placeable item -> ensure model is visible.
        if (item != null && item is TrophyItemData trophy)
        {
            // Model already exists for the SAME item -> nothing to do.
            if (hasModel && _spawnedItemAt.TryGetValue(index, out ItemData renderedItem) && renderedItem == item)
                return;

            // Different item (swap) or model missing -> destroy old & re-render.
            if (hasModel)
                DestroyModel(index);

            if (snapPoints == null || index >= snapPoints.Length || snapPoints[index] == null)
                return;

            GameObject created = CreateModel(trophy.placeablePrefab, snapPoints[index]);
            _spawnedModels[index] = created;
            _spawnedItemAt[index] = item;
            SetPlaceholder(index, false);
        }
        // Empty slot or non-placeable item -> destroy model (if any).
        else if (hasModel)
        {
            DestroyModel(index);
        }
    }

    /// <summary>
    /// Instantiates prefab as a child of the anchor point, preserving the prefab's original scale.
    /// </summary>
    private GameObject CreateModel(GameObject prefab, Transform anchor)
    {
        GameObject model = Instantiate(prefab, anchor);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        if (prefab.transform != null)
            model.transform.localScale = prefab.transform.localScale;
        return model;
    }

    private void DestroyModel(int index)
    {
        if (_spawnedModels.TryGetValue(index, out GameObject model))
        {
            if (model != null)
                Destroy(model);
            _spawnedModels.Remove(index);
            _spawnedItemAt.Remove(index);
            SetPlaceholder(index, true);
        }
    }

    private void DestroyAllModels()
    {
        foreach (GameObject model in _spawnedModels.Values)
        {
            if (model != null)
                Destroy(model);
        }
        _spawnedModels.Clear();
        _spawnedItemAt.Clear();
        SetAllPlaceholders(true);
    }

    #endregion


    private void SetPlaceholder(int index, bool visible)
    {
        if (snapPoints == null || index >= snapPoints.Length || snapPoints[index] == null)
            return;

        Transform ph = snapPoints[index].Find("PlaceholderCube");
        if (ph != null)
            ph.gameObject.SetActive(visible);
    }

    private void SetAllPlaceholders(bool visible)
    {
        if (snapPoints == null)
            return;

        for (int i = 0; i < snapPoints.Length; i++)
            SetPlaceholder(i, visible);
    }
}