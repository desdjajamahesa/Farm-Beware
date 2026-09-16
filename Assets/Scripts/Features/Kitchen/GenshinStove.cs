using System;
using System.Collections;
using UnityEngine;
using FeaturesInteraction;

/// <summary>
/// Kompor gaya Genshin Impact: tidak ada slot drag-drop.
/// Saat interaksi (E key), membuka UI buku resep di tengah layar.
/// Bahan dicek dari inventory pemain, hasil langsung masuk inventory pemain.
/// Implements IKitchenStationEvents untuk dukung SoundFx & ProgressOverlay.
/// </summary>
public class GenshinStove : MonoBehaviour, IInteractable, IKitchenStationEvents
{
    [Header("Resep Masak")]
    [Tooltip("Semua resep yang tersedia di kompor ini.")]
    [SerializeField] private KitchenRecipe[] recipes;

    [Header("UI Reference")]
    [Tooltip("StoveUIManager yang mengontrol Panel_Stove. Jika kosong, cari di scene.")]
    [SerializeField] private StoveUIManager stoveUI;

    // IKitchenStationEvents — single-slot model (slot 0)
    public event Action<int, float> OnProcessStarted;
    public event Action<int, float> OnProcessProgress;
    public event Action<int> OnProcessCompleted;
    public event Action<int> OnProcessCancelled;

    private bool isProcessing;
    private float processProgress;
    private float processDuration;

    private void Awake()
    {
        if (stoveUI == null)
            stoveUI = FindFirstObjectByType<StoveUIManager>();
    }

    public void Interact(GameObject interactor)
    {
        if (stoveUI == null)
        {
            Debug.LogWarning("[GenshinStove] StoveUIManager tidak ditemukan!");
            return;
        }

        InventoryComponent playerInv = interactor.GetComponent<InventoryComponent>();
        if (playerInv == null)
        {
            Debug.LogWarning("[GenshinStove] Player tidak punya InventoryComponent!");
            return;
        }

        // Safety: force-unlock stale input lock if no panel is active
        var pc = interactor.GetComponent<PlayerControl>();
        if (pc != null && pc.isInputLocked)
        {
            bool stoveOpen = stoveUI.gameObject != null && stoveUI.gameObject.activeSelf;
            if (!stoveOpen)
            {
                pc.isInputLocked = false;
                Debug.Log("[GenshinStove] Safety: force-unlocked stale isInputLocked");
            }
        }

        stoveUI.Open(recipes, playerInv);
    }

    // ── Public API: dipanggil oleh StoveUIManager setelah konsumsi bahan ──

    /// <summary>
    /// Mulai proses masak dengan durasi tertentu. Dipanggil oleh StoveUIManager.
    /// </summary>
    public void BeginCooking(float duration)
    {
        processDuration = duration > 0f ? duration : 1f;
        isProcessing = true;
        processProgress = 0f;

        OnProcessStarted?.Invoke(0, processDuration);
        OnProcessProgress?.Invoke(0, 0f);

        StartCoroutine(CookProgressRoutine());
    }

    /// <summary>
    /// Selesaikan proses masak. Dipanggil oleh StoveUIManager.
    /// </summary>
    public void FinishCooking()
    {
        if (!isProcessing) return;

        isProcessing = false;
        processProgress = 1f;

        OnProcessCompleted?.Invoke(0);
    }

    /// <summary>
    /// Batalkan proses masak.
    /// </summary>
    public void CancelCooking()
    {
        if (!isProcessing) return;

        StopAllCoroutines();
        isProcessing = false;
        processProgress = 0f;

        OnProcessCancelled?.Invoke(0);
    }

    // ── IKitchenStationEvents (read-only state) ──

    public bool IsProcessing(int slotIndex) => isProcessing && slotIndex == 0;

    public float GetSlotProgress(int slotIndex) => (slotIndex == 0) ? processProgress : 0f;

    // ── Internal ──

    private IEnumerator CookProgressRoutine()
    {
        float elapsed = 0f;

        while (elapsed < processDuration)
        {
            elapsed += Time.deltaTime;
            processProgress = Mathf.Clamp01(elapsed / processDuration);
            OnProcessProgress?.Invoke(0, processProgress);
            yield return null;
        }

        processProgress = 1f;
        OnProcessProgress?.Invoke(0, 1f);
    }
}
