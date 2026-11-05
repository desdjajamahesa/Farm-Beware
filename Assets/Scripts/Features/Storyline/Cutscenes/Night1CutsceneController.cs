using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmBeware.Core.Runtime;
using FeaturesCamera;
using FeaturesStoryline;
using FeaturesStoryline.Data;
using FeaturesStoryline.UI;

namespace FeaturesStoryline.Cutscenes
{
    /// <summary>
    /// In-engine cinematic cutscene controller for Night Day 1 ("WHAT IS THAT?!").
    /// Coordinates 4-shot camera choreography:
    /// Shot 1: Farmland tremor and shaking crops.
    /// Shot 2: Soil eruption VFX, emerging TuberMaw mutant sweet potato, and roar camera shake.
    /// Shot 3: Front gate camera pan showing night mist and approaching silhouettes.
    /// Shot 4: Ucup character reaction framed delivering canonical dialogue from StoryDatabase.D1_NIGHT_START.
    /// Restores CameraMode.Gameplay, releases input lock, sets Night1CutsceneCompleted fact,
    /// and seamlessly transitions into Night Brawl Wave 1 combat.
    /// </summary>
    [DisallowMultipleComponent]
    public class Night1CutsceneController : MonoBehaviour, ICutsceneService
    {
        private static Night1CutsceneController _instance;
        public static bool HasInstance
        {
            get
            {
                if (_instance != null) return true;
                _instance = FindFirstObjectByType<Night1CutsceneController>(FindObjectsInactive.Include);
                return _instance != null;
            }
        }
        private static bool _isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }

