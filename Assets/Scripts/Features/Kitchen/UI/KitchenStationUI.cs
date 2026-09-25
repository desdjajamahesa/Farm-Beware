using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI listener stasiun dapur (Sink/Kompor) — MURNI visual:
/// subscribe ke event KitchenStation, tidak menyimpan state gameplay.
/// Panel disembunyikan saat idle dan hanya muncul selama proses berjalan.
/// </summary>
public class KitchenStationUI : MonoBehaviour
{
    [Tooltip("Stasiun yang dipantau (Sink / Kompor).")]
    [SerializeField] private KitchenStation station;

    [Tooltip("Panel root progress (CanvasGroup). Saat tidak memproses, panel disembunyikan.")]
    [SerializeField] private CanvasGroup panelGroup;

    [Tooltip("Baris progress (Image berjenis Filled).")]
    [SerializeField] private Image progressFill;

    [Tooltip("Teks status proses (opsional).")]
    [SerializeField] private Text statusText;

    [Tooltip("Lama tampil teks 'Selesai!' sebelum panel disembunyikan.")]
    [SerializeField] private float hideDelayAfterComplete = 1.2f;

    private int activeSlotIndex = -1;
    private Coroutine hideRoutine;

    private void OnEnable()
    {
        if (station != null)
        {
            station.OnProcessStarted += OnProcessStarted;
            station.OnProcessProgress += OnProcessProgress;
            station.OnProcessCompleted += OnProcessCompleted;
        }

        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();

        // Sembunyikan saat mulai (tidak ada proses berjalan).
        SetPanelVisible(false);
    }

    private void OnDisable()
    {
        if (station != null)
        {
            station.OnProcessStarted -= OnProcessStarted;
            station.OnProcessProgress -= OnProcessProgress;
            station.OnProcessCompleted -= OnProcessCompleted;
        }

        CancelHideRoutine();
    }

    private void OnProcessStarted(int slotIndex, float duration)
    {
        activeSlotIndex = slotIndex;
        CancelHideRoutine();
        SetPanelVisible(true);
        SetVisual(0f, "Memproses...");
    }

    private void OnProcessProgress(int slotIndex, float progress01)
    {
        if (slotIndex != activeSlotIndex)
            return;
        SetVisual(Mathf.Clamp01(progress01), null);
    }

    private void OnProcessCompleted(int slotIndex)
    {
        if (slotIndex != activeSlotIndex)
            return;

        activeSlotIndex = -1;
        SetVisual(1f, "Selesai!");
        CancelHideRoutine();
        hideRoutine = StartCoroutine(HideAfterDelayRoutine());
    }

    private IEnumerator HideAfterDelayRoutine()
    {
        yield return new WaitForSeconds(hideDelayAfterComplete);
        SetPanelVisible(false);
        SetVisual(0f, "");
        hideRoutine = null;
    }

    private void CancelHideRoutine()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    private void SetVisual(float amount01, string status)
    {
        if (progressFill != null)
            progressFill.fillAmount = amount01;

        if (statusText != null && status != null)
            statusText.text = status;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelGroup == null)
            return;

        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.blocksRaycasts = visible;
    }
}