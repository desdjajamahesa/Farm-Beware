using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Singleton UI untuk menampilkan informasi item bergaya Minecraft:
// 1) Popup nama item di hotbar saat pindah slot terpilih.
// 2) Tooltip mengikuti kursor saat hover item di inventory.
//
// Catatan visual: background gelap tooltip dibuat di runtime pada GameObject
// TERPISAH (tanpa komponen Text). Menambahkan Image pada GameObject yang sudah
// memiliki Text melempar NullReferenceException di editor, sehingga kotak
// background dibuat sebagai sibling yang disinkronkan dengan tooltip.
public class ItemDisplayUI : MonoBehaviour
{
    public static ItemDisplayUI Instance { get; private set; }

    public Text hotbarPopupText;
    public Text mouseTooltipText;

    // Label nama objek dunia (hover) yang ditampilkan TETAP di atas hotbar
    // (bukan mengikuti kursor), dipakai oleh HoverLabelController.
    public Text worldHoverText;

    // Prompt aksi "E — Nama" untuk objek interaktif yang sedang di-hover.
    public Text interactPromptText;

    // Offset posisi tooltip dari kursor (dapat diatur dari Inspector).
    // Default (25, 65): tooltip muncul di atas kanan kursor agar tidak menutupi pointer.
    [SerializeField] private Vector2 tooltipOffset = new Vector2(25f, 65f);

    private Coroutine hideHotbarCoroutine;
    private RectTransform tooltipBg;

    void Awake()
    {
        Instance = this;

        SanitizeText(hotbarPopupText);
        SanitizeText(mouseTooltipText);
        SanitizeText(worldHoverText);
        SanitizeText(interactPromptText);

        if (mouseTooltipText != null)
            BuildTooltipBackground();
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

    // Label nama objek dunia: tampil PERSISTEN di atas hotbar (bukan ikut kursor).
    public void ShowWorldHover(string name)
    {
        if (worldHoverText == null)
            return;

        worldHoverText.text = name;
        worldHoverText.gameObject.SetActive(true);
    }

    public void HideWorldHover()
    {
        if (worldHoverText == null)
            return;

        worldHoverText.text = "";
        worldHoverText.gameObject.SetActive(false);
    }

    // Prompt aksi "E — Nama" saat objek interaktif sedang di-hover.
    public void ShowInteractPrompt(string displayName)
    {
        if (interactPromptText == null)
            return;

        interactPromptText.text = "E — " + displayName;
        interactPromptText.gameObject.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        if (interactPromptText == null)
            return;

        interactPromptText.text = "";
        interactPromptText.gameObject.SetActive(false);
    }

    public void ShowHotbarPopup(string itemName)
    {
        if (hotbarPopupText == null)
            return;

        hotbarPopupText.text = itemName;
        hotbarPopupText.gameObject.SetActive(true);

        if (hideHotbarCoroutine != null)
            StopCoroutine(hideHotbarCoroutine);

        hideHotbarCoroutine = StartCoroutine(HideHotbarRoutine());
    }

    private IEnumerator HideHotbarRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (hotbarPopupText != null)
        {
            hotbarPopupText.text = "";
            hotbarPopupText.gameObject.SetActive(false);
        }
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
}