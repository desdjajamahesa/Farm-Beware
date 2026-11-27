using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Singleton UI presenter for displaying item info and world-space tooltips:
// 1) Hotbar item name popup with smooth fade when switching selected slot.
// 2) Cursor-following tooltip when hovering inventory items.
// 3) Interaction prompt banner with [E] key badge and modern text formatting.
public class ItemDisplayUI : MonoBehaviour
{
    public static ItemDisplayUI Instance { get; private set; }

    [Header("Legacy Text References")]
    public Text hotbarPopupText;
    public Text mouseTooltipText;
    public Text worldHoverText;
    public Text interactPromptText;

    [Header("Modern TMP Interaction Prompt Banner")]
    public GameObject interactPromptRoot;
    public TextMeshProUGUI interactPromptTMP;
    public TextMeshProUGUI interactKeyTMP;
    public CanvasGroup interactPromptCanvasGroup;

    [Header("Modern TMP Hotbar Popup Banner")]
    public GameObject hotbarPopupRoot;
    public TextMeshProUGUI hotbarPopupTMP;
    public CanvasGroup hotbarPopupCanvasGroup;

    // Tooltip position offset from cursor (configurable in Inspector).
    // Default (25, 65): tooltip appears above and to the right of cursor to avoid obscuring pointer.
    [SerializeField] private Vector2 tooltipOffset = new Vector2(25f, 65f);

    private Coroutine hideHotbarCoroutine;
    private RectTransform tooltipBg;
    private RectTransform interactPromptBg;
    private CanvasGroup legacyHotbarCanvasGroup;

    void Awake()
    {
        Instance = this;

        SanitizeText(hotbarPopupText);
        SanitizeText(mouseTooltipText);
        SanitizeText(worldHoverText);
        SanitizeText(interactPromptText);

        if (interactPromptRoot != null)
            interactPromptRoot.SetActive(false);
        else if (interactPromptTMP != null)
            interactPromptTMP.gameObject.SetActive(false);

        if (hotbarPopupRoot != null)
            hotbarPopupRoot.SetActive(false);
        else if (hotbarPopupTMP != null)
            hotbarPopupTMP.gameObject.SetActive(false);

        if (mouseTooltipText != null)
            BuildTooltipBackground();

        if (interactPromptText != null)
            BuildInteractPromptBackground();

        if (hotbarPopupText != null)
        {
            legacyHotbarCanvasGroup = hotbarPopupText.GetComponent<CanvasGroup>();
            if (legacyHotbarCanvasGroup == null)
                legacyHotbarCanvasGroup = hotbarPopupText.gameObject.AddComponent<CanvasGroup>();
            legacyHotbarCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // Only move the tooltip while it is visible.
        if (mouseTooltipText == null || !mouseTooltipText.gameObject.activeSelf)
            return;

        if (Mouse.current == null)
            return;

        // Hardening: never move the hotbar popup text,
        // whether due to shared Text reference or identical RectTransform.
        RectTransform tooltipRect = mouseTooltipText.rectTransform;
        if (hotbarPopupText != null &&
            (mouseTooltipText == hotbarPopupText || tooltipRect == hotbarPopupText.rectTransform))
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
            return;

        // Convert cursor position (screen space) to local canvas coordinates,
        // then apply to the tooltip RectTransform (direct canvas child,
        // anchor 0.5/0.5) so it tracks the cursor safely.
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Mouse.current.position.ReadValue(),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint))
            return;

        Vector2 target = localPoint + tooltipOffset;
        tooltipRect.anchoredPosition = target;