        public static Night1CutsceneController Instance
        {
            get
            {
                if (!Application.isPlaying) _isQuitting = false;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<Night1CutsceneController>(FindObjectsInactive.Include);
                    if (_instance == null && Application.isPlaying && !_isQuitting)
                    {
                        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        if (activeScene.isLoaded && activeScene.name != "MainMenuScene")
                        {
                            var systems = GameObject.Find("_SYSTEMS");
                            var go = new GameObject("Night1CutsceneController");
                            if (systems != null)
                            {
                                go.transform.SetParent(systems.transform, false);
                            }
                            _instance = go.AddComponent<Night1CutsceneController>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Cinematic Coordinates")]
        [SerializeField] private Vector3 farmlandPlotsFocusPosition = new Vector3(31.20f, 0.40f, 36.50f);
        [SerializeField] private Vector3 shot1CamPosition = new Vector3(31.20f, 3.20f, 29.50f);
        [SerializeField] private Vector3 emergencePointPosition = new Vector3(31.20f, 0.05f, 36.50f);
        [SerializeField] private Vector3 shot2CamPosition = new Vector3(29.80f, 2.20f, 32.50f);
        [SerializeField] private Vector3 frontGatePosition = new Vector3(21.90f, 1.00f, 64.60f);
        [SerializeField] private Vector3 shot3CamPosition = new Vector3(21.90f, 4.00f, 45.00f);

        [Header("Timing Configuration")]
        [SerializeField] private float shot1Duration = 3.5f;
        [SerializeField] private float shot2Duration = 4.5f;
        [SerializeField] private float shot3Duration = 3.5f;
        [SerializeField] private float shot4Duration = 3.5f;
        [SerializeField] private float skipHoldThreshold = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private bool _isCutscenePlaying = false;
        private float _skipHoldTimer = 0f;
        private Coroutine _cutsceneRoutine;
        private Action _onCompleteCallback;

        private GameObject _cinematicMutantActor;
        private GameObject _soilBurstVfxObj;
        private GameObject _gateMistVfxObj;
        private static Material _soilParticleMaterial;
        private static Material _mistParticleMaterial;

        public bool IsNight1CutscenePlaying => _isCutscenePlaying;
        public bool IsCutscenePlaying
        {
            get
            {
                if (_isCutscenePlaying) return true;
                if (OpeningCutsceneController.HasInstance && OpeningCutsceneController.Instance.IsOpeningCutscenePlaying)
                {
                    return true;
                }
                return false;
            }
        }
        public event Action OnCutsceneStarted;
        public event Action OnCutsceneEnded;

        public void NotifyCutsceneStarted() => OnCutsceneStarted?.Invoke();
        public void NotifyCutsceneEnded() => OnCutsceneEnded?.Invoke();

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private void Awake()
        {
            _isQuitting = false;
            if (Instance != null && Instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }
            Instance = this;

            if (ServiceLocator.Resolve<ICutsceneService>() == null)
            {
                ServiceLocator.Register<ICutsceneService>(this);
            }

            EnsureAudioSource();
        }

        private void OnEnable()
        {
            _isQuitting = false;
            if (ServiceLocator.Resolve<ICutsceneService>() == null)
            {
                ServiceLocator.Register<ICutsceneService>(this);
            }
        }

        private void OnDisable()
        {
            TeardownCutscene();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            TeardownCutscene();

            if (ReferenceEquals(ServiceLocator.Resolve<ICutsceneService>(), this))
            {
                if (OpeningCutsceneController.HasInstance && OpeningCutsceneController.Instance != null)
                {
                    ServiceLocator.Register<ICutsceneService>(OpeningCutsceneController.Instance);
                }
                else
                {
                    ServiceLocator.Unregister<ICutsceneService>();
                }
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Ensures complete, idempotent lifecycle teardown and state restoration if the controller
        /// is disabled or destroyed unexpectedly while a cutscene is in progress.
        /// Prevents dangling input locks, hidden HUD panels, locked cameras, and permanent dialogue softlocks.
        /// </summary>
        private void TeardownCutscene()
        {
            CleanupCinematicActors();

            if (!_isQuitting)
            {
                var player = GetValidPlayerContext();
                if (player != null)
                {
                    player.RemoveInputLock("Night1Cutscene");
                }
            }

            if (_isCutscenePlaying)
            {
                _isCutscenePlaying = false;
                _skipHoldTimer = 0f;

                if (_cutsceneRoutine != null)
                {
                    StopCoroutine(_cutsceneRoutine);
                    _cutsceneRoutine = null;
                }

                // Restore UI letterbox, active subtitles, and gameplay HUD
                if (CutsceneLetterboxUI.Instance != null)
                {
                    CutsceneLetterboxUI.Instance.ClearSubtitle(0.1f);
                    CutsceneLetterboxUI.Instance.SnapLetterboxHidden();
                }
                SetGameplayHUDVisible(true);

                // Restore Camera to Gameplay mode & orthographic projection
                var cam = ServiceLocator.Resolve<ICameraService>();
                if (cam != null)
                {
                    cam.SetMode(FarmBeware.Core.Runtime.CameraMode.Gameplay);
                }
                if (IsometricCameraController.Instance != null)
                {
                    IsometricCameraController.Instance.SnapToTarget();
                }
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.orthographic = true;
                }

                // Restore fallback cutscene service if available
                if (OpeningCutsceneController.HasInstance && ReferenceEquals(ServiceLocator.Resolve<ICutsceneService>(), this))
                {
                    ServiceLocator.Register<ICutsceneService>(OpeningCutsceneController.Instance);
                }

                // Dispatch cutscene termination events so deferred story beats unblock
                OnCutsceneEnded?.Invoke();

                // Unblock any asynchronous callers awaiting cutscene completion
                if (!_isQuitting)
                {
                    var callback = _onCompleteCallback;
                    _onCompleteCallback = null;
                    callback?.Invoke();
                }
                else
                {
                    _onCompleteCallback = null;
                }
            }
        }

        private void Update()
        {
            if (!_isCutscenePlaying) return;

            HandleSkipInput();
        }

        private void HandleSkipInput()
        {
            // 1. Instant Skip: [ESC] key
            bool instantSkip = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            if (instantSkip)
            {
                Debug.Log("[Night1CutsceneController] ⏭️ Instant skip requested (ESC). Skipping cutscene.");
                SkipCurrentCutscene();
                return;
            }

            // 2. Hold-to-Skip: [SPACE] or Left Mouse Button
            bool isHoldingSkip = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) ||
                                 (Mouse.current != null && Mouse.current.leftButton.isPressed);

            if (isHoldingSkip)
            {
                _skipHoldTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(_skipHoldTimer / skipHoldThreshold);

                if (CutsceneLetterboxUI.Instance != null)
                {
                    CutsceneLetterboxUI.Instance.UpdateSkipProgress(progress);
                }

                if (_skipHoldTimer >= skipHoldThreshold)
                {
                    Debug.Log("[Night1CutsceneController] ⏭️ Hold-to-skip completed. Skipping cutscene.");
                    SkipCurrentCutscene();
                    return;
                }
            }
            else if (_skipHoldTimer > 0f)
            {
                _skipHoldTimer = Mathf.MoveTowards(_skipHoldTimer, 0f, Time.unscaledDeltaTime * 4f);
                float progress = Mathf.Clamp01(_skipHoldTimer / skipHoldThreshold);

                if (CutsceneLetterboxUI.Instance != null)
                {
                    CutsceneLetterboxUI.Instance.UpdateSkipProgress(progress);
                }
            }
        }

        /// <summary>
        /// Plays the Night Day 1 in-engine cinematic cutscene.
        /// </summary>
        public void PlayNight1Cutscene(Action onComplete = null)
        {
            if (_isCutscenePlaying) return;

            _onCompleteCallback = onComplete;
            _isCutscenePlaying = true;
            _skipHoldTimer = 0f;

            ServiceLocator.Register<ICutsceneService>(this);
            OnCutsceneStarted?.Invoke();
            if (OpeningCutsceneController.HasInstance)
            {
                OpeningCutsceneController.Instance.NotifyCutsceneStarted();
            }

            // Immediately suppress gameplay and driving HUD
            SetGameplayHUDVisible(false);

            // Explicitly dismiss active dialogue and ensure modal windows are closed before camera switch
            if (StorylineUI.HasInstance && StorylineUI.Instance != null)
            {
                StorylineUI.Instance.DismissDialogue();
            }
            if (ServiceLocator.TryResolve<IModalStackService>(out var modalStack) && modalStack != null)
            {
                modalStack.CloseAll();
            }

            // Lock player movement input
            var player = GetValidPlayerContext();
            if (player != null)
            {
                player.AddInputLock("Night1Cutscene");
            }

            // Switch camera to Cinematic Mode & Perspective Projection (50mm Cinema Lens)
            var cam = ServiceLocator.Resolve<ICameraService>();
            if (cam != null)
            {
                cam.SetMode(FarmBeware.Core.Runtime.CameraMode.Cinematic);
            }
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = false;
                mainCam.fieldOfView = 50f;
            }

            // Initialize widescreen letterboxing UI
            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.SetLetterboxVisible(true, 0.5f);
                CutsceneLetterboxUI.Instance.SetSkipVisible(true);
                CutsceneLetterboxUI.Instance.UpdateSkipProgress(0f);
            }

            if (_cutsceneRoutine != null)
            {
                StopCoroutine(_cutsceneRoutine);
            }
            _cutsceneRoutine = StartCoroutine(RoutineNight1CinematicSequence());
        }

        public void PlayOpeningCutscene(Action onComplete = null)
        {
            if (OpeningCutsceneController.Instance != null)
            {
                OpeningCutsceneController.Instance.PlayOpeningCutscene(onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Instantly aborts the cutscene, restores gameplay camera and input locks, and marks completion.
        /// </summary>
        public void SkipCurrentCutscene()
        {
            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
            if (!_isCutscenePlaying) return;

            Debug.Log("[Night1CutsceneController] ⏭️ Skipping Night 1 cutscene and transitioning into combat.");
            CompleteCutscene();
        }

        private IEnumerator RoutineNight1CinematicSequence()
        {
            var mainCam = Camera.main;

            // =========================================================================
            // SHOT 1: Farmland plots with ground tremor / camera shake
            // =========================================================================
            Debug.Log("[Night1CutsceneController] 🎬 Shot 1: Farmland tremor and shaking crops.");
            PlayRumbleAudio();

            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.SetSubtitle("<i>Wait... the ground beneath the crops is shaking?!</i>", 0.35f);
            }

            float shot1Elapsed = 0f;
            while (shot1Elapsed < shot1Duration)
            {
                shot1Elapsed += Time.deltaTime;
                if (mainCam != null)
                {
                    float jitterX = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 0.08f;
                    float jitterY = (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * 0.08f;
                    Vector3 jitterOffset = new Vector3(jitterX, jitterY, 0f);

                    mainCam.transform.position = shot1CamPosition + jitterOffset;
                    mainCam.transform.LookAt(farmlandPlotsFocusPosition);
                }
                yield return null;
            }

            // =========================================================================
            // SHOT 2: Emergence point with procedural soil burst particle VFX, scaling mutant
            // =========================================================================
            Debug.Log("[Night1CutsceneController] 🎬 Shot 2: Sweet potato mutant emergence from soil.");
            TriggerSoilBurstVFX(emergencePointPosition);
            SpawnCinematicMutant(emergencePointPosition);
            PlayRoarAudio();

            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.SetSubtitle("<b>WHAT IS THAT?!</b> The sweet potatoes just ripped themselves out of the ground!", 0.25f);
            }

            float shot2Elapsed = 0f;
            while (shot2Elapsed < shot2Duration)
            {
                shot2Elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(shot2Elapsed / 1.5f);

                // Scale mutant up from subterranean burst
                if (_cinematicMutantActor != null)
                {
                    float scaleY = Mathf.Lerp(0.05f, 1.0f, progress);
                    float wobble = Mathf.Sin(shot2Elapsed * 15f) * 0.08f;
                    _cinematicMutantActor.transform.localScale = new Vector3(1f + wobble, scaleY, 1f - wobble);
                }

                if (mainCam != null)
                {
                    float shakeIntensity = Mathf.Lerp(0.25f, 0.04f, shot2Elapsed / shot2Duration);
                    float shakeX = (Mathf.PerlinNoise(Time.time * 28f, 10f) - 0.5f) * shakeIntensity;
                    float shakeY = (Mathf.PerlinNoise(10f, Time.time * 28f) - 0.5f) * shakeIntensity;

                    mainCam.transform.position = shot2CamPosition + new Vector3(shakeX, shakeY, 0f);
                    Vector3 lookTarget = emergencePointPosition + Vector3.up * Mathf.Lerp(0.2f, 1.2f, progress);
                    mainCam.transform.LookAt(lookTarget);
                }
                yield return null;
            }

            // =========================================================================
            // SHOT 3: Front gate focus showing night mist & approaching threat silhouettes
            // =========================================================================
            Debug.Log("[Night1CutsceneController] 🎬 Shot 3: Pan to front gate perimeter and silhouettes.");
            TriggerGateMistVFX(frontGatePosition);

            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.SetSubtitle("They turned into monsters! More are breaking through the front gate!", 0.35f);
            }

            float shot3Elapsed = 0f;
            Vector3 startCamPos = mainCam != null ? mainCam.transform.position : shot3CamPosition;
            Quaternion startCamRot = mainCam != null ? mainCam.transform.rotation : Quaternion.identity;
            Quaternion targetCamRot = Quaternion.LookRotation(frontGatePosition - shot3CamPosition);

            while (shot3Elapsed < shot3Duration)
            {
                shot3Elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, shot3Elapsed / 1.2f);

                if (mainCam != null)
                {
                    mainCam.transform.position = Vector3.Lerp(startCamPos, shot3CamPosition, t);
                    mainCam.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
                }
                yield return null;
            }

            // =========================================================================
            // SHOT 4: Ucup character reaction framed on player character
            // =========================================================================
            Debug.Log("[Night1CutsceneController] 🎬 Shot 4: Ucup shocked reaction and combat readiness.");
            var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            Vector3 playerPos = playerObj != null ? playerObj.transform.position : new Vector3(22.4f, 0.04f, 53.5f);
            var playerAnimator = playerObj != null ? playerObj.GetComponentInChildren<Animator>() : null;

            // Turn player character to face the emergence point
            Vector3 lookDir = (emergencePointPosition - playerPos);
            lookDir.y = 0f;
            Vector3 playerForward = lookDir.sqrMagnitude > 0.01f ? lookDir.normalized : (playerObj != null ? playerObj.transform.forward : Vector3.forward);
            if (playerObj != null)
            {
                playerObj.transform.rotation = Quaternion.LookRotation(playerForward);
            }

            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward).normalized;
            Vector3 playerHead = playerPos + Vector3.up * 1.35f;
            Vector3 playerChest = playerPos + Vector3.up * 1.05f;

            // Check indoor status to scale camera framing radius
            bool isIndoors = FeaturesCombat.NightBrawlManager.IsInsideHouse(playerPos);
            float distScale = isIndoors ? 0.65f : 1.0f;

            // Ideal camera framing: In front of Ucup (facing his face/torso), looking back at Ucup
            // Phase 1 (Shocked Face): Medium close-up focusing directly on Ucup's reaction
            Vector3 idealCamPosStart = playerHead + (playerForward * (1.55f * distScale)) + (playerRight * (0.35f * distScale)) + Vector3.up * 0.12f;
            Vector3 shot4LookPosStart = playerHead;

            // Phase 2 (Heroic Stance): Dynamic medium shot framing Ucup assuming defensive posture
            Vector3 idealCamPosHeroic = playerChest + (playerForward * (2.10f * distScale)) - (playerRight * (0.25f * distScale)) - Vector3.up * 0.15f;
            Vector3 shot4LookPosHeroic = playerChest;

            // Obstacle-clearance precalculation
            Vector3 shot4CamPosStart = CalculateObstacleFreeCamPos(playerHead, idealCamPosStart, playerObj);
            Vector3 shot4CamPosHeroic = CalculateObstacleFreeCamPos(playerChest, idealCamPosHeroic, playerObj);

            // Phase 1 of Shot 4: Immediate Shock & Disbelief
            PlayShockAudio();
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("HitReact");
            }

            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.SetSubtitle("<i>Wh-what the hell is that?! The crops... they've turned into monsters?!</i>", 0.25f);
            }

