using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FeaturesCommon
{
    /// <summary>
    /// Manages a 100% full-viewport organic cloud transition.
    /// Completely blankets the camera viewport including all four corners and all edges
    /// using deeply overlapping, natural, irregular fluffy cloud formations.
    /// Pure horizontal movement (Left ⟷ Right), with zero box/rectangular panels or dark gaps.
    /// </summary>
    [DisallowMultipleComponent]
    public class CloudTransitionManager : MonoBehaviour
    {
        public static CloudTransitionManager Instance { get; private set; }

        [Header("Cinematic Timing Settings")]
        [Tooltip("Durasi awan meluncur masuk menutup layar secara perlahan (detik)")]
        [SerializeField] private float coverDuration = 0.40f;

        [Tooltip("Durasi awan perlahan membuka ke arah luar layar (detik)")]
        [SerializeField] private float revealDuration = 0.40f;

        [Tooltip("Jeda waktu setelah scene selesai dimuat sebelum awan mulai membuka")]
        [SerializeField] private float revealDelayAfterSceneLoad = 0.05f;

        [Header("Cloud Sprites (Organic Feathered Silhouettes)")]
        [SerializeField] private Sprite spriteFoundA;
        [SerializeField] private Sprite spriteFoundB;
        [SerializeField] private Sprite spriteCorner;
        [SerializeField] private Sprite spriteLargeA;
        [SerializeField] private Sprite spriteLargeB;
        [SerializeField] private Sprite spriteMediumA;
        [SerializeField] private Sprite spriteMediumB;
        [SerializeField] private Sprite spritePuff;
        [SerializeField] private Sprite spriteWisp;

        [Header("Visual & Color")]
        [SerializeField] private Color cloudTintColor = new Color(0.98f, 0.99f, 1.0f, 1.0f);

        [Header("Audio Settings")]
        [SerializeField] private bool playSoundEffects = true;
        [SerializeField] private AudioClip customCoverSfx;
        [SerializeField] private AudioClip customRevealSfx;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.50f;

        // UI hierarchy references
        private Canvas transitionCanvas;
        private CanvasScaler canvasScaler;
        private CanvasGroup canvasGroup;
        private RectTransform rootContainer;

        // Individual cloud item data
        private class CloudItem
        {
            public RectTransform rect;
            public Image image;
            public float fixedY;
            public float openX;
            public float closedX;
            public float speedWeight;
            public float baseAlpha;
            public float floatFreq;
            public float floatAmp;
            public float floatPhase;
        }

        private CloudItem[] leftClouds;
        private CloudItem[] rightClouds;

        private AudioSource audioSource;
        private Coroutine activeTransitionCoroutine;
        private bool isCoveringScreen = false;
        private bool isTransitioning = false;
        private bool isTransitioningToScene = false;
        private bool shouldRevealOnReady = false;
        private float idleTimer = 0f;

        public bool IsTransitioning => isTransitioning;
        public bool IsScreenCovered => isCoveringScreen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInstance();
        }

        public static CloudTransitionManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<CloudTransitionManager>();
            if (existing != null)
            {
                Instance = existing;
                existing.Initialize();
                return Instance;
            }

            var prefab = Resources.Load<GameObject>("Prefabs/CloudTransitionCanvas");
            if (prefab != null)
            {
                var instantiated = Instantiate(prefab);
                instantiated.name = "CloudTransitionManager";
                Instance = instantiated.GetComponent<CloudTransitionManager>();
                if (Instance != null)
                {
                    Instance.Initialize();
                    return Instance;
                }
            }

            GameObject fallbackGo = new GameObject("CloudTransitionManager");
            var manager = fallbackGo.AddComponent<CloudTransitionManager>();
            manager.Initialize();
            return manager;
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            LoadSpritesIfNeeded();
            SetupUI();
            SetupAudio();

            if (Application.isPlaying)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneLoaded += HandleSceneLoaded;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                Instance = null;
            }
        }

        private void Update()
        {
            // Pure horizontal calm drifting while holding full coverage
            if (isCoveringScreen && !isTransitioning)
            {
                idleTimer += Time.unscaledDeltaTime;
                SetCloudProgress(1f, idleTimer);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (isTransitioningToScene)
            {
                // RoutineTransitionToScene is already orchestrating the load and reveal flow
                return;
            }

            if (isCoveringScreen || isTransitioning)
            {
                shouldRevealOnReady = true;
                StartCoroutine(RoutineAutoRevealOnSceneLoad());
            }
        }

        private IEnumerator RoutineAutoRevealOnSceneLoad()
        {
            while (isTransitioning && !isCoveringScreen)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(revealDelayAfterSceneLoad);

            if (shouldRevealOnReady && isCoveringScreen)
            {
                shouldRevealOnReady = false;
                PlayReveal(revealDuration);
            }
        }

        private void LoadSpritesIfNeeded()
        {
            if (spriteFoundA == null) spriteFoundA = Resources.Load<Sprite>("Textures/Clouds/Cloud_Massive_Foundation_A");
            if (spriteFoundB == null) spriteFoundB = Resources.Load<Sprite>("Textures/Clouds/Cloud_Massive_Foundation_B");
            if (spriteCorner == null) spriteCorner = Resources.Load<Sprite>("Textures/Clouds/Cloud_Corner_Billow");
            if (spriteLargeA == null) spriteLargeA = Resources.Load<Sprite>("Textures/Clouds/Cloud_Organic_Large_A");
            if (spriteLargeB == null) spriteLargeB = Resources.Load<Sprite>("Textures/Clouds/Cloud_Organic_Large_B");
            if (spriteMediumA == null) spriteMediumA = Resources.Load<Sprite>("Textures/Clouds/Cloud_Organic_Medium_A");
            if (spriteMediumB == null) spriteMediumB = Resources.Load<Sprite>("Textures/Clouds/Cloud_Organic_Medium_B");
            if (spritePuff == null) spritePuff = Resources.Load<Sprite>("Textures/Clouds/Cloud_Organic_Puff");
            if (spriteWisp == null) spriteWisp = Resources.Load<Sprite>("Textures/Clouds/Cloud_Organic_Wisp");
        }

        private void SetupAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void SetupUI()
        {
            transitionCanvas = GetComponent<Canvas>();
            if (transitionCanvas == null)
                transitionCanvas = gameObject.AddComponent<Canvas>();

            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 32760;

            canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler == null)
                canvasScaler = gameObject.AddComponent<CanvasScaler>();

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Clear previous children
            var existingRoot = transform.Find("CloudRoot");
            if (existingRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(existingRoot.gameObject);
                else
                    DestroyImmediate(existingRoot.gameObject);
            }

            GameObject rootGo = new GameObject("CloudRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            rootContainer = rootGo.GetComponent<RectTransform>();
            rootContainer.anchorMin = Vector2.zero;
            rootContainer.anchorMax = Vector2.one;
            rootContainer.sizeDelta = Vector2.zero;
            rootContainer.anchoredPosition = Vector2.zero;

            // Build Left & Right organic cloud formations guaranteeing 100% viewport coverage
            BuildCloudFormations();

            SetCloudProgress(0f);
        }

        private void BuildCloudFormations()
        {
            // The viewport on 1920x1080 extends X: -960 to +960, Y: -540 to +540.
            // When closed:
            // - Left clouds move from offscreen-left (-2300px) to cross past the center (+350px to +550px).
            // - Right clouds move from offscreen-right (+2300px) to cross past the center (-350px to -550px).
            // - Trailing edges of both left and right clouds stay far past screen borders (past +/-1200px).
            // - Top/Bottom edges extend to Y: +/-750px (far past +/-540px), thoroughly covering all 4 corners!

            // ==================== LEFT CLOUD SYSTEM ====================
            var leftList = new List<CloudItem>();

            // 1. Massive Foundation Base (Wide, tall, impenetrable volumetric core, Y: -320 to +320)
            leftList.Add(CreateCloudItem("L_Base_Core", spriteFoundA, new Vector2(2400, 1500), 0f, -2550f, -450f, 1.00f, 1.00f, 0.35f, 12f, 0.0f));
            leftList.Add(CreateCloudItem("L_Base_TopSpan", spriteFoundB, new Vector2(2200, 1300), 320f, -2500f, -500f, 0.96f, 0.98f, 0.38f, 10f, 1.1f));
            leftList.Add(CreateCloudItem("L_Base_BotSpan", spriteFoundB, new Vector2(2200, 1300), -320f, -2500f, -500f, 0.96f, 0.98f, 0.36f, 10f, 2.2f));

            // 2. Corner Coverage Clouds (Guarantees Top-Left & Bottom-Left corners are 100% blanketed)
            leftList.Add(CreateCloudItem("L_Corner_TopLeft", spriteCorner, new Vector2(1800, 1250), 420f, -2450f, -550f, 0.94f, 1.00f, 0.40f, 10f, 0.8f));
            leftList.Add(CreateCloudItem("L_Corner_BotLeft", spriteCorner, new Vector2(1800, 1250), -420f, -2450f, -550f, 0.94f, 1.00f, 0.42f, 10f, 2.7f));

            // 3. Midground Volumetric Cumulus Billows (Fluffy organic mounds reaching center)
            leftList.Add(CreateCloudItem("L_Mid_Upper", spriteLargeA, new Vector2(1600, 1000), 200f, -2350f, -350f, 1.02f, 1.00f, 0.48f, 14f, 1.5f));
            leftList.Add(CreateCloudItem("L_Mid_Center", spriteLargeB, new Vector2(1700, 1100), 0f, -2400f, -300f, 1.03f, 1.00f, 0.44f, 16f, 0.4f));
            leftList.Add(CreateCloudItem("L_Mid_Lower", spriteLargeA, new Vector2(1600, 1000), -200f, -2350f, -350f, 1.02f, 1.00f, 0.50f, 14f, 3.1f));

            // 4. Accent Puffs & Intermediate Fillers (Staggered elevations to eliminate all pinholes)
            leftList.Add(CreateCloudItem("L_Puff_High", spriteMediumA, new Vector2(1250, 850), 380f, -2250f, -380f, 1.05f, 0.98f, 0.55f, 12f, 1.8f));
            leftList.Add(CreateCloudItem("L_Puff_MidHigh", spriteMediumB, new Vector2(1250, 850), 120f, -2200f, -280f, 1.06f, 0.98f, 0.52f, 14f, 2.4f));
            leftList.Add(CreateCloudItem("L_Puff_MidLow", spriteMediumA, new Vector2(1250, 850), -120f, -2200f, -280f, 1.06f, 0.98f, 0.54f, 14f, 3.8f));
            leftList.Add(CreateCloudItem("L_Puff_Low", spriteMediumB, new Vector2(1250, 850), -380f, -2250f, -380f, 1.05f, 0.98f, 0.56f, 12f, 0.6f));

            // 5. Foreground Floating Wisps (Feathery airy tendrils drifting across camera)
            leftList.Add(CreateCloudItem("L_Wisp_Top", spriteWisp, new Vector2(1350, 650), 180f, -2300f, -220f, 1.14f, 0.85f, 0.60f, 16f, 1.2f));
            leftList.Add(CreateCloudItem("L_Wisp_Bot", spriteWisp, new Vector2(1350, 650), -180f, -2300f, -220f, 1.15f, 0.85f, 0.58f, 16f, 2.9f));
            leftList.Add(CreateCloudItem("L_Near_Puff", spritePuff, new Vector2(950, 950), -20f, -2150f, -250f, 1.12f, 0.92f, 0.65f, 15f, 0.3f));

            leftClouds = leftList.ToArray();

            // ==================== RIGHT CLOUD SYSTEM ====================
            var rightList = new List<CloudItem>();

            // 1. Massive Foundation Base (Wide, tall, impenetrable volumetric core)
            rightList.Add(CreateCloudItem("R_Base_Core", spriteFoundB, new Vector2(2400, 1500), 0f, 2550f, 450f, 1.00f, 1.00f, 0.35f, 12f, 0.7f));
            rightList.Add(CreateCloudItem("R_Base_TopSpan", spriteFoundA, new Vector2(2200, 1300), 340f, 2500f, 500f, 0.96f, 0.98f, 0.37f, 10f, 1.9f));
            rightList.Add(CreateCloudItem("R_Base_BotSpan", spriteFoundA, new Vector2(2200, 1300), -340f, 2500f, 500f, 0.96f, 0.98f, 0.39f, 10f, 3.0f));

            // 2. Corner Coverage Clouds (Guarantees Top-Right & Bottom-Right corners are 100% blanketed)
            rightList.Add(CreateCloudItem("R_Corner_TopRight", spriteCorner, new Vector2(1800, 1250), 430f, 2450f, 550f, 0.94f, 1.00f, 0.41f, 10f, 1.4f));
            rightList.Add(CreateCloudItem("R_Corner_BotRight", spriteCorner, new Vector2(1800, 1250), -430f, 2450f, 550f, 0.94f, 1.00f, 0.43f, 10f, 0.3f));

            // 3. Midground Volumetric Cumulus Billows
            rightList.Add(CreateCloudItem("R_Mid_Upper", spriteLargeB, new Vector2(1600, 1000), 220f, 2350f, 350f, 1.02f, 1.00f, 0.47f, 14f, 2.0f));
            rightList.Add(CreateCloudItem("R_Mid_Center", spriteLargeA, new Vector2(1700, 1100), -10f, 2400f, 300f, 1.03f, 1.00f, 0.45f, 16f, 0.9f));
            rightList.Add(CreateCloudItem("R_Mid_Lower", spriteLargeB, new Vector2(1600, 1000), -220f, 2350f, 350f, 1.02f, 1.00f, 0.49f, 14f, 2.5f));

            // 4. Accent Puffs & Intermediate Fillers
            rightList.Add(CreateCloudItem("R_Puff_High", spriteMediumB, new Vector2(1250, 850), 390f, 2250f, 380f, 1.05f, 0.98f, 0.54f, 12f, 2.2f));
            rightList.Add(CreateCloudItem("R_Puff_MidHigh", spriteMediumA, new Vector2(1250, 850), 130f, 2200f, 280f, 1.06f, 0.98f, 0.51f, 14f, 0.5f));
            rightList.Add(CreateCloudItem("R_Puff_MidLow", spriteMediumB, new Vector2(1250, 850), -130f, 2200f, 280f, 1.06f, 0.98f, 0.53f, 14f, 1.7f));
            rightList.Add(CreateCloudItem("R_Puff_Low", spriteMediumA, new Vector2(1250, 850), -390f, 2250f, 380f, 1.05f, 0.98f, 0.55f, 12f, 3.3f));

            // 5. Foreground Floating Wisps
            rightList.Add(CreateCloudItem("R_Wisp_Top", spriteWisp, new Vector2(1350, 650), 170f, 2300f, 220f, 1.14f, 0.85f, 0.61f, 16f, 2.6f));
            rightList.Add(CreateCloudItem("R_Wisp_Bot", spriteWisp, new Vector2(1350, 650), -190f, 2300f, 220f, 1.15f, 0.85f, 0.57f, 16f, 0.8f));
            rightList.Add(CreateCloudItem("R_Near_Puff", spritePuff, new Vector2(950, 950), 30f, 2150f, 250f, 1.12f, 0.92f, 0.64f, 15f, 1.9f));

            rightClouds = rightList.ToArray();
        }

        private CloudItem CreateCloudItem(string name, Sprite sprite, Vector2 size, float fixedY, float openX, float closedX, float speedWeight, float baseAlpha, float floatFreq, float floatAmp, float floatPhase)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rootContainer, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            // Pure horizontal: fixed Y elevation, initial offscreen X
            rt.anchoredPosition = new Vector2(openX, fixedY);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(cloudTintColor.r, cloudTintColor.g, cloudTintColor.b, 0f);
            img.raycastTarget = false;

            return new CloudItem
            {
                rect = rt,
                image = img,
                fixedY = fixedY,
                openX = openX,
                closedX = closedX,
                speedWeight = speedWeight,
                baseAlpha = baseAlpha,
                floatFreq = floatFreq,
                floatAmp = floatAmp,
                floatPhase = floatPhase
            };
        }

        /// <summary>
        /// Updates pure horizontal positions of all organic cloud items.
        /// Progress: 0.0 (fully open/offscreen) to 1.0 (fully closed/overlapping across center).
        /// </summary>
        public void SetCloudProgress(float progress, float idleTime = 0f)
        {
            float baseEase = SmootherStep(progress);

            // Update Left Cloud System
            if (leftClouds != null)
            {
                for (int i = 0; i < leftClouds.Length; i++)
                {
                    var c = leftClouds[i];
                    if (c.rect == null) continue;

                    float layerProgress = Mathf.Approximately(c.speedWeight, 1f)
                        ? baseEase
                        : Mathf.Pow(baseEase, 1f / Mathf.Max(0.1f, c.speedWeight));
                    float curX = Mathf.Lerp(c.openX, c.closedX, layerProgress);

                    if (progress >= 0.95f && idleTime > 0f)
                    {
                        float driftX = Mathf.Sin(idleTime * c.floatFreq + c.floatPhase) * c.floatAmp;
                        curX += driftX;
                    }

                    c.rect.anchoredPosition = new Vector2(curX, c.fixedY);

                    if (c.image != null)
                    {
                        float alphaMul = Mathf.Clamp01(progress * 4f);
                        Color col = c.image.color;
                        col.a = c.baseAlpha * alphaMul;
                        c.image.color = col;
                    }
                }
            }

            // Update Right Cloud System
            if (rightClouds != null)
            {
                for (int i = 0; i < rightClouds.Length; i++)
                {
                    var c = rightClouds[i];
                    if (c.rect == null) continue;

                    float layerProgress = Mathf.Approximately(c.speedWeight, 1f)
                        ? baseEase
                        : Mathf.Pow(baseEase, 1f / Mathf.Max(0.1f, c.speedWeight));
                    float curX = Mathf.Lerp(c.openX, c.closedX, layerProgress);

                    if (progress >= 0.95f && idleTime > 0f)
                    {
                        float driftX = Mathf.Cos(idleTime * c.floatFreq + c.floatPhase) * c.floatAmp;
                        curX += driftX;
                    }

                    c.rect.anchoredPosition = new Vector2(curX, c.fixedY);

                    if (c.image != null)
                    {
                        float alphaMul = Mathf.Clamp01(progress * 4f);
                        Color col = c.image.color;
                        col.a = c.baseAlpha * alphaMul;
                        c.image.color = col;
                    }
                }
            }
        }

        private static float SmootherStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (6f * t - 15f) + 10f);
        }

        #region Public Transition Controls

        /// <summary>
        /// Plays slow, smooth horizontal cloud cover animation.
        /// </summary>
        public Coroutine PlayCover(float duration = -1f, Action onComplete = null)
        {
            if (duration <= 0f) duration = coverDuration;
            if (activeTransitionCoroutine != null) StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = StartCoroutine(RoutinePlayCover(duration, onComplete));
            return activeTransitionCoroutine;
        }

        /// <summary>
        /// Plays slow, majestic cinematic cloud reveal outward horizontally.
        /// </summary>
        public Coroutine PlayReveal(float duration = -1f, Action onComplete = null)
        {
            if (duration <= 0f) duration = revealDuration;
            if (activeTransitionCoroutine != null) StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = StartCoroutine(RoutinePlayReveal(duration, onComplete));
            return activeTransitionCoroutine;
        }

        /// <summary>
        /// Covers screen with organic clouds, asynchronously loads target scene, and reveals game.
        /// </summary>
        public void TransitionToScene(string targetSceneName, Action onCovered = null, Action onRevealed = null)
        {
            if (activeTransitionCoroutine != null) StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = StartCoroutine(RoutineTransitionToScene(targetSceneName, onCovered, onRevealed));
        }

        public static void LoadSceneWithClouds(string sceneName)
        {
            EnsureInstance().TransitionToScene(sceneName);
        }

        public void PreviewProgress(float progress, float idleTime = 0f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = progress > 0.001f ? 1f : 0f;
                canvasGroup.blocksRaycasts = progress > 0.5f;
            }
            SetCloudProgress(progress, idleTime);
        }

        #endregion

        #region Coroutines

        private IEnumerator RoutinePlayCover(float duration, Action onComplete)
        {
            isTransitioning = true;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            PlayCoverSound();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                SetCloudProgress(t, elapsed);
                yield return null;
            }

            SetCloudProgress(1f, 0f);
            isCoveringScreen = true;
            isTransitioning = false;
            activeTransitionCoroutine = null;

            onComplete?.Invoke();
        }

        private IEnumerator RoutinePlayReveal(float duration, Action onComplete)
        {
            isTransitioning = true;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            PlayRevealSound();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Reversed smoother step: clouds gently glide outwards to the left & right
                float progress = 1f - t;
                SetCloudProgress(progress, elapsed);

                yield return null;
            }

            SetCloudProgress(0f, 0f);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            isCoveringScreen = false;
            isTransitioning = false;
            activeTransitionCoroutine = null;

            onComplete?.Invoke();
        }

        private IEnumerator RoutineTransitionToScene(string targetSceneName, Action onCovered, Action onRevealed)
        {
            isTransitioningToScene = true;
            yield return RoutinePlayCover(coverDuration, onCovered);

            // Screen is organically blanketed by clouds. Load scene in background.
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
                float holdTime = 0f;
                while (!asyncLoad.isDone)
                {
                    holdTime += Time.unscaledDeltaTime;
                    SetCloudProgress(1f, holdTime);
                    yield return null;
                }
            }

            // Cinematic settle pause
            yield return new WaitForSecondsRealtime(revealDelayAfterSceneLoad);

            yield return RoutinePlayReveal(revealDuration, onRevealed);
            isTransitioningToScene = false;
        }

        #endregion

        #region Procedural Audio

        private void PlayCoverSound()
        {
            if (!playSoundEffects || audioSource == null) return;
            AudioClip clip = customCoverSfx != null ? customCoverSfx : GetOrCreateCoverClip();
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, sfxVolume);
            }
        }

        private void PlayRevealSound()
        {
            if (!playSoundEffects || audioSource == null) return;
            AudioClip clip = customRevealSfx != null ? customRevealSfx : GetOrCreateRevealClip();
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, sfxVolume * 0.85f);
            }
        }

        private static AudioClip cachedCoverClip;
        private static AudioClip GetOrCreateCoverClip()
        {
            if (cachedCoverClip != null) return cachedCoverClip;

            int sampleRate = 44100;
            float duration = 0.42f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            var rnd = new System.Random(42);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                float env = Mathf.Sin(progress * Mathf.PI);
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float hum = Mathf.Sin(2f * Mathf.PI * 135f * t);

                samples[i] = (noise * 0.25f + hum * 0.18f) * env * 0.35f;
            }

            cachedCoverClip = AudioClip.Create("CinematicCloudWhoosh", count, 1, sampleRate, false);
            cachedCoverClip.SetData(samples, 0);
            return cachedCoverClip;
        }

        private static AudioClip cachedRevealClip;
        private static AudioClip GetOrCreateRevealClip()
        {
            if (cachedRevealClip != null) return cachedRevealClip;

            int sampleRate = 44100;
            float duration = 0.45f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            float[] freqs = new float[] { 349.23f, 440.00f, 523.25f, 659.25f, 880.00f };

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 5.0f);

                float signal = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float noteTime = Mathf.Max(0f, t - f * 0.04f);
                    float noteEnv = Mathf.Exp(-noteTime * 6.0f);
                    signal += Mathf.Sin(2f * Mathf.PI * freqs[f] * noteTime) * noteEnv * 0.18f;
                }

                float air = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.05f;
                samples[i] = (signal + air) * 0.35f;
            }

            cachedRevealClip = AudioClip.Create("CinematicCloudChime", count, 1, sampleRate, false);
            cachedRevealClip.SetData(samples, 0);
            return cachedRevealClip;
        }

        #endregion
    }
}