        // Background box moves along behind the tooltip (same canvas).
        if (tooltipBg != null)
            tooltipBg.anchoredPosition = target;
    }

    public void ShowHover(string itemName)
    {
        if (mouseTooltipText == null)
            return;

        mouseTooltipText.text = itemName;
        mouseTooltipText.gameObject.SetActive(true);
        if (tooltipBg != null)
            tooltipBg.gameObject.SetActive(true);
    }

    public void HideHover()
    {
        if (mouseTooltipText == null)
            return;

        mouseTooltipText.gameObject.SetActive(false);
        if (tooltipBg != null)
            tooltipBg.gameObject.SetActive(false);
    }

    // World object name label: disabled to prevent duplicate text above hotbar.
    public void ShowWorldHover(string name)
    {
        // Safe no-op. If a legacy worldHoverText remains in the scene, ensure it stays inactive.
        if (worldHoverText != null && worldHoverText.gameObject.activeSelf)
        {
            worldHoverText.text = "";
            worldHoverText.gameObject.SetActive(false);
        }
    }

    public void HideWorldHover()
    {
        if (worldHoverText != null)
        {
            worldHoverText.text = "";
            worldHoverText.gameObject.SetActive(false);
        }
    }

    // Action prompt "E — Name" when an interactive object is hovered.
    public void ShowInteractPrompt(string displayName)
    {
        if (worldHoverText != null)
        {
            worldHoverText.gameObject.SetActive(false);
        }

        string formatted = FormatPromptText(displayName);

        if (interactPromptTMP != null)
        {
            if (interactKeyTMP != null)
                interactKeyTMP.text = "E";

            interactPromptTMP.text = formatted;

            if (interactPromptRoot != null)
            {
                interactPromptRoot.SetActive(true);
                var rect = interactPromptRoot.GetComponent<RectTransform>();
                if (rect != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
            }
            else
            {
                interactPromptTMP.gameObject.SetActive(true);
            }

            if (interactPromptCanvasGroup != null)
                interactPromptCanvasGroup.alpha = 1f;
        }
        else if (interactPromptText != null)
        {
            interactPromptText.text = $"<b>[ E ]</b>  {formatted}";
            interactPromptText.gameObject.SetActive(true);

            if (interactPromptBg != null)
            {
                interactPromptBg.gameObject.SetActive(true);
            }
        }
    }

    public void HideInteractPrompt()
    {
        if (interactPromptRoot != null)
            interactPromptRoot.SetActive(false);
        else if (interactPromptTMP != null)
            interactPromptTMP.gameObject.SetActive(false);

        if (interactPromptText != null)
        {
            interactPromptText.text = "";
            interactPromptText.gameObject.SetActive(false);
        }

        if (interactPromptBg != null)
        {
            interactPromptBg.gameObject.SetActive(false);
        }
    }

    public static string FormatPromptText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        // Strip legacy prefixes such as "E — ", "E - ", or "[ E ]"
        if (raw.StartsWith("E — ") || raw.StartsWith("E - "))
            raw = raw.Substring(4);
        else if (raw.StartsWith("[ E ]") || raw.StartsWith("<b>[ E ]</b>"))
        {
            int closeBracket = raw.IndexOf(']');
            if (closeBracket >= 0 && closeBracket + 1 < raw.Length)
                raw = raw.Substring(closeBracket + 1).Trim();
        }

        int parenOpen = raw.IndexOf('(');
        int parenClose = raw.LastIndexOf(')');

        if (parenOpen > 0 && parenClose > parenOpen)
        {
            string mainAction = raw.Substring(0, parenOpen).Trim();
            string detail = raw.Substring(parenOpen, parenClose - parenOpen + 1);

            // Duration countdown format such as (15s)
            if (detail.EndsWith("s)") && detail.Length > 2 && char.IsDigit(detail[1]))
            {
                return $"<b>{mainAction}</b> <color=#FBBF24><size=90%>{detail}</size></color>";
            }

            // Action hint / secondary instruction
            return $"<b>{mainAction}</b> <color=#94A3B8><size=85%>{detail}</size></color>";
        }

        if (raw.Contains("Harvest") || raw.Contains("Panen"))
        {
            return $"<b><color=#4ADE80>{raw}</color></b>";
        }

        return $"<b>{raw}</b>";
    }

    public void ShowHotbarPopup(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return;

        if (hotbarPopupTMP != null)
        {
            hotbarPopupTMP.text = itemName;

            if (hotbarPopupRoot != null)
                hotbarPopupRoot.SetActive(true);
            else
                hotbarPopupTMP.gameObject.SetActive(true);

            if (hideHotbarCoroutine != null)
                StopCoroutine(hideHotbarCoroutine);

            hideHotbarCoroutine = StartCoroutine(HotbarPopupRoutine(hotbarPopupCanvasGroup, hotbarPopupRoot ?? hotbarPopupTMP.gameObject));
        }
        else if (hotbarPopupText != null)
        {
            hotbarPopupText.text = itemName;
            hotbarPopupText.gameObject.SetActive(true);

            if (hideHotbarCoroutine != null)
                StopCoroutine(hideHotbarCoroutine);

            hideHotbarCoroutine = StartCoroutine(HotbarPopupRoutine(legacyHotbarCanvasGroup, hotbarPopupText.gameObject));
        }
    }

    private IEnumerator HotbarPopupRoutine(CanvasGroup cg, GameObject targetObj)
    {
        // Smooth fade in
        if (cg != null)
        {
            cg.alpha = 0f;
            float elapsed = 0f;
            float fadeInDur = 0.12f;
            while (elapsed < fadeInDur)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDur);
                yield return null;
            }
            cg.alpha = 1f;
        }

        // Stay visible steadily for 1.8 seconds
        yield return new WaitForSecondsRealtime(1.8f);

        // Smooth fade out
        if (cg != null)
        {
            float elapsed = 0f;
            float fadeOutDur = 0.25f;
            while (elapsed < fadeOutDur)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDur);
                yield return null;
            }
            cg.alpha = 0f;
        }

        if (targetObj != null)
            targetObj.SetActive(false);

        hideHotbarCoroutine = null;
    }

    private static void SanitizeText(Text text)
    {
        if (text == null)
            return;

        text.text = "";
        text.gameObject.SetActive(false);
    }

    // Build dark tooltip box as a sibling (earliest index)
    // so it renders behind the tooltip text and tracks it every frame.
    private void BuildTooltipBackground()
    {
        if (tooltipBg != null)
            return;

        RectTransform textRect = mouseTooltipText.rectTransform;
        Canvas canvas = textRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
            return;

        var bgGO = new GameObject("UI_MouseTooltipBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasRect, false);
        // Sibling index 0: drawn first, so tooltip text (higher sibling
        // index) renders on top of the dark box.
        bgGO.transform.SetAsFirstSibling();

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        bgImg.sprite = null;
        bgImg.raycastTarget = false;
        bgImg.type = Image.Type.Simple;

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = textRect.anchorMin;
        bgRT.anchorMax = textRect.anchorMax;
        bgRT.pivot = textRect.pivot;
        bgRT.sizeDelta = textRect.sizeDelta + new Vector2(20f, 12f);

        tooltipBg = bgRT;
        bgGO.SetActive(false);
    }

    private void BuildInteractPromptBackground()
    {
        if (interactPromptBg != null || interactPromptText == null)
            return;

        RectTransform textRect = interactPromptText.rectTransform;
        Canvas canvas = textRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
            return;

        var bgGO = new GameObject("UI_InteractPromptBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasRect, false);
        // Position immediately before interactPromptText in sibling order so it renders behind it
        int textSiblingIndex = textRect.GetSiblingIndex();
        bgGO.transform.SetSiblingIndex(Mathf.Max(0, textSiblingIndex));

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f); // Deep dark pill
        bgImg.raycastTarget = false;

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = textRect.anchorMin;
        bgRT.anchorMax = textRect.anchorMax;
        bgRT.pivot = textRect.pivot;
        bgRT.anchoredPosition = textRect.anchoredPosition;
        bgRT.sizeDelta = new Vector2(480f, 44f);

        interactPromptBg = bgRT;
        bgGO.SetActive(false);
    }
}