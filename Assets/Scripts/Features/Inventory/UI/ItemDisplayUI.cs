using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Singleton UI untuk menampilkan informasi item bergaya Minecraft:
// 1) Popup nama item di hotbar saat pindah slot terpilih dengan smooth fade.
// 2) Tooltip mengikuti kursor saat hover item di inventory.
// 3) Banner prompt interaksi dengan badge tombol [E] dan formatting teks modern.
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

    // Offset posisi tooltip dari kursor (dapat diatur dari Inspector).
    // Default (25, 65): tooltip muncul di atas kanan kursor agar tidak menutupi pointer.
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
        // Hanya gerakkan tooltip saat sedang tampil.
        if (mouseTooltipText == null || !mouseTooltipText.gameObject.activeSelf)
            return;

        if (Mouse.current == null)
            return;

        // Hardening: jangan pernah menggerakkan teks popup hotbar,
        // baik karena referensi Text yang sama maupun rect transform identik.
        RectTransform tooltipRect = mouseTooltipText.rectTransform;
        if (hotbarPopupText != null &&
            (mouseTooltipText == hotbarPopupText || tooltipRect == hotbarPopupText.rectTransform))
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
            return;

        // Konversi posisi kursor (screen space) ke koordinat lokal canvas,
        // lalu terapkan ke RectTransform tooltip (anak langsung kanvas,
        // anchor 0.5/0.5) sehingga mengikuti kursor dengan aman.
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Mouse.current.position.ReadValue(),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint))
            return;

        Vector2 target = localPoint + tooltipOffset;
        tooltipRect.anchoredPosition = target;

        // Kotak background ikut bergerak di belakang tooltip (kanvas yang sama).
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

    // Label nama objek dunia: dinonaktifkan untuk mencegah teks dobel di atas hotbar.
    public void ShowWorldHover(string name)
    {
        // Safe no-op. Jika masih ada worldHoverText legacy di scene, pastikan tetap nonaktif.
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

    // Prompt aksi "E — Nama" saat objek interaktif sedang di-hover.
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
                interactPromptRoot.SetActive(true);
            else
                interactPromptTMP.gameObject.SetActive(true);

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

        // Bersihkan prefix jika ada string lama "E — " atau "E - " atau "[ E ]"
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

            // Jika countdown durasi seperti (15s)
            if (detail.EndsWith("s)") && detail.Length > 2 && char.IsDigit(detail[1]))
            {
                return $"<b>{mainAction}</b> <color=#FBBF24><size=90%>{detail}</size></color>";
            }

            // Hint aksi / instruksi sekunder
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

        // Tampil stabil selama 1.8 detik
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

    // Bangun kotak gelap untuk tooltip sebagai sibling (indeks paling awal)
    // sehingga dirender di belakang teks tooltip dan ikut bergerak setiap frame.
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
        // Indeks sibling 0: digambar paling awal, sehingga teks tooltip (sibling
        // dengan indeks lebih besar) tampil di atas kotak gelap.
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
        // Posisikan tepat sebelum interactPromptText dalam urutan sibling agar berada di belakangnya
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