            float shot4Elapsed = 0f;
            bool enteredCombatStance = false;

            while (shot4Elapsed < shot4Duration)
            {
                shot4Elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(shot4Elapsed / shot4Duration);

                // At 1.5 seconds into shot: MC recovers military composure and assumes guard stance
                if (shot4Elapsed >= 1.5f && !enteredCombatStance)
                {
                    enteredCombatStance = true;
                    if (playerAnimator != null)
                    {
                        playerAnimator.SetBool("HasWeapon", true);
                        playerAnimator.SetTrigger("Parry");
                    }
                    if (CutsceneLetterboxUI.Instance != null)
                    {
                        CutsceneLetterboxUI.Instance.SetSubtitle("Good thing my army reflexes are still sharp—defend the farm!", 0.25f);
                    }
                }

                if (mainCam != null)
                {
                    Vector3 currentTargetCam = Vector3.Lerp(shot4CamPosStart, shot4CamPosHeroic, progress);
                    Vector3 currentTargetLook = Vector3.Lerp(shot4LookPosStart, shot4LookPosHeroic, progress);

                    // Dynamic obstruction avoidance per-frame
                    mainCam.transform.position = CalculateObstacleFreeCamPos(currentTargetLook, currentTargetCam, playerObj);
                    mainCam.transform.LookAt(currentTargetLook);
                }
                yield return null;
            }

