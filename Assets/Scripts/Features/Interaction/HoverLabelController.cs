using FeaturesInteraction;
using UnityEngine;
using FarmBeware.Core.Runtime;

/// <summary>
/// World interaction feedback presenter:
/// 1) Object name label (above hotbar) via ItemDisplayUI.ShowWorldHover.
/// 2) Action prompt "E — Name" via ItemDisplayUI.ShowInteractPrompt.
/// 3) Object highlight (Highlightable) on the currently hovered target.
/// Reads PlayerInteractor.CurrentTarget every frame. Pure UI listener — data-driven.
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

        // Hide during non-gameplay modes (Trophy Mode, Wardrobe Mode)
        var camService = ServiceLocator.Resolve<ICameraService>();
        if (camService != null && (camService.CurrentMode == CameraMode.TrophyMode || camService.CurrentMode == CameraMode.WardrobeMode))
        {
            ClearAll();
            return;
        }

        // Hide when player input is locked (UI modal is open: shop, cooking, inventory, etc.)
        var pc = GetComponent<IPlayerContext>() ?? GetComponentInParent<IPlayerContext>();
        if (pc != null && pc.IsInputLocked)
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

        // Switch highlight when target changes.
        if (target != lastTarget)
        {
            ClearHighlight();
            lastTarget = target;
            lastHighlight = target.GetComponent<Highlightable>() ?? target.GetComponentInChildren<Highlightable>();
            if (lastHighlight != null)
                lastHighlight.SetHighlight(true);
        }

        var dynamicLabel = target.GetComponent<IDynamicLabelProvider>() ?? target.GetComponentInChildren<IDynamicLabelProvider>();
        if (dynamicLabel != null)
        {
            dynamicLabel.UpdateDynamicLabel(interactor != null ? interactor.gameObject : null);
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
        // Avoid calling SetActive repeatedly every frame (prevents flicker).
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