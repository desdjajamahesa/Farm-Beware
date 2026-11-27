using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline.UI
{
    public partial class StorylineUI
    {

        public void EnsureUIHierarchy()
        {
            if (targetCanvas == null)
            {
                var canvasObj = GameObject.Find("UI_Canvas");
                if (canvasObj != null) targetCanvas = canvasObj.GetComponent<Canvas>();
                if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasGO = new GameObject("Storyline_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                targetCanvas = canvasGO.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 80;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // 1. Procedural Banner Panel
            if (bannerPanel == null)
            {
                var existingBanner = targetCanvas.transform.Find("StoryBannerPanel");
                if (existingBanner != null)
                {
                    bannerPanel = existingBanner.gameObject;
                    bannerCanvasGroup = bannerPanel.GetComponent<CanvasGroup>();
                    var tTitle = bannerPanel.transform.Find("BannerTitle");
                    if (tTitle != null) bannerTitleText = tTitle.GetComponent<TextMeshProUGUI>();
                    var tBody = bannerPanel.transform.Find("BannerBody");
                    if (tBody != null) bannerBodyText = tBody.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    bannerPanel = new GameObject("StoryBannerPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    bannerPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = bannerPanel.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -85f);
                    rt.sizeDelta = new Vector2(700f, 100f);

                    var img = bannerPanel.GetComponent<Image>();
                    img.color = new Color(0.08f, 0.10f, 0.12f, 0.92f);

                    bannerCanvasGroup = bannerPanel.GetComponent<CanvasGroup>();
                    bannerCanvasGroup.blocksRaycasts = false;

                    // Title
                    var titleGO = new GameObject("BannerTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(bannerPanel.transform, false);
                    var titleRT = titleGO.GetComponent<RectTransform>();
                    titleRT.anchorMin = new Vector2(0f, 0.6f);
                    titleRT.anchorMax = new Vector2(1f, 1f);
                    titleRT.offsetMin = new Vector2(16f, 0f);
                    titleRT.offsetMax = new Vector2(-16f, -6f);
                    bannerTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    bannerTitleText.fontSize = 22;
                    bannerTitleText.fontStyle = FontStyles.Bold;
                    bannerTitleText.color = new Color(1f, 0.85f, 0.4f);
                    bannerTitleText.alignment = TextAlignmentOptions.Center;

                    // Body
                    var bodyGO = new GameObject("BannerBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(bannerPanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0f, 0f);
                    bodyRT.anchorMax = new Vector2(1f, 0.6f);
                    bodyRT.offsetMin = new Vector2(16f, 6f);
                    bodyRT.offsetMax = new Vector2(-16f, 0f);
                    bannerBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    bannerBodyText.fontSize = 18;
                    bannerBodyText.color = Color.white;
                    bannerBodyText.alignment = TextAlignmentOptions.Center;
                    bannerBodyText.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            if (bannerPanel != null)
            {
                if (bannerTitleText == null) bannerTitleText = bannerPanel.transform.Find("BannerTitle")?.GetComponent<TextMeshProUGUI>();
                if (bannerBodyText == null) bannerBodyText = bannerPanel.transform.Find("BannerBody")?.GetComponent<TextMeshProUGUI>();
                if (bannerTitleText == null || bannerBodyText == null)
                {
                    var tmps = bannerPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                    for (int i = 0; i < tmps.Length; i++)
                    {
                        var t = tmps[i];
                        if (bannerTitleText == null && t.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0) bannerTitleText = t;
                        else if (bannerBodyText == null && t.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0) bannerBodyText = t;
                    }
                    if (bannerTitleText == null && tmps.Length > 0) bannerTitleText = tmps[0];
                    if (bannerBodyText == null && tmps.Length > 1) bannerBodyText = tmps[1];
                }
                if (bannerCanvasGroup == null) bannerCanvasGroup = bannerPanel.GetComponent<CanvasGroup>() ?? bannerPanel.AddComponent<CanvasGroup>();

                bannerPanel.SetActive(false);
                if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;
                if (bannerTitleText != null)
                {
                    bannerTitleText.fontSize = 22;
                    bannerTitleText.text = "";
                }
                if (bannerBodyText != null)
                {
                    bannerBodyText.fontSize = 18;
                    bannerBodyText.text = "";
                }
            }

            // 2. Procedural Dialogue Panel
            if (dialoguePanel == null)
            {
                var existingDlg = targetCanvas.transform.Find("StoryDialoguePanel");
                if (existingDlg != null)
                {
                    dialoguePanel = existingDlg.gameObject;
                }
                else
                {
                    dialoguePanel = new GameObject("StoryDialoguePanel", typeof(RectTransform), typeof(Image));
                    dialoguePanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = dialoguePanel.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 115f);
                    rt.sizeDelta = new Vector2(920f, 200f);

                    var img = dialoguePanel.GetComponent<Image>();
                    img.color = new Color(0.06f, 0.08f, 0.10f, 0.95f);

                    // Speaker
                    var spkGO = new GameObject("DialogueSpeaker", typeof(RectTransform), typeof(TextMeshProUGUI));
                    spkGO.transform.SetParent(dialoguePanel.transform, false);
                    var spkRT = spkGO.GetComponent<RectTransform>();
                    spkRT.anchorMin = new Vector2(0f, 0.7f);
                    spkRT.anchorMax = new Vector2(1f, 1f);
                    spkRT.offsetMin = new Vector2(24f, 0f);
                    spkRT.offsetMax = new Vector2(-24f, -10f);
                    dialogueSpeakerText = spkGO.GetComponent<TextMeshProUGUI>();
                    dialogueSpeakerText.fontSize = 24;
                    dialogueSpeakerText.fontStyle = FontStyles.Bold;
                    dialogueSpeakerText.color = new Color(0.95f, 0.75f, 0.35f);

                    // Body
                    var bodyGO = new GameObject("DialogueBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(dialoguePanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0f, 0f);
                    bodyRT.anchorMax = new Vector2(1f, 0.7f);
                    bodyRT.offsetMin = new Vector2(24f, 16f);
                    bodyRT.offsetMax = new Vector2(-120f, 0f);
                    dialogueBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    dialogueBodyText.fontSize = 21;
                    dialogueBodyText.color = Color.white;
                    dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;

                    // Button
                    var btnGO = new GameObject("DialogueButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGO.transform.SetParent(dialoguePanel.transform, false);
                    var btnRT = btnGO.GetComponent<RectTransform>();
                    btnRT.anchorMin = new Vector2(1f, 0f);
                    btnRT.anchorMax = new Vector2(1f, 0f);
                    btnRT.pivot = new Vector2(1f, 0f);
                    btnRT.anchoredPosition = new Vector2(-20f, 20f);
                    btnRT.sizeDelta = new Vector2(140f, 36f);
                    btnGO.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.3f);
                    dialogueContinueButton = btnGO.GetComponent<Button>();
                    dialogueContinueButton.onClick.AddListener(DismissDialogue);

                    var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    btnTextGO.transform.SetParent(btnGO.transform, false);
                    var btxt = btnTextGO.GetComponent<TextMeshProUGUI>();
                    btxt.text = "Continue [SPACE] >";
                    btxt.fontSize = 12;
                    btxt.alignment = TextAlignmentOptions.Center;
                    btxt.color = Color.white;
                }
            }

            if (dialoguePanel != null)
            {
                var rt = dialoguePanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 115f);
                    rt.sizeDelta = new Vector2(920f, 200f);
                }

                if (dialogueSpeakerText == null)
                    dialogueSpeakerText = dialoguePanel.transform.Find("DialogueSpeaker")?.GetComponent<TextMeshProUGUI>();
                if (dialogueBodyText == null)
                    dialogueBodyText = dialoguePanel.transform.Find("DialogueBody")?.GetComponent<TextMeshProUGUI>();

                if (dialogueSpeakerText != null) dialogueSpeakerText.fontSize = 24;
                if (dialogueBodyText != null) dialogueBodyText.fontSize = 21;

                if (dialogueContinueButton == null)
                {
                    dialogueContinueButton = dialoguePanel.transform.Find("DialogueButton")?.GetComponent<Button>();
                    if (dialogueContinueButton != null)
                    {
                        dialogueContinueButton.onClick.RemoveListener(DismissDialogue);
                        dialogueContinueButton.onClick.AddListener(DismissDialogue);
                    }
                }
                var btnTxt = dialoguePanel.transform.Find("DialogueButton/Text")?.GetComponent<TextMeshProUGUI>()
                          ?? dialoguePanel.transform.Find("DialogueButton")?.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null)
                {
                    btnTxt.text = "Continue [SPACE] >";
                }

                var panelCanvas = dialoguePanel.GetComponent<Canvas>();
                if (panelCanvas == null)
                {
                    panelCanvas = dialoguePanel.AddComponent<Canvas>();
                }
                panelCanvas.overrideSorting = true;
                panelCanvas.sortingOrder = 500;
                if (!dialoguePanel.TryGetComponent<GraphicRaycaster>(out _))
                {
                    dialoguePanel.AddComponent<GraphicRaycaster>();
                }
                dialoguePanel.SetActive(false);
            }

            // 3. Procedural Overlay Panel (Prologue/Epilogue)
            if (overlayPanel == null)
            {
                var existingOverlay = targetCanvas.transform.Find("StoryOverlayPanel");
                if (existingOverlay != null)
                {
                    overlayPanel = existingOverlay.gameObject;
                    overlayCanvasGroup = overlayPanel.GetComponent<CanvasGroup>();
                    var tTitle = overlayPanel.transform.Find("OverlayTitle");
                    if (tTitle != null) overlayTitleText = tTitle.GetComponent<TextMeshProUGUI>();
                    var tBody = overlayPanel.transform.Find("OverlayBody");
                    if (tBody != null) overlayBodyText = tBody.GetComponent<TextMeshProUGUI>();
                    var tPrompt = overlayPanel.transform.Find("OverlayPrompt");
                    if (tPrompt != null) overlayPromptText = tPrompt.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    overlayPanel = new GameObject("StoryOverlayPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    overlayPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = overlayPanel.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = overlayPanel.GetComponent<Image>();
                    img.color = new Color(0.02f, 0.02f, 0.03f, 0.98f);

                    overlayCanvasGroup = overlayPanel.GetComponent<CanvasGroup>();

                    // Title
                    var titleGO = new GameObject("OverlayTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(overlayPanel.transform, false);
                    var titleRT = titleGO.GetComponent<RectTransform>();
                    titleRT.anchorMin = new Vector2(0.1f, 0.65f);
                    titleRT.anchorMax = new Vector2(0.9f, 0.85f);
                    titleRT.offsetMin = Vector2.zero;
                    titleRT.offsetMax = Vector2.zero;
                    overlayTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    overlayTitleText.fontSize = 42;
                    overlayTitleText.fontStyle = FontStyles.Bold;
                    overlayTitleText.color = new Color(1f, 0.88f, 0.5f);
                    overlayTitleText.alignment = TextAlignmentOptions.Center;

                    // Body
                    var bodyGO = new GameObject("OverlayBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(overlayPanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0.15f, 0.25f);
                    bodyRT.anchorMax = new Vector2(0.85f, 0.65f);
                    bodyRT.offsetMin = Vector2.zero;
                    bodyRT.offsetMax = Vector2.zero;
                    overlayBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    overlayBodyText.fontSize = 24;
                    overlayBodyText.color = new Color(0.9f, 0.9f, 0.92f);
                    overlayBodyText.alignment = TextAlignmentOptions.Center;
                    overlayBodyText.textWrappingMode = TextWrappingModes.Normal;

                    // Prompt
                    var promptGO = new GameObject("OverlayPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
                    promptGO.transform.SetParent(overlayPanel.transform, false);
                    var promptRT = promptGO.GetComponent<RectTransform>();
                    promptRT.anchorMin = new Vector2(0.1f, 0.1f);
                    promptRT.anchorMax = new Vector2(0.9f, 0.2f);
                    promptRT.offsetMin = Vector2.zero;
                    promptRT.offsetMax = Vector2.zero;
                    overlayPromptText = promptGO.GetComponent<TextMeshProUGUI>();
                    overlayPromptText.fontSize = 15;
                    overlayPromptText.color = new Color(0.6f, 0.6f, 0.6f);
                    overlayPromptText.alignment = TextAlignmentOptions.Center;
                }
            }
            if (overlayPanel != null)
            {
                if (overlayTitleText == null) overlayTitleText = overlayPanel.transform.Find("OverlayTitle")?.GetComponent<TextMeshProUGUI>();
                if (overlayBodyText == null) overlayBodyText = overlayPanel.transform.Find("OverlayBody")?.GetComponent<TextMeshProUGUI>();
                if (overlayPromptText == null) overlayPromptText = overlayPanel.transform.Find("OverlayPrompt")?.GetComponent<TextMeshProUGUI>();
                if (overlayTitleText == null || overlayBodyText == null || overlayPromptText == null)
                {
                    var tmps = overlayPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                    for (int i = 0; i < tmps.Length; i++)
                    {
                        var t = tmps[i];
                        if (overlayTitleText == null && t.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0) overlayTitleText = t;
                        else if (overlayBodyText == null && t.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0) overlayBodyText = t;
                        else if (overlayPromptText == null && t.name.IndexOf("Prompt", StringComparison.OrdinalIgnoreCase) >= 0) overlayPromptText = t;
                    }
                    if (overlayTitleText == null && tmps.Length > 0) overlayTitleText = tmps[0];
                    if (overlayBodyText == null && tmps.Length > 1) overlayBodyText = tmps[1];
                    if (overlayPromptText == null && tmps.Length > 2) overlayPromptText = tmps[2];
                }
                if (overlayCanvasGroup == null) overlayCanvasGroup = overlayPanel.GetComponent<CanvasGroup>() ?? overlayPanel.AddComponent<CanvasGroup>();

                if (overlayTitleText != null) overlayTitleText.fontSize = 42;
                if (overlayBodyText != null) overlayBodyText.fontSize = 24;

                var panelCanvas = overlayPanel.GetComponent<Canvas>();
                if (panelCanvas == null)
                {
                    panelCanvas = overlayPanel.AddComponent<Canvas>();
                }
                panelCanvas.overrideSorting = true;
                panelCanvas.sortingOrder = 600;
                if (!overlayPanel.TryGetComponent<GraphicRaycaster>(out _))
                {
                    overlayPanel.AddComponent<GraphicRaycaster>();
                }
                overlayPanel.SetActive(false);
            }

            // 4. Procedural Dream Panel
            if (dreamPanel == null)
            {
                var existingDream = targetCanvas.transform.Find("StoryDreamPanel");
                if (existingDream != null)
                {
                    dreamPanel = existingDream.gameObject;
                    dreamCanvasGroup = dreamPanel.GetComponent<CanvasGroup>();
                    var tBody = dreamPanel.transform.Find("DreamBody");
                    if (tBody != null) dreamBodyText = tBody.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    dreamPanel = new GameObject("StoryDreamPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    dreamPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = dreamPanel.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = dreamPanel.GetComponent<Image>();
                    img.color = new Color(0.04f, 0.02f, 0.08f, 0.95f);

                    dreamCanvasGroup = dreamPanel.GetComponent<CanvasGroup>();

                    var bodyGO = new GameObject("DreamBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(dreamPanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0.15f, 0.3f);
                    bodyRT.anchorMax = new Vector2(0.85f, 0.7f);
                    bodyRT.offsetMin = Vector2.zero;
                    bodyRT.offsetMax = Vector2.zero;
                    dreamBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    dreamBodyText.fontSize = 22;
                    dreamBodyText.fontStyle = FontStyles.Italic;
                    dreamBodyText.color = new Color(0.85f, 0.7f, 1f);
                    dreamBodyText.alignment = TextAlignmentOptions.Center;
                    dreamBodyText.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            if (dreamPanel != null)
            {
                if (dreamBodyText == null) dreamBodyText = dreamPanel.transform.Find("DreamBody")?.GetComponent<TextMeshProUGUI>() ?? dreamPanel.GetComponentInChildren<TextMeshProUGUI>(true);
                if (dreamCanvasGroup == null) dreamCanvasGroup = dreamPanel.GetComponent<CanvasGroup>() ?? dreamPanel.AddComponent<CanvasGroup>();
                dreamPanel.SetActive(false);
            }

            EnsureTypewriterAudio();
        }

        private void EnsureTypewriterAudio()
        {
            if (typewriterAudioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out typewriterAudioSource))
                {
                    typewriterAudioSource = gameObject.AddComponent<AudioSource>();
                }
                typewriterAudioSource.playOnAwake = false;
                typewriterAudioSource.spatialBlend = 0f;
            }

            if (typewriterAudioChit == null)
            {
                int sampleRate = 44100;
                int sampleCount = (int)(sampleRate * 0.015f);
                float[] samples = new float[sampleCount];
                float freq = 880f;
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1f - ((float)i / sampleCount);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
                }
                var clip = AudioClip.Create("TypewriterProceduralChit", sampleCount, 1, sampleRate, false);
                clip.SetData(samples, 0);
                typewriterAudioChit = clip;
            }
        }

    }
}
