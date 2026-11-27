using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FeaturesCommon;

public partial class MainMenuController
{
    private void UpdateFadingIn(float dt)
    {
        stateTimer += dt;

        if (bgOverlayCG != null)
        {
            float t = Mathf.Clamp01((stateTimer - bgFadeDelay) / elementFadeDuration);
            bgOverlayCG.alpha = EaseOutCubic(t);
        }

        if (titleCG != null && titleText != null)
        {
            float t = Mathf.Clamp01((stateTimer - titleFadeDelay) / elementFadeDuration);
            titleCG.alpha = EaseOutCubic(t);
            float scaleT = EaseOutBack(Mathf.Clamp01((stateTimer - titleFadeDelay) / elementFadeDuration));
            titleText.rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, scaleT);
        }

        float[] delays = { startBtnDelay, settingsBtnDelay, quitBtnDelay };
        for (int i = 0; i < mainPanelCG.transform.childCount && i < delays.Length; i++)
        {
            var child = mainPanelCG.transform.GetChild(i);
            var cg = child.GetComponent<CanvasGroup>();
            if (cg == null) continue;

            float t = Mathf.Clamp01((stateTimer - delays[i]) / elementFadeDuration);
            cg.alpha = EaseOutCubic(t);
            Vector3 targetPos = child.localPosition;
            Vector3 startPos = targetPos + Vector3.down * 25f;
            child.localPosition = Vector3.Lerp(startPos, targetPos, EaseOutCubic(t));
        }

