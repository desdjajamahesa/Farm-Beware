using FeaturesInteraction;
using FeaturesWardrobe;
using UnityEngine;

/// <summary>
/// Presenter feedback interaksi dunia:
/// 1) Label nama objek (di atas hotbar) via ItemDisplayUI.ShowWorldHover.
/// 2) Prompt aksi "E — Nama" via ItemDisplayUI.ShowInteractPrompt.
/// 3) Highlight objek (Highlightable) pada target yang sedang di-hover.
/// Membaca PlayerInteractor.CurrentTarget tiap frame. Murni listener UI — data-driven.
/// </summary>
public class HoverLabelController : MonoBehaviour
{
    [Tooltip("Interactor pemain yang menjadi sumber target hover.")]
    [SerializeField] private PlayerInteractor interactor;

    private string lastShownName;
    private bool isShowing;

    private GameObject lastTarget;
    private Highlightable lastHighlight;

    private void Awake()
    {
        if (interactor == null)
            interactor = GetComponent<PlayerInteractor>();
        this.enabled = true;
    }

    private void OnEnable()
    {
        lastShownName = null;
        isShowing = false;
        lastTarget = null;
        lastHighlight = null;
    }

    private void OnDisable()
    {
        ClearHighlight();
        HideIfShowing();
    }

    private void Update()
    {
        if (interactor == null)
        {
            ClearAll();
            return;
        }

        // Sembunyikan saat Trophy Mode (mode khusus rak piala).
        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
        {
            ClearAll();
            return;
        }

        // Sembunyikan saat Wardrobe Mode
        if (WardrobeManager.IsInWardrobeMode)
        {
            ClearAll();
            return;
        }

        // Sembunyikan saat player input terkunci (UI modal sedang terbuka: toko, masak, inventori, dll.)
        var pc = GetComponent<PlayerControl>();
        if (pc == null) pc = GetComponentInParent<PlayerControl>();
        if (pc != null && pc.isInputLocked)
        {
            ClearAll();
            return;
        }

        GameObject target = interactor.CurrentTarget;
        if (target == null)
        {
            ClearAll();
            return;
        }

        // Ganti highlight bila target berubah.
        if (target != lastTarget)
        {
            ClearHighlight();
            lastTarget = target;
            lastHighlight = target.GetComponent<Highlightable>() ?? target.GetComponentInChildren<Highlightable>();
            if (lastHighlight != null)
                lastHighlight.SetHighlight(true);
        }

        var farmland = target.GetComponent<FeaturesFarming.FarmlandTile>() ?? target.GetComponentInChildren<FeaturesFarming.FarmlandTile>();
        if (farmland != null && interactor != null)
        {
            farmland.UpdateLabelText(interactor.gameObject);
        }

        var door = target.GetComponent<DoorInteractable>() ?? target.GetComponentInChildren<DoorInteractable>();
        if (door != null)
        {
            door.UpdateDynamicLabel();
        }

        WorldLabel label = target.GetComponent<WorldLabel>() ?? target.GetComponentInChildren<WorldLabel>();
        string displayName = label != null ? label.GetDisplayName() : target.name;

        if (string.IsNullOrEmpty(displayName))
        {
            ClearAll();
            return;
        }

        Show(displayName);
    }

    private void Show(string name)
    {
        // Hindari SetActive ulang setiap frame (anti flicker).
        if (isShowing && lastShownName == name)
            return;

        lastShownName = name;
        isShowing = true;

        if (ItemDisplayUI.Instance != null)
        {
            ItemDisplayUI.Instance.ShowInteractPrompt(name);
        }
    }

    public void ClearAll()
    {
        ClearHighlight();
        HideIfShowing();
    }

    public void ClearHighlight()
    {
        if (lastHighlight != null)
        {
            lastHighlight.SetHighlight(false);
            lastHighlight = null;
        }
        lastTarget = null;
    }

    public void HideIfShowing()
    {
        if (!isShowing)
            return;

        isShowing = false;
        lastShownName = null;

        if (ItemDisplayUI.Instance != null)
        {
            ItemDisplayUI.Instance.HideInteractPrompt();
        }
    }
}