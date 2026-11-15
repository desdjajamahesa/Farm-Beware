using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FeaturesTime;
using FeaturesRendering.Lighting;
using FarmBeware.Core.Runtime;

public partial class DayTransitionUI
{
    #region Atmospheric Particle Motes

    private void ConfigureMotes(bool isNight)
    {
        if (_moteImages == null || _moteImages.Length == 0) return;

        Color moteColor = isNight
            ? new Color(1.00f, 0.32f, 0.34f, 0.78f)
            : new Color(1.00f, 0.88f, 0.42f, 0.74f);

        for (int i = 0; i < _moteImages.Length; i++)
        {
            if (_moteImages[i] != null)
            {
                _moteImages[i].color = moteColor;
                _moteImages[i].gameObject.SetActive(true);
            }
        }
    }

    private void AnimateMotes(float elapsed, bool isNight)
    {
        if (_moteImages == null || _moteImages.Length == 0) return;

        for (int i = 0; i < _moteImages.Length; i++)
        {
            if (_moteImages[i] == null) continue;

            var rt = _moteImages[i].rectTransform;
            Vector2 basePos = _moteBasePositions[i];
            float speed = _moteSpeeds[i];
            float phase = _motePhaseOffsets[i];

            float yOffset = isNight
                ? -((elapsed * speed * 24f + phase * 40f) % 105f)
                : ((elapsed * speed * 24f + phase * 40f) % 105f);

            float xSway = Mathf.Sin((elapsed + phase) * 2.4f) * 14f;

            rt.anchoredPosition = basePos + new Vector2(xSway, yOffset);

            Color c = _moteImages[i].color;
            c.a = 0.25f + 0.50f * Mathf.Sin((elapsed + phase) * 3.8f);
            _moteImages[i].color = c;
        }
    }

    private void HideMotes()
    {
        if (_moteImages == null) return;
        for (int i = 0; i < _moteImages.Length; i++)
        {
            if (_moteImages[i] != null)
                _moteImages[i].gameObject.SetActive(false);
        }
    }

    #endregion

    #region Procedural Audio Synthesis (AAA Orchestral & Doom Bell)