        if (stateTimer >= 1.2f)
        {
            currentState = MenuState.Active;
            titleOriginalPos = titleText != null ? titleText.rectTransform.localPosition : Vector3.zero;
            titleOriginalScale = titleText != null ? titleText.rectTransform.localScale : Vector3.one;
        }
    }

    private void UpdateFadingOut(float dt)
    {
        var fantasyAnimator = GetComponent<FantasyMenuAnimator>();
        if (fantasyAnimator != null && fantasyAnimator.IsTransitioningToGame)
            return;

        stateTimer += dt;
        float t = Mathf.Clamp01(stateTimer / stateDuration);
        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 1f - EaseOutCubic(t);

        if (t >= 1f)
        {
            SetMenuVisualsActive(false);
            LockPlayerInput(false);
            FarmBeware.Core.Runtime.UIModalHelper.RemovePauseLock("PauseMenu");
            if (mainCamera != null)
                mainCamera.transform.position = cameraOriginalPos;
            currentState = MenuState.Hidden;

            bool isMainMenu = SceneManager.GetActiveScene().name == "MainMenuScene";
            bool willLoadScene = isMainMenu && loadSceneOnStart && !string.IsNullOrEmpty(targetSceneName);

            if (willLoadScene)
            {
                if (targetSceneName == "StagingScene") targetSceneName = "MainScene";
                Debug.Log($"[MainMenuController] Loading target scene: {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                if (FadeManager.Instance != null && FadeManager.Instance.IsFading)
                    FadeManager.Instance.FadeOut(0.3f);
            }
        }
    }

    private void UpdateTitleIdle(float dt)
    {
        if (titleText == null) return;
        float time = (float)(System.DateTime.UtcNow - System.DateTime.MinValue).TotalSeconds;
        float yOffset = Mathf.Sin(time * titleFloatSpeed * Mathf.PI * 2f) * titleFloatAmplitude;
        float scaleBreath = 1f + Mathf.Sin(time * titleScaleBreathSpeed * Mathf.PI * 2f) * titleScaleBreathAmount;
        titleText.rectTransform.localPosition = titleOriginalPos + Vector3.up * yOffset;
        titleText.rectTransform.localScale = titleOriginalScale * scaleBreath;
    }

    private void UpdateCameraSway(float dt)
    {
        if (mainCamera == null) return;
        float time = (float)(System.DateTime.UtcNow - System.DateTime.MinValue).TotalSeconds;
        float x = Mathf.Sin(time * 0.15f) * 0.3f;
        float y = Mathf.Sin(time * 0.1f + 1.5f) * 0.15f;
        mainCamera.transform.position = cameraOriginalPos + new Vector3(x, y, 0f);
    }

    private float EaseOutCubic(float t) { return 1f - Mathf.Pow(1f - t, 3f); }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void SetupLayout()
    {
        if (titleText != null)
        {
            var rt = titleText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localPosition = new Vector3(0, 180, 0);
            rt.sizeDelta = new Vector2(1000, 160);
        }

        if (mainMenuPanel != null)
        {
            var rt = mainMenuPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localPosition = new Vector3(0, -30, 0);
            rt.sizeDelta = new Vector2(400, 220);

            var vlg = mainMenuPanel.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = mainMenuPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(0, 0, 5, 5);

            foreach (Transform child in mainMenuPanel.transform)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 360;
                le.preferredHeight = 55;
            }
        }

        SetupSettingsPanel();
    }

    private void SetupSettingsPanel()
    {
        if (settingsPanel == null) return;
        var rt = settingsPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(920, 613);
        }

        var vlg = settingsPanel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Destroy(vlg);

        var bg = settingsPanel.transform.Find("SettingsBackground");
        if (bg != null)
        {
            var bgRT = bg.GetComponent<RectTransform>();
            if (bgRT != null)
            {
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = Vector2.zero;
                bgRT.anchoredPosition = Vector2.zero;
            }
            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.color = Color.white;
        }

        var backBtn = settingsPanel.transform.Find("BackButton");
        if (backBtn != null)
        {
            EnsureAnimationHandler(backBtn.gameObject);
        }
    }

    private void ApplyVisualStyling()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.fontSize = 52;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.95f, 0.92f, 0.82f, 1f);
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0f, 0f, 0.05f, 0.55f);
        }

        Color btnBg = new Color(0.12f, 0.11f, 0.16f, 0.85f);
        Color btnText = new Color(0.95f, 0.92f, 0.82f, 1f);
        SetupButton(startButton, "START", btnBg, btnText);
        SetupButton(settingsButton, "SETTINGS", btnBg, btnText);
        SetupButton(quitButton, "QUIT", btnBg, btnText);
    }

    private void SetupButton(Button btn, string label, Color bgColor, Color textColor)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = bgColor;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.gameObject.SetActive(true);
            txt.text = label;
            txt.fontSize = 26;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = textColor;
        }
        EnsureAnimationHandler(btn.gameObject);
    }

    private void EnsureAnimationHandler(GameObject go)
    {
        if (go.GetComponent<UIAnimationHandler>() == null)
            go.AddComponent<UIAnimationHandler>();
    }

    private void SetupAtmosphere()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.70f, 0.74f, 0.80f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f, 1.0f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.32f, 0.28f, 1.0f);
        RenderSettings.ambientIntensity = 1.0f;

        var dl = GameObject.Find("Directional Light");
        if (dl != null)
        {
            var l = dl.GetComponent<Light>();
            if (l != null)
            {
                l.color = new Color(1.0f, 0.95f, 0.88f, 1.0f);
                l.intensity = 0.95f;
                l.shadowStrength = 0.55f;
            }
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0f, 0f, 0.05f, 0.55f);
            backgroundOverlay.type = Image.Type.Simple;
            backgroundOverlay.raycastTarget = false;
        }
    }

    private void UpdateSettingsOpening(float dt)
    {
        stateTimer += dt;
        float fadeDur = 0.15f;
        float showDur = 0.25f;

        if (stateTimer < fadeDur)
        {
            float t = EaseOutCubic(Mathf.Clamp01(stateTimer / fadeDur));
            if (mainPanelCG != null) mainPanelCG.alpha = 1f - t;
            if (titleCG != null) titleCG.alpha = 1f - t;
        }
        else
        {
            // Ensure MainMenuPanel and GameTitle are closed and do not block raycasts
            if (mainMenuPanel != null && mainMenuPanel.activeSelf)
                mainMenuPanel.SetActive(false);
            if (titleRoot != null && titleRoot.activeSelf)
                titleRoot.SetActive(false);
            if (loadGameButton != null && loadGameButton.gameObject.activeSelf)
                loadGameButton.gameObject.SetActive(false);

            if (mainPanelCG != null)
            {
                mainPanelCG.alpha = 0f;
                mainPanelCG.blocksRaycasts = false;
                mainPanelCG.interactable = false;
            }
            if (titleCG != null)
            {
                titleCG.alpha = 0f;
                titleCG.blocksRaycasts = false;
                titleCG.interactable = false;
            }

            Vector3 targetScale = GetSettingsTargetScale();
            Vector3 startScale = targetScale * 0.9f;

            // Enable SettingsPanel
            if (settingsPanel != null && !settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(true);
                if (settingsCG == null)
                    settingsCG = settingsPanel.GetComponent<CanvasGroup>() ?? settingsPanel.AddComponent<CanvasGroup>();
                settingsPanel.transform.localScale = startScale;
                settingsCG.alpha = 0f;
            }

            float elapsed = stateTimer - fadeDur;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / showDur));

            if (settingsCG != null)
            {
                settingsCG.alpha = t;
                settingsCG.blocksRaycasts = true;
                settingsCG.interactable = true;
            }

            if (settingsPanel != null)
                settingsPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (t >= 1f)
            {
                currentState = MenuState.SettingsOpen;
                if (settingsPanel != null)
                    settingsPanel.transform.localScale = targetScale;

                if (settingsCG != null)
                {
                    settingsCG.alpha = 1f;
                    settingsCG.blocksRaycasts = true;
                    settingsCG.interactable = true;
                }
            }
        }
    }

    private void UpdateSettingsClosing(float dt)
    {
        stateTimer += dt;
        float hideDur = 0.15f;
        float showDur = 0.20f;

        Vector3 targetScale = GetSettingsTargetScale();

        if (stateTimer < hideDur)
        {
            float t = EaseOutCubic(Mathf.Clamp01(stateTimer / hideDur));
            if (settingsCG != null)
            {
                settingsCG.alpha = 1f - t;
                settingsCG.interactable = false;
            }
            if (settingsPanel != null)
                settingsPanel.transform.localScale = Vector3.Lerp(targetScale, targetScale * 0.95f, t);
        }
        else
        {
            // Ensure SettingsPanel is completely closed and disabled so it does not block raycasts
            if (settingsCG != null)
            {
                settingsCG.alpha = 0f;
                settingsCG.blocksRaycasts = false;
                settingsCG.interactable = false;
            }

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }

            // Re-enable MainMenuPanel and GameTitle
            if (mainMenuPanel != null && !mainMenuPanel.activeSelf)
            {
                mainMenuPanel.SetActive(true);
            }
            if (titleRoot != null && !titleRoot.activeSelf)
            {
                titleRoot.SetActive(true);
            }
            if (loadGameButton != null && !loadGameButton.gameObject.activeSelf)
            {
                loadGameButton.gameObject.SetActive(true);
            }

            if (mainPanelCG == null && mainMenuPanel != null)
            {
                mainPanelCG = mainMenuPanel.GetComponent<CanvasGroup>() ?? mainMenuPanel.AddComponent<CanvasGroup>();
            }
            if (titleCG == null && titleRoot != null)
            {
                titleCG = titleRoot.GetComponent<CanvasGroup>() ?? titleRoot.AddComponent<CanvasGroup>();
            }

            float elapsed = stateTimer - hideDur;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / showDur));

            if (mainPanelCG != null)
            {
                mainPanelCG.alpha = t;
                mainPanelCG.blocksRaycasts = true;
                mainPanelCG.interactable = true;
            }
            if (titleCG != null)
            {
                titleCG.alpha = t;
            }

            if (t >= 1f)
            {
                currentState = MenuState.Active;
                if (mainPanelCG != null) mainPanelCG.alpha = 1f;
                if (titleCG != null) titleCG.alpha = 1f;

                if (mainPanelCG != null)
                {
                    mainPanelCG.alpha = 1f;
                    mainPanelCG.blocksRaycasts = true;
                    mainPanelCG.interactable = true;
                }

                if (settingsPanel != null)
                    settingsPanel.SetActive(false);

                if (settingsCG != null)
                {
                    settingsCG.alpha = 0f;
                    settingsCG.blocksRaycasts = false;
                    settingsCG.interactable = false;
                }

                if (useLegacyCodeStyling)
                {
                    UpdateTitleIdle(0);
                    UpdateCameraSway(0);
                }
            }
        }
    }

}