            // Complete and transition into combat
            CompleteCutscene();
        }

        /// <summary>
        /// Mathematically calculates an obstacle-free camera position between focus point and target,
        /// ensuring zero occlusion by walls, chests, fences, or props during cinematic framing.
        /// Performs full 360-degree radial ray-probing if direct line of sight is obstructed.
        /// </summary>
        public static Vector3 CalculateObstacleFreeCamPos(Vector3 focusPoint, Vector3 idealCamPos, GameObject playerObj)
        {
            Vector3 offset = idealCamPos - focusPoint;
            float maxDist = offset.magnitude;
            if (maxDist < 0.05f) return idealCamPos;

            Vector3 dir = offset / maxDist;
            int mask = ~LayerMask.GetMask("Ignore Raycast", "UI", "TransparentFX", "PreviewLayer");

            RaycastHit[] hits = Physics.SphereCastAll(focusPoint, 0.18f, dir, maxDist, mask, QueryTriggerInteraction.Ignore);
            float closestValidDist = maxDist;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;

                // Ignore player colliders
                if (playerObj != null && (h.collider.gameObject == playerObj || h.collider.transform.IsChildOf(playerObj.transform)))
                {
                    continue;
                }

                if (h.distance < closestValidDist)
                {
                    closestValidDist = h.distance;
                }
            }

            // If direct line-of-sight has ample clearance (>= 1.25m), maintain preferred framing
            if (closestValidDist >= 1.25f && closestValidDist >= maxDist * 0.85f)
            {
                return idealCamPos;
            }

            // Direct path is cramped (< 1.25m) or occluded by wall, wardrobe, or storage chest.
            // Execute omnidirectional radial probe (16 horizontal azimuths x 22.5 deg + elevated pitch)
            // to find the guaranteed obstacle-free vantage point with maximum clear clearance.
            Vector3 bestPos = Vector3.zero;
            float bestClearance = 0f;
            float bestScore = -9999f;
            Vector3 idealHorizDir = new Vector3(dir.x, 0f, dir.z).normalized;
            if (idealHorizDir.sqrMagnitude < 0.001f) idealHorizDir = Vector3.forward;

            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 horizDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 testDir = (horizDir + Vector3.up * 0.35f).normalized;
                float testDist = Mathf.Max(1.80f, maxDist);

                float candClearance = testDist;
                RaycastHit[] candHits = Physics.SphereCastAll(focusPoint, 0.18f, testDir, testDist, mask, QueryTriggerInteraction.Ignore);
                for (int hIdx = 0; hIdx < candHits.Length; hIdx++)
                {
                    var ch = candHits[hIdx];
                    if (ch.collider == null) continue;
                    if (playerObj != null && (ch.collider.gameObject == playerObj || ch.collider.transform.IsChildOf(playerObj.transform)))
                        continue;
                    if (ch.distance < candClearance)
                    {
                        candClearance = ch.distance;
                    }
                }

                if (candClearance >= 1.15f)
                {
                    float alignment = Vector3.Dot(horizDir, idealHorizDir);
                    float score = candClearance + alignment * 0.6f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestClearance = candClearance;
                        bestPos = focusPoint + testDir * (candClearance - 0.15f);
                    }
                }
            }

            if (bestScore > -9990f && bestClearance >= 1.15f)
            {
                return bestPos;
            }

            // High-angle top-down fallback for extremely tight closets/corners
            Vector3 highAngleFallback = focusPoint + Vector3.up * 1.85f + idealHorizDir * 0.50f;
            RaycastHit[] highHits = Physics.SphereCastAll(focusPoint, 0.15f, (highAngleFallback - focusPoint).normalized, 1.90f, mask, QueryTriggerInteraction.Ignore);
            bool highHit = false;
            for (int hIdx = 0; hIdx < highHits.Length; hIdx++)
            {
                var ch = highHits[hIdx];
                if (ch.collider == null) continue;
                if (playerObj != null && (ch.collider.gameObject == playerObj || ch.collider.transform.IsChildOf(playerObj.transform))) continue;
                highHit = true;
                break;
            }
            if (!highHit)
            {
                return highAngleFallback;
            }

            float safeDist = Mathf.Clamp(closestValidDist - 0.20f, 0.35f, maxDist);
            return focusPoint + dir * safeDist;
        }

        private void CompleteCutscene()
        {
            if (_cutsceneRoutine != null)
            {
                StopCoroutine(_cutsceneRoutine);
                _cutsceneRoutine = null;
            }

            // 1. Clean up temporary cinematic actors and VFX
            CleanupCinematicActors();

            // 2. Snap UI letterbox hidden immediately
            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.ClearSubtitle(0.1f);
                CutsceneLetterboxUI.Instance.SnapLetterboxHidden();
            }

            // 3. Restore gameplay and driving HUD
            SetGameplayHUDVisible(true);

            // 4. Restore Camera to Gameplay mode
            var cam = ServiceLocator.Resolve<ICameraService>();
            if (cam != null)
            {
                cam.SetMode(FarmBeware.Core.Runtime.CameraMode.Gameplay);
            }
            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.SnapToTarget();
            }
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
            }

            // 5. Release player input lock
            var player = GetValidPlayerContext();
            if (player != null)
            {
                player.RemoveInputLock("Night1Cutscene");
            }

            // 6. Update narrative facts and canonical story beat
            if (ServiceLocator.TryResolve<INarrativeFactBoard>(out var facts) && facts != null)
            {
                facts.SetFact(NarrativeFactKey.Night1CutsceneCompleted, 1);
                facts.SetFact(NarrativeFactKey.NexaPhytoTruthLevel, 2);
            }
            if (StorylineManager.Instance != null && StorylineManager.Instance.Engine != null)
            {
                StorylineManager.Instance.Engine.RecordBeatTriggered("D1_NIGHT_START");
            }

            _isCutscenePlaying = false;
            _skipHoldTimer = 0f;

            if (OpeningCutsceneController.HasInstance && ReferenceEquals(ServiceLocator.Resolve<ICutsceneService>(), this))
            {
                ServiceLocator.Register<ICutsceneService>(OpeningCutsceneController.Instance);
            }

            OnCutsceneEnded?.Invoke();

            Debug.Log("[Night1CutsceneController] ✅ Night 1 Cutscene completed. Launching Wave 1 combat.");
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        }

        private void CleanupCinematicActors()
        {
            if (_cinematicMutantActor != null)
            {
                SafeDestroy(_cinematicMutantActor);
                _cinematicMutantActor = null;
            }
            if (_soilBurstVfxObj != null)
            {
                SafeDestroy(_soilBurstVfxObj);
                _soilBurstVfxObj = null;
            }
            if (_gateMistVfxObj != null)
            {
                SafeDestroy(_gateMistVfxObj);
                _gateMistVfxObj = null;
            }
        }

        private void TriggerSoilBurstVFX(Vector3 position)
        {
            if (_soilBurstVfxObj != null) SafeDestroy(_soilBurstVfxObj);

            _soilBurstVfxObj = new GameObject("VFX_SoilBurst_Cinematic");
            _soilBurstVfxObj.transform.SetParent(transform, true);
            _soilBurstVfxObj.transform.position = position;

            var ps = _soilBurstVfxObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startColor = new Color(0.32f, 0.20f, 0.12f, 1.0f); // Rich dark soil clumps
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 3.0f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 40),
                new ParticleSystem.Burst(0.2f, 25)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.4f;

            var renderer = _soilBurstVfxObj.GetComponent<ParticleSystemRenderer>();
            if (_soilParticleMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                if (s != null)
                {
                    _soilParticleMaterial = new Material(s);
                    _soilParticleMaterial.color = new Color(0.36f, 0.22f, 0.14f, 1f);
                }
            }
            if (_soilParticleMaterial != null)
            {
                renderer.material = _soilParticleMaterial;
            }

            ps.Play();
        }

        private void TriggerGateMistVFX(Vector3 position)
        {
            if (_gateMistVfxObj != null) SafeDestroy(_gateMistVfxObj);

            _gateMistVfxObj = new GameObject("VFX_GateMist_Cinematic");
            _gateMistVfxObj.transform.SetParent(transform, true);
            _gateMistVfxObj.transform.position = position;

            var ps = _gateMistVfxObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 2.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
            main.startColor = new Color(0.12f, 0.16f, 0.18f, 0.45f); // Night mist
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 15;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 1f, 4f);

            var renderer = _gateMistVfxObj.GetComponent<ParticleSystemRenderer>();
            if (_mistParticleMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                if (s != null)
                {
                    _mistParticleMaterial = new Material(s);
                    _mistParticleMaterial.color = new Color(0.15f, 0.18f, 0.20f, 0.4f);
                }
            }
            if (_mistParticleMaterial != null)
            {
                renderer.material = _mistParticleMaterial;
            }

            ps.Play();
        }

        private void SpawnCinematicMutant(Vector3 position)
        {
            if (_cinematicMutantActor != null) SafeDestroy(_cinematicMutantActor);

            var prefab = Resources.Load<GameObject>("Enemies/Enemy_TuberMaw");
            if (prefab != null)
            {
                _cinematicMutantActor = Instantiate(prefab, position, Quaternion.Euler(0f, 160f, 0f));
                _cinematicMutantActor.transform.SetParent(transform, true);
                // Disable combat AI adapters during cutscene so it doesn't wander or attack
                var enemyBase = _cinematicMutantActor.GetComponent<MonoBehaviour>();
                if (enemyBase != null)
                {
                    var colliders = _cinematicMutantActor.GetComponentsInChildren<Collider>();
                    foreach (var c in colliders) c.enabled = false;
                    var rbs = _cinematicMutantActor.GetComponentsInChildren<Rigidbody>();
                    foreach (var rb in rbs) rb.isKinematic = true;
                }
            }
            else
            {
                // Procedural fallback stand-in
                _cinematicMutantActor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _cinematicMutantActor.name = "Cinematic_TuberMaw_Standin";
                _cinematicMutantActor.transform.SetParent(transform, true);
                _cinematicMutantActor.transform.position = position + Vector3.up * 0.5f;
                _cinematicMutantActor.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
                var rend = _cinematicMutantActor.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.55f, 0.20f, 0.45f); // Sweet potato purple
                }
                var col = _cinematicMutantActor.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }

            _cinematicMutantActor.transform.localScale = new Vector3(1f, 0.05f, 1f);
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D Stereo Punch
            }
        }

        private void PlayRumbleAudio()
        {
            if (audioSource == null) return;
            var clip = GenerateRumbleAudioClip();
            audioSource.PlayOneShot(clip, 0.75f);
        }

        private void PlayRoarAudio()
        {
            if (audioSource == null) return;
            var clip = GenerateRoarAudioClip();
            audioSource.PlayOneShot(clip, 0.90f);
        }

        private AudioClip GenerateRumbleAudioClip()
        {
            int sampleRate = 22050;
            float duration = 1.5f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 45f + Mathf.Sin(t * 8f) * 15f;
                float envelope = Mathf.Sin(t / duration * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + noise) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SFX_GroundRumble", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateRoarAudioClip()
        {
            int sampleRate = 22050;
            float duration = 1.2f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(160f, 65f, t / duration);
                float envelope = Mathf.Pow(1f - (t / duration), 1.5f);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.35f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + harmonic + noise) * envelope * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_MutantRoar", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayShockAudio()
        {
            if (audioSource == null) return;
            var clip = GenerateShockAudioClip();
            audioSource.PlayOneShot(clip, 0.85f);
        }

        private AudioClip GenerateShockAudioClip()
        {
            int sampleRate = 22050;
            float duration = 0.9f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(340f, 80f, t / duration);
                float envelope = Mathf.Exp(-t * 5.0f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f * envelope;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + noise) * envelope * 0.75f;
            }

            AudioClip clip = AudioClip.Create("SFX_ShockStinger", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private readonly System.Collections.Generic.List<GameObject> _hiddenHudPanels = new System.Collections.Generic.List<GameObject>();

        public void SetGameplayHUDVisible(bool visible)
        {
            GameObject canvas = null;
            if (CutsceneLetterboxUI.Instance != null && CutsceneLetterboxUI.Instance.TargetCanvas != null)
            {
                canvas = CutsceneLetterboxUI.Instance.TargetCanvas.gameObject;
            }
            if (canvas == null)
            {
                canvas = GameObject.Find("Test_UI_Canvas") ?? GameObject.Find("UI_Canvas") ?? GameObject.Find("_UI/UI_Canvas");
            }

            string[] hudPanelNames = {
                "HUD_HotbarPanel",
                "HUD_Hotbar",
                "HUD_CombatPhaseTracker",
                "HUD_BuffBar",
                "HUD_PlayerStatsPanel",
                "HUD_EnemyIndicators",
                "HUD_NightBrawlWaveTracker"
            };

            if (!visible)
            {
                _hiddenHudPanels.Clear();
                foreach (var name in hudPanelNames)
                {
                    Transform t = canvas != null ? canvas.transform.Find(name) : null;
                    if (t == null)
                    {
                        var found = GameObject.Find(name);
                        if (found != null) t = found.transform;
                    }
                    if (t != null && t.gameObject.activeSelf)
                    {
                        _hiddenHudPanels.Add(t.gameObject);
                        t.gameObject.SetActive(false);
                    }
                }

                var invUI = UnityEngine.Object.FindFirstObjectByType<InventoryManagerUI>();
                if (invUI != null && invUI.playerHotbarContainer != null)
                {
                    invUI.playerHotbarContainer.gameObject.SetActive(false);
                }

                var hudHost = GameObject.Find("VehicleDrivingHUD_Host");
                if (hudHost != null)
                {
                    var hud = hudHost.GetComponent("VehicleDrivingHUD");
                    if (hud != null)
                    {
                        hud.GetType().GetMethod("SnapHidden", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.Invoke(hud, null);
                    }
                    else if (Application.isPlaying && hudHost.activeInHierarchy)
                    {
                        hudHost.SendMessage("SnapHidden", SendMessageOptions.DontRequireReceiver);
                    }
                }

                var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < allCanvases.Length; i++)
                {
                    if (allCanvases[i] == null) continue;

                    if (allCanvases[i].gameObject.name == "VehicleDrivingCanvas")
                    {
                        var hud = allCanvases[i].GetComponent("VehicleDrivingHUD");
                        if (hud != null)
                        {
                            hud.GetType().GetMethod("SnapHidden", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.Invoke(hud, null);
                        }
                        else if (Application.isPlaying && allCanvases[i].gameObject.activeInHierarchy)
                        {
                            allCanvases[i].gameObject.SendMessage("SnapHidden", SendMessageOptions.DontRequireReceiver);
                        }
                        allCanvases[i].gameObject.SetActive(false);
                        allCanvases[i].enabled = false;
                        var cg = allCanvases[i].GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 0f;
                    }

                    string[] modalPanelNames = { "BossIntroPanel", "StoryBannerPanel", "StoryDialoguePanel", "StoryOverlayPanel", "StoryDreamPanel" };
                    foreach (var mName in modalPanelNames)
                    {
                        var modalTransform = allCanvases[i].transform.Find(mName);
                        if (modalTransform != null)
                        {
                            modalTransform.gameObject.SetActive(false);
                            var cg = modalTransform.GetComponent<CanvasGroup>();
                            if (cg != null)
                            {
                                cg.alpha = 0f;
                                cg.blocksRaycasts = false;
                                cg.interactable = false;
                            }
                        }
                    }
                }

                // Modal sequence and dialogue suppression
                if (StorylineUI.HasInstance && StorylineUI.Instance != null)
                {
                    StorylineUI.Instance.DismissDialogue();
                }
                if (ServiceLocator.TryResolve<IModalStackService>(out var modalStackService) && modalStackService != null)
                {
                    modalStackService.CloseAll();
                }
                if (FeaturesStoryline.UI.BossIntroSequence.HasInstance && FeaturesStoryline.UI.BossIntroSequence.Instance != null)
                {
                    FeaturesStoryline.UI.BossIntroSequence.Instance.HideImmediate();
                }
            }
            else
            {
                foreach (var go in _hiddenHudPanels)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
                _hiddenHudPanels.Clear();

                foreach (var name in hudPanelNames)
                {
                    Transform t = canvas != null ? canvas.transform.Find(name) : null;
                    if (t == null)
                    {
                        var found = GameObject.Find(name);
                        if (found != null) t = found.transform;
                    }
                    if (t != null && !t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(true);
                    }
                }

                var invUI = UnityEngine.Object.FindFirstObjectByType<InventoryManagerUI>();
                if (invUI != null && invUI.playerHotbarContainer != null)
                {
                    invUI.playerHotbarContainer.gameObject.SetActive(invUI.ShouldHotbarBeVisible());
                }
            }
        }

        private IPlayerContext GetValidPlayerContext()
        {
            var ctx = ServiceLocator.Resolve<IPlayerContext>();
            if (ctx != null && (ctx as Component) != null)
            {
                return ctx;
            }
            if (!Application.isPlaying || _isQuitting || !gameObject.scene.isLoaded) return null;
            var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            return playerObj != null ? playerObj.GetComponent<IPlayerContext>() : null;
        }
    }
}
