using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual Listener untuk Inventory 2 (Trophy Rack).
/// Murni pendengar event: subscribe ke rackInventory.OnInventoryChanged lalu
/// MERENDER/MENGHAPUS model 3D placeablePrefab di atas titik-titik SnapPoint.
/// Tidak menyimpan state gameplay dan tidak pernah memodifikasi data inventory —
/// seluruh mutasi dilakukan backend (TrophySystemManager / InventoryComponent).
/// </summary>
public class TrophyRackVisuals : MonoBehaviour
{
    #region Fields & Properties

    [Tooltip("Inventori rak (Inventory 2) yang menjadi sumber data visual.")]
    [SerializeField] private InventoryComponent rackInventory;

    [Tooltip("Titik SnapPoint 3D (panjangnya harus sama dengan jumlah slot rack).")]
    [SerializeField] private Transform[] snapPoints;

    // index slot rak -> model 3D yang sedang tampil (jika ada).
    private readonly Dictionary<int, GameObject> _spawnedModels = new Dictionary<int, GameObject>();

    // index slot rak -> ItemData model sedang dirender (untuk deteksi pergantian item/swap).
    private readonly Dictionary<int, ItemData> _spawnedItemAt = new Dictionary<int, ItemData>();

    // Guard re-entrancy: OnInventoryChanged bisa terpanggil bertingkat saat operasi
    // transfer berantai (AddItem -> RemoveItem -> transfer). Jaga eksekusi sinkron.
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
    /// Sinkronisasi seluruh slot rak ke model 3D. Idempoten & aman dipanggil berulang.
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
    /// Render/hapus model 3D pada satu indeks slot agar selalu sesuai data rak.
    /// </summary>
    private void SyncSlot(int index)
    {
        InventorySlot slot = (index >= 0 && index < rackInventory.slots.Count) ? rackInventory.slots[index] : null;
        ItemData item = (slot != null && !slot.IsEmpty) ? slot.item : null;

        bool hasModel = _spawnedModels.TryGetValue(index, out GameObject model) && model != null;

        // Slot terisi item yang bisa ditempatkan di dunia -> pastikan model tampil.
        if (item != null && item.placeablePrefab != null)
        {
            // Model sudah ada untuk item yang SAMA -> tidak perlu apa-apa.
            if (hasModel && _spawnedItemAt.TryGetValue(index, out ItemData renderedItem) && renderedItem == item)
                return;

            // Item berbeda (swap) atau model belum ada -> hapus lama & render ulang.
            if (hasModel)
                DestroyModel(index);

            if (snapPoints == null || index >= snapPoints.Length || snapPoints[index] == null)
                return;

            GameObject created = CreateModel(item.placeablePrefab, snapPoints[index]);
            _spawnedModels[index] = created;
            _spawnedItemAt[index] = item;
        }
        // Slot kosong atau item tanpa model placeable -> hapus model (jika ada).
        else if (hasModel)
        {
            DestroyModel(index);
        }
    }

    /// <summary>
    /// Instantiate prefab sebagai child dari titik tempat, mempertahankan skala asli prefab.
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
    }

    #endregion
}