    private void PlaySound(bool isNight)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isNight ? nightTransitionSfx : dayTransitionSfx;
        if (clipToPlay == null)
        {
            clipToPlay = isNight ? GetOrCreateNightDoomToll() : GetOrCreateDayDawnAwakening();
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, sfxVolume);
        }
    }

    private static AudioClip _cachedDayChime;
    private static AudioClip _cachedNightChime;

    private static AudioClip GetOrCreateDayDawnAwakening()
    {
        if (_cachedDayChime != null) return _cachedDayChime;

        int sampleRate = 44100;
        float duration = 2.0f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] freqs = { 261.63f, 329.63f, 392.00f, 493.88f, 587.33f, 783.99f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sub = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.32f * Mathf.Exp(-t * 1.5f);

            float harp = 0f;
            for (int n = 0; n < freqs.Length; n++)
            {
                float noteStart = n * 0.075f;
                if (t >= noteStart)
                {
                    float noteT = t - noteStart;
                    float noteEnv = Mathf.Exp(-noteT * (3.2f + n * 0.3f));
                    float fundamental = Mathf.Sin(2f * Mathf.PI * freqs[n] * noteT);
                    float overtone = Mathf.Sin(4f * Mathf.PI * freqs[n] * noteT) * 0.28f;
                    harp += (fundamental + overtone) * noteEnv * 0.20f;
                }
            }

            samples[i] = Mathf.Clamp(sub + harp, -1f, 1f);
        }

        _cachedDayChime = AudioClip.Create("Chime_DayDawnAwakening", sampleCount, 1, sampleRate, false);
        _cachedDayChime.SetData(samples, 0);
        return _cachedDayChime;
    }

    private static AudioClip GetOrCreateNightDoomToll()
    {
        if (_cachedNightChime != null) return _cachedNightChime;

        int sampleRate = 44100;
        float duration = 2.4f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            float subImpact = Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 3.6f) * 0.45f;

            float bellEnv = Mathf.Exp(-t * 1.5f);
            float bellFundamental = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.38f;
            float bellMinor3rd = Mathf.Sin(2f * Mathf.PI * 87.31f * t) * 0.22f;
            float bellTritone = Mathf.Sin(2f * Mathf.PI * 103.83f * t) * 0.18f;
            float bellOvertone = Mathf.Sin(2f * Mathf.PI * 220.0f * t) * 0.14f * Mathf.Exp(-t * 2.2f);
            float bellMetallic = Mathf.Sin(2f * Mathf.PI * 587.3f * t) * 0.08f * Mathf.Exp(-t * 3.8f);

            float gong = (bellFundamental + bellMinor3rd + bellTritone + bellOvertone + bellMetallic) * bellEnv;
            float rumble = (Mathf.PerlinNoise(t * 14f, 0.5f) - 0.5f) * 0.10f * Mathf.Exp(-t * 0.9f);

            samples[i] = Mathf.Clamp(subImpact + gong + rumble, -1f, 1f);
        }

        _cachedNightChime = AudioClip.Create("Chime_NightDoomToll", sampleCount, 1, sampleRate, false);
        _cachedNightChime.SetData(samples, 0);
        return _cachedNightChime;
    }

    #endregion

    #region Procedural Sprite Generators

    private static Sprite _cachedVignetteSprite;
    private static Sprite GetOrCreateVignetteSprite()
    {
        if (_cachedVignetteSprite != null) return _cachedVignetteSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxRadius = size * 0.5f;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normDist = Mathf.Clamp01(dist / maxRadius);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normDist - 0.32f) / 0.68f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedVignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedVignetteSprite;
    }

    private static Sprite _cachedFeatheredRibbonSprite;
    /// <summary>
    /// Generates an anamorphic widescreen ribbon sprite that feathers smoothly to 0% alpha
    /// on both horizontal edges and subtle vertical edges (eliminating ugly boxy rectangular cards).
    /// </summary>
    private static Sprite GetOrCreateFeatheredRibbonSprite()
    {
        if (_cachedFeatheredRibbonSprite != null) return _cachedFeatheredRibbonSprite;

        const int w = 256;
        const int h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float ny = (y + 0.5f) / h;
            float distY = Mathf.Abs(ny - 0.5f) * 2f;
            float verticalMask = 1f - Mathf.SmoothStep(0.76f, 1.0f, distY);

            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f) / w;
                float distX = Mathf.Abs(nx - 0.5f) * 2f;
                float horizontalMask = 1f - Mathf.SmoothStep(0.28f, 0.98f, distX);
                float alpha = horizontalMask * verticalMask;
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedFeatheredRibbonSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _cachedFeatheredRibbonSprite;
    }

    private static Sprite _cachedFlareSprite;
    private static Sprite GetOrCreateFlareSprite()
    {
        if (_cachedFlareSprite != null) return _cachedFlareSprite;

        int w = 256;
        int h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float ny = 1f - Mathf.Abs((y - (h * 0.5f)) / (h * 0.5f));
            for (int x = 0; x < w; x++)
            {
                float nx = 1f - Mathf.Abs((x - (w * 0.5f)) / (w * 0.5f));
                float alpha = Mathf.Pow(nx, 2.2f) * Mathf.Pow(ny, 1.5f);
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedFlareSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _cachedFlareSprite;
    }

    private static Sprite _cachedSunCrestSprite;
    /// <summary>
    /// Generates a crisp 96x96 Radiant Sunburst Crest medallion for the Daybreak transition.
    /// </summary>
    private static Sprite GetOrCreateSunCrestSprite()
    {
        if (_cachedSunCrestSprite != null) return _cachedSunCrestSprite;

        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - center;
                float r = d.magnitude / maxR;
                float angle = Mathf.Atan2(d.y, d.x);

                // Inner solar disc
                float core = 1f - Mathf.SmoothStep(0.24f, 0.30f, r);

                // Concentric inner ring
                float ring = (1f - Mathf.SmoothStep(0.0f, 0.035f, Mathf.Abs(r - 0.42f))) * 0.85f;

                // 8-point geometric sunburst rays
                float rayWave = Mathf.Max(0f, Mathf.Cos(angle * 8f));
                float rayReach = 0.32f + Mathf.Pow(rayWave, 3f) * 0.56f;
                float rays = (r > 0.28f && r < rayReach)
                    ? (1f - Mathf.SmoothStep(0.28f, rayReach, r)) * Mathf.Pow(rayWave, 2f) * 0.90f
                    : 0f;

                // Soft ambient corona
                float corona = Mathf.Pow(Mathf.Clamp01(1f - r), 2.6f) * 0.45f;

                float a = Mathf.Clamp01(core + ring + rays + corona);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedSunCrestSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedSunCrestSprite;
    }

    private static Sprite _cachedBloodMoonCrestSprite;
    /// <summary>
    /// Generates a crisp 96x96 Blood-Moon Eclipse Crescent medallion for the Nightfall transition.
    /// </summary>
    private static Sprite GetOrCreateBloodMoonCrestSprite()
    {
        if (_cachedBloodMoonCrestSprite != null) return _cachedBloodMoonCrestSprite;

        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 shadowCenter = center + new Vector2(size * 0.13f, size * 0.06f);
        float maxR = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float rMain = Vector2.Distance(p, center) / maxR;
                float rShadow = Vector2.Distance(p, shadowCenter) / maxR;

                float outerDisc = 1f - Mathf.SmoothStep(0.54f, 0.60f, rMain);
                float shadowCutout = 1f - Mathf.SmoothStep(0.44f, 0.50f, rShadow);
                float crescent = Mathf.Clamp01(outerDisc - shadowCutout * 0.96f);

                // Thin eclipse outer ring
                float eclipseRing = (1f - Mathf.SmoothStep(0f, 0.03f, Mathf.Abs(rMain - 0.68f))) * 0.65f;

                // Soft blood corona glow
                float corona = Mathf.Pow(Mathf.Clamp01(1f - rMain), 2.4f) * 0.40f;

                float a = Mathf.Clamp01(crescent + eclipseRing + corona);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedBloodMoonCrestSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedBloodMoonCrestSprite;
    }

    private static Sprite _cachedHaloRingSprite;
    /// <summary>
    /// Generates a dashed geometric halo ring that rotates slowly behind the celestial medallion.
    /// </summary>
    private static Sprite GetOrCreateHaloRingSprite()
    {
        if (_cachedHaloRingSprite != null) return _cachedHaloRingSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - center;
                float r = d.magnitude / maxR;
                float angle = Mathf.Atan2(d.y, d.x);

                float ringBand = 1f - Mathf.SmoothStep(0f, 0.025f, Mathf.Abs(r - 0.82f));
                float dashMask = Mathf.SmoothStep(-0.2f, 0.3f, Mathf.Cos(angle * 12f));
                float cardinalDiamond = (1f - Mathf.SmoothStep(0f, 0.045f, Mathf.Abs(r - 0.82f)))
                    * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 4f)), 16f);

                float a = Mathf.Clamp01(ringBand * dashMask * 0.75f + cardinalDiamond);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedHaloRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedHaloRingSprite;
    }

    private static Sprite _cachedSparkleMoteSprite;
    private static Sprite GetOrCreateSparkleMoteSprite()
    {
        if (_cachedSparkleMoteSprite != null) return _cachedSparkleMoteSprite;

        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1f - norm, 2.2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedSparkleMoteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedSparkleMoteSprite;
    }

    private static TMP_FontAsset GetFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        var existing = FindFirstObjectByType<TextMeshProUGUI>();
        return existing != null ? existing.font : null;
    }

    #endregion

    #region Procedural UI Hierarchy Generation

    /// <summary>
    /// Builds the redesigned widescreen anamorphic transition hierarchy.
    /// Automatically upgrades any existing scene hierarchy to the new layout.
    /// </summary>
    public void EnsureUIHierarchy()
    {
        // 1. Parent Canvas Resolution
        var parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
        if (parentCanvas == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 750;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = gameObject.AddComponent<GraphicRaycaster>();

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                if (Application.isPlaying) Destroy(scaler);
                else DestroyImmediate(scaler);
            }

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = 750;

            transform.SetAsLastSibling();
        }

        var legacyDayText = transform.Find("DayText");
        if (legacyDayText != null)
        {
            if (Application.isPlaying) Destroy(legacyDayText.gameObject);
            else DestroyImmediate(legacyDayText.gameObject);
        }

        transitionCanvasGroup = GetComponent<CanvasGroup>();
        if (transitionCanvasGroup == null)
            transitionCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.blocksRaycasts = false;
        transitionCanvasGroup.interactable = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // 2. Fullscreen Atmospheric Vignette Overlay
        var vignetteGO = GetOrCreateChild(transform, "VignetteOverlay", typeof(Image));
        var vignetteRect = vignetteGO.GetComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;
        vignetteOverlay = vignetteGO.GetComponent<Image>();
        vignetteOverlay.sprite = GetOrCreateVignetteSprite();
        vignetteOverlay.color = new Color(0f, 0f, 0f, 0f);
        vignetteOverlay.raycastTarget = false;

        // 3. CinemaScope Letterbox Bars (64px with Feathered Metallic Edge Lines)
        var barTopGO = GetOrCreateChild(transform, "CinematicBar_Top", typeof(Image));
        cinematicBarTop = barTopGO.GetComponent<RectTransform>();
        cinematicBarTop.anchorMin = new Vector2(0f, 1f);
        cinematicBarTop.anchorMax = new Vector2(1f, 1f);
        cinematicBarTop.pivot = new Vector2(0.5f, 1f);
        cinematicBarTop.sizeDelta = new Vector2(0f, 64f);
        cinematicBarTop.anchoredPosition = new Vector2(0f, 64f);
        var barTopImg = barTopGO.GetComponent<Image>();
        barTopImg.color = new Color(0.015f, 0.02f, 0.035f, 0.98f);
        barTopImg.raycastTarget = false;

        var topEdgeGO = GetOrCreateChild(cinematicBarTop, "TopBarEdgeLine", typeof(Image));
        var topEdgeRt = topEdgeGO.GetComponent<RectTransform>();
        topEdgeRt.anchorMin = new Vector2(0f, 0f);
        topEdgeRt.anchorMax = new Vector2(1f, 0f);
        topEdgeRt.pivot = new Vector2(0.5f, 0f);
        topEdgeRt.sizeDelta = new Vector2(0f, 2f);
        topEdgeRt.anchoredPosition = Vector2.zero;
        topBarEdgeLine = topEdgeGO.GetComponent<Image>();
        topBarEdgeLine.sprite = GetOrCreateFlareSprite();
        topBarEdgeLine.raycastTarget = false;

        var barBottomGO = GetOrCreateChild(transform, "CinematicBar_Bottom", typeof(Image));
        cinematicBarBottom = barBottomGO.GetComponent<RectTransform>();
        cinematicBarBottom.anchorMin = new Vector2(0f, 0f);
        cinematicBarBottom.anchorMax = new Vector2(1f, 0f);
        cinematicBarBottom.pivot = new Vector2(0.5f, 0f);
        cinematicBarBottom.sizeDelta = new Vector2(0f, 64f);
        cinematicBarBottom.anchoredPosition = new Vector2(0f, -64f);
        var barBottomImg = barBottomGO.GetComponent<Image>();
        barBottomImg.color = new Color(0.015f, 0.02f, 0.035f, 0.98f);
        barBottomImg.raycastTarget = false;

        var botEdgeGO = GetOrCreateChild(cinematicBarBottom, "BottomBarEdgeLine", typeof(Image));
        var botEdgeRt = botEdgeGO.GetComponent<RectTransform>();
        botEdgeRt.anchorMin = new Vector2(0f, 1f);
        botEdgeRt.anchorMax = new Vector2(1f, 1f);
        botEdgeRt.pivot = new Vector2(0.5f, 1f);
        botEdgeRt.sizeDelta = new Vector2(0f, 2f);
        botEdgeRt.anchoredPosition = Vector2.zero;
        bottomBarEdgeLine = botEdgeGO.GetComponent<Image>();
        bottomBarEdgeLine.sprite = GetOrCreateFlareSprite();
        bottomBarEdgeLine.raycastTarget = false;

        // 4. Anamorphic Horizon Flare Beam (sweeps behind the title veil)
        var flareGO = GetOrCreateChild(transform, "HorizonFlareBeam", typeof(Image));
        var flareRt = flareGO.GetComponent<RectTransform>();
        flareRt.anchorMin = new Vector2(0.5f, 1f);
        flareRt.anchorMax = new Vector2(0.5f, 1f);
        flareRt.pivot = new Vector2(0.5f, 0.5f);
        flareRt.anchoredPosition = new Vector2(0f, -215f);
        flareRt.sizeDelta = new Vector2(0f, 14f);
        horizonFlareBeam = flareGO.GetComponent<Image>();
        horizonFlareBeam.sprite = GetOrCreateFlareSprite();
        horizonFlareBeam.color = new Color(1f, 1f, 1f, 0f);
        horizonFlareBeam.raycastTarget = false;

        // 5. Feathered Widescreen Banner Veil (1360x220, horizontally feathered to 0% alpha at ends)
        var bannerGO = GetOrCreateChild(transform, "BannerRoot", typeof(Image));
        bannerRoot = bannerGO.GetComponent<RectTransform>();
        bannerRoot.anchorMin = new Vector2(0.5f, 1f);
        bannerRoot.anchorMax = new Vector2(0.5f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 1f);
        bannerRoot.anchoredPosition = baseBannerPosition;
        bannerRoot.sizeDelta = new Vector2(1360f, 224f);

        bannerBackgroundImage = bannerGO.GetComponent<Image>();
        bannerBackgroundImage.sprite = GetOrCreateFeatheredRibbonSprite();
        bannerBackgroundImage.type = Image.Type.Simple;
        bannerBackgroundImage.color = new Color(0.035f, 0.05f, 0.085f, 0.90f);
        bannerBackgroundImage.raycastTarget = false;

        // Soft inner core glow behind medallion & title
        var innerGlowGO = GetOrCreateChild(bannerRoot, "InnerCoreGlow", typeof(Image));
        var innerGlowRt = innerGlowGO.GetComponent<RectTransform>();
        innerGlowRt.anchorMin = new Vector2(0.22f, 0.15f);
        innerGlowRt.anchorMax = new Vector2(0.78f, 0.88f);
        innerGlowRt.offsetMin = Vector2.zero;
        innerGlowRt.offsetMax = Vector2.zero;
        bannerInnerGlowImage = innerGlowGO.GetComponent<Image>();
        bannerInnerGlowImage.sprite = GetOrCreateFeatheredRibbonSprite();
        bannerInnerGlowImage.color = new Color(0.55f, 0.38f, 0.08f, 0.28f);
        bannerInnerGlowImage.raycastTarget = false;

        // 6. Tapered Filigree Rules (Top & Bottom of Veil)
        var topAccentGO = GetOrCreateChild(bannerRoot, "TopAccentLine", typeof(Image));
        var topAccentRect = topAccentGO.GetComponent<RectTransform>();
        topAccentRect.anchorMin = new Vector2(0.10f, 0.985f);
        topAccentRect.anchorMax = new Vector2(0.90f, 0.985f);
        topAccentRect.pivot = new Vector2(0.5f, 0.5f);
        topAccentRect.anchoredPosition = Vector2.zero;
        topAccentRect.sizeDelta = new Vector2(0f, 2.5f);
        bannerTopAccentLine = topAccentGO.GetComponent<Image>();
        bannerTopAccentLine.sprite = GetOrCreateFlareSprite();
        bannerTopAccentLine.raycastTarget = false;

        var accentGO = GetOrCreateChild(bannerRoot, "AccentLine", typeof(Image));
        var accentRect = accentGO.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.10f, 0.015f);
        accentRect.anchorMax = new Vector2(0.90f, 0.015f);
        accentRect.pivot = new Vector2(0.5f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 2.5f);
        bannerAccentLine = accentGO.GetComponent<Image>();
        bannerAccentLine.sprite = GetOrCreateFlareSprite();
        bannerAccentLine.raycastTarget = false;

        // 7. Celestial Crest Medallion & Rotating Runic Halo (Top Center of Veil)
        var haloGO = GetOrCreateChild(bannerRoot, "CelestialHaloRing", typeof(Image));
        var haloRt = haloGO.GetComponent<RectTransform>();
        haloRt.anchorMin = new Vector2(0.5f, 1f);
        haloRt.anchorMax = new Vector2(0.5f, 1f);
        haloRt.pivot = new Vector2(0.5f, 0.5f);
        haloRt.anchoredPosition = new Vector2(0f, -34f);
        haloRt.sizeDelta = new Vector2(78f, 78f);
        celestialHaloImage = haloGO.GetComponent<Image>();
        celestialHaloImage.sprite = GetOrCreateHaloRingSprite();
        celestialHaloImage.raycastTarget = false;

        var medallionGO = GetOrCreateChild(bannerRoot, "CelestialMedallion", typeof(Image));
        var medallionRt = medallionGO.GetComponent<RectTransform>();
        medallionRt.anchorMin = new Vector2(0.5f, 1f);
        medallionRt.anchorMax = new Vector2(0.5f, 1f);
        medallionRt.pivot = new Vector2(0.5f, 0.5f);
        medallionRt.anchoredPosition = new Vector2(0f, -34f);
        medallionRt.sizeDelta = new Vector2(52f, 52f);
        celestialMedallionImage = medallionGO.GetComponent<Image>();
        celestialMedallionImage.sprite = GetOrCreateSunCrestSprite();
        celestialMedallionImage.raycastTarget = false;

        var defaultFont = GetFontAsset();

        // 8. Category Tag Text (Chapter / Eclipse Kicker Overline)
        var catGO = GetOrCreateChild(bannerRoot, "CategoryTagText", typeof(TextMeshProUGUI));
        var catRect = catGO.GetComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0.10f, 0.63f);
        catRect.anchorMax = new Vector2(0.90f, 0.74f);
        catRect.offsetMin = Vector2.zero;
        catRect.offsetMax = Vector2.zero;
        categoryTagText = catGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) categoryTagText.font = defaultFont;
        categoryTagText.fontSize = 11.5f;
        categoryTagText.fontStyle = FontStyles.Bold;
        categoryTagText.characterSpacing = 8.5f;
        categoryTagText.alignment = TextAlignmentOptions.Center;
        categoryTagText.raycastTarget = false;

        // 9. Main Hero Display Title ("DAY X" / "NIGHTFALL") + Left/Right Filigree Wings
        var titleGO = GetOrCreateChild(bannerRoot, "TitleText", typeof(TextMeshProUGUI));
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.15f, 0.36f);
        titleRect.anchorMax = new Vector2(0.85f, 0.64f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleText.font = defaultFont;
        titleText.fontSize = 44f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 8f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        var leftWingGO = GetOrCreateChild(bannerRoot, "LeftTitleWing", typeof(Image));
        var leftWingRt = leftWingGO.GetComponent<RectTransform>();
        leftWingRt.anchorMin = new Vector2(0.16f, 0.50f);
        leftWingRt.anchorMax = new Vector2(0.36f, 0.50f);
        leftWingRt.pivot = new Vector2(1f, 0.5f);
        leftWingRt.anchoredPosition = Vector2.zero;
        leftWingRt.sizeDelta = new Vector2(0f, 2f);
        leftTitleWingImage = leftWingGO.GetComponent<Image>();
        leftTitleWingImage.sprite = GetOrCreateFlareSprite();
        leftTitleWingImage.raycastTarget = false;

        var rightWingGO = GetOrCreateChild(bannerRoot, "RightTitleWing", typeof(Image));
        var rightWingRt = rightWingGO.GetComponent<RectTransform>();
        rightWingRt.anchorMin = new Vector2(0.64f, 0.50f);
        rightWingRt.anchorMax = new Vector2(0.84f, 0.50f);
        rightWingRt.pivot = new Vector2(0f, 0.5f);
        rightWingRt.anchoredPosition = Vector2.zero;
        rightWingRt.sizeDelta = new Vector2(0f, 2f);
        rightTitleWingImage = rightWingGO.GetComponent<Image>();
        rightTitleWingImage.sprite = GetOrCreateFlareSprite();
        rightTitleWingImage.raycastTarget = false;

        // 10. Editorial Narrative Subtitle
        var subGO = GetOrCreateChild(bannerRoot, "SubtitleText", typeof(TextMeshProUGUI));
        var subRect = subGO.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.12f, 0.21f);
        subRect.anchorMax = new Vector2(0.88f, 0.36f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) subtitleText.font = defaultFont;
        subtitleText.fontSize = 14f;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.characterSpacing = 1.2f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.raycastTarget = false;

        // 11. Sleek Tactical Phase Telemetry Ribbon (StatusPill)
        var pillGO = GetOrCreateChild(bannerRoot, "StatusPill", typeof(Image));
        var pillRt = pillGO.GetComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(0.5f, 0.045f);
        pillRt.anchorMax = new Vector2(0.5f, 0.045f);
        pillRt.pivot = new Vector2(0.5f, 0f);
        pillRt.anchoredPosition = Vector2.zero;
        pillRt.sizeDelta = new Vector2(760f, 26f);
        statusPillBg = pillGO.GetComponent<Image>();
        statusPillBg.sprite = GetOrCreateFeatheredRibbonSprite();
        statusPillBg.type = Image.Type.Simple;
        statusPillBg.color = new Color(0.05f, 0.07f, 0.12f, 0.82f);
        statusPillBg.raycastTarget = false;

        var pillRuleGO = GetOrCreateChild(pillRt, "TopRule", typeof(Image));
        var pillRuleRt = pillRuleGO.GetComponent<RectTransform>();
        pillRuleRt.anchorMin = new Vector2(0.1f, 1f);
        pillRuleRt.anchorMax = new Vector2(0.9f, 1f);
        pillRuleRt.pivot = new Vector2(0.5f, 1f);
        pillRuleRt.anchoredPosition = Vector2.zero;
        pillRuleRt.sizeDelta = new Vector2(0f, 1.5f);
        statusPillTopRule = pillRuleGO.GetComponent<Image>();
        statusPillTopRule.sprite = GetOrCreateFlareSprite();
        statusPillTopRule.raycastTarget = false;

        var pillTextGO = GetOrCreateChild(pillRt, "Text", typeof(TextMeshProUGUI));
        var pillTextRt = pillTextGO.GetComponent<RectTransform>();
        pillTextRt.anchorMin = Vector2.zero;
        pillTextRt.anchorMax = Vector2.one;
        pillTextRt.offsetMin = Vector2.zero;
        pillTextRt.offsetMax = Vector2.zero;
        statusPillText = pillTextGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statusPillText.font = defaultFont;
        statusPillText.fontSize = 11f;
        statusPillText.richText = true;
        statusPillText.characterSpacing = 2.0f;
        statusPillText.alignment = TextAlignmentOptions.Center;
        statusPillText.raycastTarget = false;
        if (!string.IsNullOrEmpty(statusPillText.text) && statusPillText.text.IndexOf('\u25C6') >= 0)
            statusPillText.text = statusPillText.text.Replace('\u25C6', '\u2022');

        // 12. Legacy IconText compatibility (hidden/clear)
        var iconGO = GetOrCreateChild(bannerRoot, "IconText", typeof(TextMeshProUGUI));
        iconText = iconGO.GetComponent<TextMeshProUGUI>();
        iconText.raycastTarget = false;
        iconText.text = "";

        // 13. Atmospheric Motes Container
        var motesGO = GetOrCreateChild(transform, "AtmosphereMotes");
        motesContainer = motesGO.GetComponent<RectTransform>();
        motesContainer.anchorMin = new Vector2(0.5f, 1f);
        motesContainer.anchorMax = new Vector2(0.5f, 1f);
        motesContainer.pivot = new Vector2(0.5f, 1f);
        motesContainer.anchoredPosition = baseBannerPosition;
        motesContainer.sizeDelta = new Vector2(1100f, 220f);

        BuildParticleMotes(motesContainer);
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName, params Type[] components)
    {
        var existing = parent.Find(childName);
        if (existing != null)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (existing.GetComponent(components[i]) == null)
                {
                    existing.gameObject.AddComponent(components[i]);
                }
            }
            return existing.gameObject;
        }

        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++)
        {
            if (go.GetComponent(components[i]) == null)
            {
                go.AddComponent(components[i]);
            }
        }
        return go;
    }

    private void BuildParticleMotes(RectTransform container)
    {
        const int count = 14;
        _moteImages = new Image[count];
        _moteBasePositions = new Vector2[count];
        _moteSpeeds = new float[count];
        _motePhaseOffsets = new float[count];

        Sprite moteSprite = GetOrCreateSparkleMoteSprite();

        for (int i = 0; i < count; i++)
        {
            var moteName = $"Mote_{i}";
            var moteChild = GetOrCreateChild(container, moteName, typeof(Image));

            var rt = moteChild.GetComponent<RectTransform>();
            float size = UnityEngine.Random.Range(4.5f, 8.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            Vector2 rndPos = new Vector2(
                UnityEngine.Random.Range(-460f, 460f),
                UnityEngine.Random.Range(-85f, 85f)
            );
            rt.anchoredPosition = rndPos;

            var img = moteChild.GetComponent<Image>();
            img.sprite = moteSprite;
            img.raycastTarget = false;
            moteChild.SetActive(false);

            _moteImages[i] = img;
            _moteBasePositions[i] = rndPos;
            _moteSpeeds[i] = UnityEngine.Random.Range(0.8f, 1.6f);
            _motePhaseOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }
    }

    #endregion
}
