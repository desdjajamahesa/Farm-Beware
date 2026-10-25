using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.InputSystem;
using FarmBeware.Core.Runtime;
using FeaturesCamera;

namespace FeaturesStoryline.Cutscenes
{
    /// <summary>
    /// Thin MonoBehaviour Adapter orchestrating the Unity Timeline opening cutscene.
    /// Manages PlayableDirector execution, hold-to-skip input tracking, cinematic letterbox presentation,
    /// player gate repositioning, and zero-GC narrative blackboard fact updates.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayableDirector))]
    public class OpeningCutsceneController : MonoBehaviour, ICutsceneService
    {
        private static OpeningCutsceneController _instance;
        public static OpeningCutsceneController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<OpeningCutsceneController>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var systems = GameObject.Find("_SYSTEMS");
                        var go = new GameObject("OpeningCutsceneController");
                        if (systems != null)
                        {
                            go.transform.SetParent(systems.transform, false);
                        }
                        _instance = go.AddComponent<OpeningCutsceneController>();
                    }
                }
                if (_instance != null && !_instance.gameObject.activeInHierarchy)
                {
                    _instance.gameObject.SetActive(true);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Timeline Configuration")]
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private PlayableAsset openingTimelineAsset;
        [SerializeField] private float skipHoldThreshold = 0.5f;

        [Header("UI Canvas")]
        [SerializeField] private Canvas targetCanvas;
        public Canvas TargetCanvas
        {
            get => targetCanvas;
            set => targetCanvas = value;
        }

        [Header("Spawn Position (Outside Gate)")]
        [SerializeField] private Vector3 gateArrivalPosition = new Vector3(22.40f, 0.04f, 53.50f);
        [SerializeField] private Vector3 gateArrivalRotationEuler = new Vector3(0f, 180f, 0f);

        [Header("Vehicle Arrival Path (Left to Right along Road)")]
        [SerializeField] private Vector3 vehicleRoadStartPosition = new Vector3(-2.00f, 0.04f, 53.50f);
        [SerializeField] private Vector3 vehicleGateStopPosition = new Vector3(20.40f, 0.04f, 53.50f);
        [SerializeField] private Vector3 vehicleRoadRotationEuler = new Vector3(0f, 90f, 0f);

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private float _skipHoldTimer = 0f;
        private bool _isCutscenePlaying = false;
        private Coroutine _cutsceneRoutine;
        private Action _onCompleteCallback;
        private readonly System.Collections.Generic.List<GameObject> _hiddenHudPanels = new System.Collections.Generic.List<GameObject>();

        public bool IsCutscenePlaying => _isCutscenePlaying;
        public event Action OnCutsceneStarted;
        public event Action OnCutsceneEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ServiceLocator.Register<ICutsceneService>(this);

            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }

            if (playableDirector != null)
            {
                playableDirector.playOnAwake = false;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D Stereo
            }

            EnsureTimelineAssetLoaded();
        }

        private IPlayerContext GetValidPlayerContext()
        {
            var ctx = ServiceLocator.Resolve<IPlayerContext>();
            if (ctx != null && (ctx as Component) != null)
            {
                return ctx;
            }
            var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            return playerObj != null ? playerObj.GetComponent<IPlayerContext>() : null;
        }

        private void OnDisable()
        {
            if (_isCutscenePlaying)
            {
                _isCutscenePlaying = false;
                _skipHoldTimer = 0f;
                StopAllCoroutines();
                _cutsceneRoutine = null;
            }
        }

        private void OnDestroy()
        {
            CleanupCameraAnimator();
            var player = GetValidPlayerContext();
            if (player != null)
            {
                player.RemoveInputLock("OpeningCutscene");
            }
            if (Instance == this)
            {
                ServiceLocator.Unregister<ICutsceneService>();
                Instance = null;
            }
        }

        private void Update()
        {
            if (!_isCutscenePlaying) return;

            UpdateHoldToSkip();
        }

        private void UpdateHoldToSkip()
        {
            bool isHolding = false;
            bool skipInstant = false;

            // Unity Input System support
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    skipInstant = true;
                }
                if (Keyboard.current.spaceKey.isPressed)
                {
                    isHolding = true;
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                isHolding = true;
            }

            if (skipInstant)
            {
                Debug.Log("[OpeningCutsceneController] ⏭️ Instant skip requested (ESC). Skipping cutscene.");
                SkipCurrentCutscene();
                return;
            }

            if (isHolding)
            {
                _skipHoldTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(_skipHoldTimer / skipHoldThreshold);

                if (CutsceneLetterboxUI.Instance != null)
                {
                    CutsceneLetterboxUI.Instance.UpdateSkipProgress(progress);
                }

                if (_skipHoldTimer >= skipHoldThreshold)
                {
                    Debug.Log("[OpeningCutsceneController] ⏭️ Hold-to-skip completed. Skipping cutscene.");
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
        /// Initiates the Day 0 Arrival Timeline sequence.
        /// Repositions player outside the gate, locks movement input, switches camera to Cinematic,
        /// and coordinates widescreen letterbox subtitles.
        /// </summary>
        public void PlayOpeningCutscene(Action onComplete = null)
        {
            if (_isCutscenePlaying) return;

            _onCompleteCallback = onComplete;
            EnsureTimelineAssetLoaded();

            _isCutscenePlaying = true;
            _skipHoldTimer = 0f;

            OnCutsceneStarted?.Invoke();

            // 1. Position vehicle at road start and seat player inside
            InitializeVehicleAndPlayerForCutscene();

            // 2. Lock player input
            var player = GetValidPlayerContext();
            if (player != null)
            {
                player.AddInputLock("OpeningCutscene");
            }

            // 3. Switch camera to Cinematic Mode & Perspective Projection (50mm Cinema Lens)
            var cam = ServiceLocator.Resolve<ICameraService>();
            if (cam != null)
            {
                cam.SetMode(CameraMode.Cinematic);
            }
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = false;
                mainCam.fieldOfView = 50.0f;
                mainCam.nearClipPlane = 0.3f;
                mainCam.farClipPlane = 1000.0f;
            }

            // 4. Reveal Letterbox and Skip Gauge
            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.EnsureStoryCardObjects();
                CutsceneLetterboxUI.Instance.SetLetterboxVisible(true, 0f);
                CutsceneLetterboxUI.Instance.SetSkipVisible(true);
                CutsceneLetterboxUI.Instance.UpdateSkipProgress(0f);
            }

            // Hide gameplay HUD during cutscene presentation
            SetGameplayHUDVisible(false);

            // 5. Wire Timeline PlayableDirector with Activation Tracks and Audio Track (StupidaZZle Reference)
            if (playableDirector != null && openingTimelineAsset != null)
            {
                playableDirector.playableAsset = openingTimelineAsset;

                var cards = CutsceneLetterboxUI.Instance != null ? CutsceneLetterboxUI.Instance.StoryCardObjects : null;
                int cardIndex = 0;

                foreach (var output in openingTimelineAsset.outputs)
                {
                    if (output.sourceObject is ActivationTrack actTrack && cards != null && cardIndex < cards.Length)
                    {
                        if (cards[cardIndex] != null)
                        {
                            playableDirector.SetGenericBinding(actTrack, cards[cardIndex]);
                        }
                        cardIndex++;
                    }
                    else if (output.sourceObject is AudioTrack audioTrack && audioSource != null)
                    {
                        playableDirector.SetGenericBinding(audioTrack, audioSource.gameObject);
                    }
                }

                playableDirector.time = 0;
                playableDirector.Play();
            }
            else
            {
                // Fallback: Play dawn atmospheric chord if PlayableDirector not configured
                PlayAtmosphericAudio();
            }

            if (_cutsceneRoutine != null) StopCoroutine(_cutsceneRoutine);
            _cutsceneRoutine = StartCoroutine(RoutineCutsceneSequence());
        }

        private void PurgePlayerEquipmentAndInventoryForDay0(GameObject playerObj)
        {
            if (playerObj == null) return;
            var inv = playerObj.GetComponent<InventoryComponent>();
            if (inv != null && inv.slots != null)
            {
                for (int i = 0; i < inv.slots.Count; i++)
                {
                    inv.slots[i] = new InventorySlot();
                }
                inv.selectedHotbarIndex = 0;
                inv.OnInventoryChanged?.Invoke();
            }

            var equip = playerObj.GetComponent("PlayerEquipment");
            if (equip != null)
            {
                equip.GetType().GetMethod("DestroyCurrentWeapon")?.Invoke(equip, null);
                equip.GetType().GetMethod("UpdateEquipmentVisual")?.Invoke(equip, new object[] { 0 });
            }
            playerObj.SendMessage("EnsureStarterKit", 0, SendMessageOptions.DontRequireReceiver);
        }

        private void InitializeVehicleAndPlayerForCutscene()
        {
            var truckObj = GameObject.Find("OldFarmPickup_Truck");
            var playerCtx = GetValidPlayerContext();
            var playerObj = (playerCtx as Component)?.gameObject ?? GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            var vehicleCtrl = truckObj != null ? truckObj.GetComponent<IVehicleActor>() : null;

            if (playerObj != null)
            {
                PurgePlayerEquipmentAndInventoryForDay0(playerObj);
            }

            if (truckObj != null && vehicleCtrl != null)
            {
                vehicleCtrl.Teleport(vehicleRoadStartPosition, Quaternion.Euler(vehicleRoadRotationEuler));
                if (playerObj != null && !vehicleCtrl.IsDriverOccupied)
                {
                    vehicleCtrl.EnterVehicle(playerObj);
                }
                vehicleCtrl.SetHeadlights(true);
            }
            else if (playerObj != null)
            {
                RepositionPlayerAtGate();
            }
        }

        /// <summary>
        /// Immediately skips the active cutscene, restoring gameplay mode and positioning.
        /// </summary>
        public void SkipCurrentCutscene()
        {
            if (!_isCutscenePlaying) return;

            // 1. Immediately abort all camera sweeps and cutscene coroutines
            StopAllCoroutines();
            _cutsceneRoutine = null;

            // 2. Halt Timeline playback unconditionally
            if (playableDirector != null)
            {
                playableDirector.Stop();
                playableDirector.time = 0;
            }

            // 3. Remove dynamically attached Camera Animator
            CleanupCameraAnimator();

            // 4. Snap truck to parked position and dismount player cleanly
            var truckObj = GameObject.Find("OldFarmPickup_Truck");
            var vehicleCtrl = truckObj != null ? truckObj.GetComponent<IVehicleActor>() : null;
            if (vehicleCtrl != null)
            {
                vehicleCtrl.Teleport(vehicleGateStopPosition, Quaternion.Euler(vehicleRoadRotationEuler));
                if (vehicleCtrl.IsDriverOccupied)
                {
                    vehicleCtrl.ExitVehicle();
                }
            }

            // 5. Complete cutscene with instant skip mode
            CompleteCutscene(isSkipped: true);
        }

        private IEnumerator RoutineCutsceneSequence()
        {
            var ui = CutsceneLetterboxUI.Instance;
            var cam = Camera.main;
            Transform camTransform = cam != null ? cam.transform : null;

            var truckObj = GameObject.Find("OldFarmPickup_Truck");
            var vehicleCtrl = truckObj != null ? truckObj.GetComponent<IVehicleActor>() : null;
            var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");

            Quaternion roadRot = Quaternion.Euler(vehicleRoadRotationEuler);

            // Ensure truck & player are properly positioned at start of road (West, facing East)
            if (vehicleCtrl != null)
            {
                vehicleCtrl.Teleport(vehicleRoadStartPosition, roadRot);
                if (playerObj != null && !vehicleCtrl.IsDriverOccupied)
                {
                    vehicleCtrl.EnterVehicle(playerObj);
                }
                vehicleCtrl.SetHeadlights(true);
            }

            // -------------------------------------------------------------
            // PHASE 1: INTRO STORY TIMELINE (StupidaZZle Reference)
            // Displays sequential story sentences over dark canvas with ambient audio
            // -------------------------------------------------------------
            if (ui != null)
            {
                ui.SetIntroBackdropVisible(true, 0.1f);
            }

            if (playableDirector != null && playableDirector.playableAsset != null && playableDirector.state == PlayState.Playing)
            {
                double targetDuration = playableDirector.duration > 0 ? playableDirector.duration : 14.5;
                while (playableDirector != null && playableDirector.state == PlayState.Playing && playableDirector.time < targetDuration)
                {
                    yield return null;
                }
            }
            else
            {
                // Fallback coroutine sequence if PlayableDirector is absent
                if (ui != null) ui.SetIntroStoryCard("Ten years in the army was enough. At thirty, I just wanted a quiet life.", 0.6f);
                yield return new WaitForSecondsRealtime(3.2f);
                if (ui != null) ui.ClearIntroStoryCard(0.4f);
                yield return new WaitForSecondsRealtime(0.4f);

                if (ui != null) ui.SetIntroStoryCard("I used all my savings to buy a cheap piece of land far away from the city.", 0.6f);
                yield return new WaitForSecondsRealtime(3.2f);
                if (ui != null) ui.ClearIntroStoryCard(0.4f);
                yield return new WaitForSecondsRealtime(0.4f);

                if (ui != null) ui.SetIntroStoryCard("For a while, everything went fine. I cleared the weeds and built a few small farm plots.", 0.6f);
                yield return new WaitForSecondsRealtime(3.2f);
                if (ui != null) ui.ClearIntroStoryCard(0.4f);
                yield return new WaitForSecondsRealtime(0.4f);

                if (ui != null) ui.SetIntroStoryCard("Life was peaceful... until the day I started digging in an untouched part of the field.", 0.6f);
                yield return new WaitForSecondsRealtime(3.2f);
                if (ui != null) ui.ClearIntroStoryCard(0.4f);
                yield return new WaitForSecondsRealtime(0.4f);
            }

            // Crossfade out the dark backdrop into the 3D dawn world
            if (ui != null)
            {
                ui.SetIntroBackdropVisible(false, 1.0f);
            }

            // -------------------------------------------------------------
            // PHASE 2: 3D VEHICLE ARRIVAL (LEFT TO RIGHT TRACKING SHOT)
            // -------------------------------------------------------------
            // SHOT 1 (0.0s - 4.5s): Vehicle approach from Left along perimeter road
            // Subtitle: "Finally back from town with the last box of supplies and fresh seeds."
            // -------------------------------------------------------------
            if (ui != null)
            {
                ui.SetSubtitle("Finally back from town with the last box of supplies and fresh seeds.");
            }

            // Frame camera North of road looking South-East across the road at truck approach
            Vector3 shot1CamStart = new Vector3(4.0f, 3.8f, 62.5f);
            Vector3 shot1CamEnd = new Vector3(12.0f, 3.5f, 61.2f);
            if (camTransform != null)
            {
                camTransform.position = shot1CamStart;
                Vector3 initialLook = vehicleRoadStartPosition + Vector3.up * 1.1f;
                camTransform.rotation = Quaternion.LookRotation(initialLook - camTransform.position);
            }

            float shot1Duration = 4.5f;
            float elapsed1 = 0f;
            Vector3 shot1TruckEnd = Vector3.Lerp(vehicleRoadStartPosition, vehicleGateStopPosition, 0.55f);

            while (elapsed1 < shot1Duration)
            {
                elapsed1 += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed1 / shot1Duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (truckObj != null && vehicleCtrl != null)
                {
                    Vector3 currentTruckPos = Vector3.Lerp(vehicleRoadStartPosition, shot1TruckEnd, smoothT);
                    vehicleCtrl.Teleport(currentTruckPos, roadRot);
                }

                if (camTransform != null)
                {
                    Vector3 targetLook = (truckObj != null) ? truckObj.transform.position + Vector3.up * 1.1f : new Vector3(10.0f, 1.2f, 53.5f);
                    Vector3 targetCamPos = Vector3.Lerp(shot1CamStart, shot1CamEnd, smoothT);
                    camTransform.position = Vector3.Lerp(camTransform.position, targetCamPos, Time.unscaledDeltaTime * 4f);
                    Vector3 dir = targetLook - camTransform.position;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, Quaternion.LookRotation(dir), Time.unscaledDeltaTime * 6f);
                    }
                }
                yield return null;
            }

            // -------------------------------------------------------------
            // SHOT 2 (4.5s - 9.0s): Approaching gate, deceleration curve, stops outside gate
            // Subtitle: "The land was dirt cheap because an old factory used to stand near here decades ago."
            // -------------------------------------------------------------
            if (ui != null)
            {
                ui.SetSubtitle("The land was dirt cheap because an old factory used to stand near here decades ago.");
            }

            float shot2Duration = 4.5f;
            float elapsed2 = 0f;
            Vector3 shot2CamEnd = new Vector3(16.5f, 2.8f, 59.5f);

            while (elapsed2 < shot2Duration)
            {
                elapsed2 += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed2 / shot2Duration);
                float easeOutT = 1f - Mathf.Pow(1f - t, 3f);

                if (truckObj != null && vehicleCtrl != null)
                {
                    Vector3 currentTruckPos = Vector3.Lerp(shot1TruckEnd, vehicleGateStopPosition, easeOutT);
                    vehicleCtrl.Teleport(currentTruckPos, roadRot);
                }

                if (camTransform != null)
                {
                    Vector3 targetCamPos = Vector3.Lerp(shot1CamEnd, shot2CamEnd, easeOutT);
                    Vector3 targetLook = (truckObj != null) ? truckObj.transform.position + Vector3.up * 1.1f : vehicleGateStopPosition + Vector3.up * 1.0f;
                    camTransform.position = Vector3.Lerp(camTransform.position, targetCamPos, Time.unscaledDeltaTime * 3f);
                    Vector3 dir = targetLook - camTransform.position;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, Quaternion.LookRotation(dir), Time.unscaledDeltaTime * 5f);
                    }
                }
                yield return null;
            }

            if (truckObj != null && vehicleCtrl != null)
            {
                vehicleCtrl.Teleport(vehicleGateStopPosition, roadRot);
            }

            // -------------------------------------------------------------
            // SHOT 3 (9.0s - 13.0s): Vehicle stopped, engine idles and cuts off, close on driver side
            // Subtitle: "Doesn't matter to me. No more orders, no more war—just my own cabin and open ground."
            // -------------------------------------------------------------
            if (ui != null)
            {
                ui.SetSubtitle("Doesn't matter to me. No more orders, no more war—just my own cabin and open ground.");
            }

            // Cut vehicle engine audio
            if (truckObj != null)
            {
                var engAudio = truckObj.transform.Find("Audio/Engine_Audio")?.GetComponent<AudioSource>() 
                               ?? truckObj.GetComponentInChildren<AudioSource>();
                if (engAudio != null && engAudio.isPlaying)
                {
                    engAudio.Stop();
                }
            }

            float shot3Duration = 4.0f;
            float elapsed3 = 0f;
            Vector3 shot3CamEnd = new Vector3(18.2f, 2.2f, 57.5f);
            Vector3 driverFocusPoint = new Vector3(20.4f, 1.1f, 54.0f); // North driver-side door / cabin window

            while (elapsed3 < shot3Duration)
            {
                elapsed3 += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed3 / shot3Duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (camTransform != null)
                {
                    Vector3 targetCamPos = Vector3.Lerp(shot2CamEnd, shot3CamEnd, smoothT);
                    camTransform.position = Vector3.Lerp(camTransform.position, targetCamPos, Time.unscaledDeltaTime * 4f);
                    Vector3 dir = driverFocusPoint - camTransform.position;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, Quaternion.LookRotation(dir), Time.unscaledDeltaTime * 6f);
                    }
                }
                yield return null;
            }

            // -------------------------------------------------------------
            // SHOT 4 (13.0s - 16.5s): Atomic player dismount onto soil outside gate
            // Subtitle: "Getting late. I'll get some sleep inside, and tomorrow I'll open up that new plot of dirt."
            // -------------------------------------------------------------
            if (ui != null)
            {
                ui.SetSubtitle("Getting late. I'll get some sleep inside, and tomorrow I'll open up that new plot of dirt.");
            }

            if (vehicleCtrl != null && vehicleCtrl.IsDriverOccupied)
            {
                vehicleCtrl.ExitVehicle();
                PurgePlayerEquipmentAndInventoryForDay0(playerObj);
            }
            else if (playerObj != null)
            {
                RepositionPlayerAtGate();
                PurgePlayerEquipmentAndInventoryForDay0(playerObj);
            }

            float shot4Duration = 3.5f;
            float elapsed4 = 0f;
            Vector3 shot4CamEnd = new Vector3(18.8f, 2.4f, 58.2f);

            while (elapsed4 < shot4Duration)
            {
                elapsed4 += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed4 / shot4Duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (camTransform != null && playerObj != null)
                {
                    Vector3 targetCamPos = Vector3.Lerp(shot3CamEnd, shot4CamEnd, smoothT);
                    Vector3 targetLook = playerObj.transform.position + Vector3.up * 1.0f;
                    camTransform.position = Vector3.Lerp(camTransform.position, targetCamPos, Time.unscaledDeltaTime * 3f);
                    Vector3 dir = targetLook - camTransform.position;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, Quaternion.LookRotation(dir), Time.unscaledDeltaTime * 5f);
                    }
                }
                yield return null;
            }

            // Concluding fade
            if (ui != null)
            {
                ui.ClearSubtitle(0.3f);
            }
            yield return new WaitForSecondsRealtime(0.5f);

            CompleteCutscene(isSkipped: false);
        }

        private IEnumerator RoutineCameraSweep(Transform cam, Vector3 startPos, Vector3 endPos, Vector3 targetLook, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                cam.position = Vector3.Lerp(startPos, endPos, t);
                Vector3 dir = targetLook - cam.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(dir), Time.unscaledDeltaTime * 6f);
                }
                yield return null;
            }
        }

        private void CompleteCutscene(bool isSkipped = false)
        {
            _isCutscenePlaying = false;
            _skipHoldTimer = 0f;

            // Stop all lingering cutscene routines if any
            StopAllCoroutines();
            _cutsceneRoutine = null;

            // Ensure dynamic camera animator is removed
            CleanupCameraAnimator();

            var truckObj = GameObject.Find("OldFarmPickup_Truck");
            var vehicleCtrl = truckObj != null ? truckObj.GetComponent<IVehicleActor>() : null;

            // 1. Guarantee truck is parked outside the gate and occupant dismounted
            if (vehicleCtrl != null)
            {
                vehicleCtrl.Teleport(vehicleGateStopPosition, Quaternion.Euler(vehicleRoadRotationEuler));
                if (vehicleCtrl.IsDriverOccupied)
                {
                    vehicleCtrl.ExitVehicle();
                }
            }
            RepositionPlayerAtGate();
            PurgePlayerEquipmentAndInventoryForDay0((GetValidPlayerContext() as Component)?.gameObject ?? GameObject.FindWithTag("Player"));

            // 2. Hide Letterbox and Intro story overlay cleanly with zero leftover elements
            if (CutsceneLetterboxUI.Instance != null)
            {
                CutsceneLetterboxUI.Instance.SnapLetterboxHidden();
            }

            // Restore gameplay HUD (will stay suppressed on Day 0 per game rules)
            SetGameplayHUDVisible(true);

            // Explicitly ensure player hotbar is governed by ShouldHotbarBeVisible()
            var invUI = UnityEngine.Object.FindFirstObjectByType<InventoryManagerUI>();
            if (invUI != null && invUI.playerHotbarContainer != null)
            {
                invUI.playerHotbarContainer.gameObject.SetActive(invUI.ShouldHotbarBeVisible());
            }

            // 4. Switch camera back to Gameplay & SNAP CAMERA IMMEDIATELY
            RestoreGameplayCameraProjection();

            var cam = ServiceLocator.Resolve<ICameraService>();
            if (cam != null)
            {
                cam.SetMode(CameraMode.Gameplay);
            }
            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.SnapToTarget();
            }

            // 5. Release player input lock
            var player = GetValidPlayerContext();
            if (player != null)
            {
                player.RemoveInputLock("OpeningCutscene");
            }

            // 6. Update Narrative Fact Blackboard
            if (ServiceLocator.TryResolve<INarrativeFactBoard>(out var facts) && facts != null)
            {
                facts.SetFact(NarrativeFactKey.IntroCutsceneCompleted, 1);
                facts.SetFact(NarrativeFactKey.NexaPhytoTruthLevel, 1);
            }
            else if (StorylineManager.Instance != null && StorylineManager.Instance.FactBoard != null)
            {
                StorylineManager.Instance.FactBoard.SetFact(NarrativeFactKey.IntroCutsceneCompleted, 1);
                StorylineManager.Instance.FactBoard.SetFact(NarrativeFactKey.NexaPhytoTruthLevel, 1);
            }

            // 7. Stop audio fadeout
            if (audioSource != null && audioSource.isPlaying)
            {
                if (isSkipped)
                {
                    audioSource.Stop();
                }
                else
                {
                    StartCoroutine(RoutineAudioFadeOut(audioSource, 0.5f));
                }
            }

            OnCutsceneEnded?.Invoke();
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;

            Debug.Log("[OpeningCutsceneController] ✅ Opening Cutscene completed. Day 0 gameplay begins.");
        }

        private void CleanupCameraAnimator()
        {
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                var anim = mainCam.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.enabled = false;
                    if (Application.isPlaying)
                    {
                        Destroy(anim);
                    }
                    else
                    {
                        DestroyImmediate(anim);
                    }
                }
            }
        }

        private void RestoreGameplayCameraProjection()
        {
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                bool isOrtho = true;
                float orthoSize = 6.0f;
                if (IsometricCameraController.Instance != null)
                {
                    isOrtho = IsometricCameraController.Instance.isOrthographic;
                    if (IsometricCameraController.Instance.orthographicSize > 0f)
                    {
                        orthoSize = IsometricCameraController.Instance.orthographicSize;
                    }
                }
                mainCam.orthographic = isOrtho;
                mainCam.orthographicSize = orthoSize;
                mainCam.nearClipPlane = 0.08f;
                mainCam.farClipPlane = 1000.0f;
            }
        }

        private void RepositionPlayerAtGate()
        {
            var player = GetValidPlayerContext();
            if (player != null)
            {
                player.Teleport(gateArrivalPosition, Quaternion.Euler(gateArrivalRotationEuler));
            }
            else
            {
                var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (playerObj != null)
                {
                    var rb = playerObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.position = gateArrivalPosition;
                        rb.rotation = Quaternion.Euler(gateArrivalRotationEuler);
                    }
                    playerObj.transform.position = gateArrivalPosition;
                    playerObj.transform.rotation = Quaternion.Euler(gateArrivalRotationEuler);
                    Physics.SyncTransforms();
                }
            }
        }

        private void EnsureTimelineAssetLoaded()
        {
            if (openingTimelineAsset == null)
            {
                openingTimelineAsset = Resources.Load<PlayableAsset>("Timelines/OpeningCutsceneTimeline");
            }
        }

        private void PlayAtmosphericAudio()
        {
            if (audioSource == null) return;

            // Generate or load atmospheric clip
            if (audioSource.clip == null)
            {
                var clip = Resources.Load<AudioClip>("Audio/Atmosphere/OpeningDawn");
                if (clip != null)
                {
                    audioSource.clip = clip;
                }
                else
                {
                    // Procedural subtle harmonic drone
                    audioSource.clip = CreateProceduralAtmosphericDrone();
                }
            }

            if (audioSource.clip != null)
            {
                audioSource.volume = 0.6f;
                audioSource.loop = false;
                audioSource.Play();
            }
        }

        private AudioClip CreateProceduralAtmosphericDrone()
        {
            int sampleRate = 44100;
            float duration = 14.5f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Harmonic chord: A2 (110Hz), E3 (164.81Hz), A3 (220Hz), C#4 (277.18Hz)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin((t / duration) * Mathf.PI); // Smooth bow envelope

                float drone = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.4f +
                              Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.25f +
                              Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.2f +
                              Mathf.Sin(2f * Mathf.PI * 277.18f * t) * 0.15f;

                // Subtle low-frequency wind fluctuation (0.4 Hz)
                float wind = Mathf.Sin(2f * Mathf.PI * 0.4f * t) * 0.1f;

                samples[i] = (drone + wind) * envelope * 0.25f;
            }

            AudioClip clip = AudioClip.Create("ProceduralDawnDrone", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private IEnumerator RoutineAudioFadeOut(AudioSource src, float duration)
        {
            float startVol = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            src.Stop();
            src.volume = startVol;
        }

        public void SetGameplayHUDVisible(bool visible)
        {
            var canvas = targetCanvas != null ? targetCanvas.gameObject : GameObject.Find("UI_Canvas");
            string[] hudPanelNames = {
                "HUD_HotbarPanel",
                "HUD_Hotbar",
                "HUD_CombatPhaseTracker",
                "HUD_BuffBar",
                "HUD_PlayerStatsPanel",
                "HUD_EnemyIndicators",
                "HUD_NightBrawlWaveTracker"
            };

            int currentDay = 0;
            if (FarmBeware.Core.Runtime.ServiceLocator.TryResolve<FarmBeware.Core.Runtime.ITimeService>(out var timeService) && timeService != null)
            {
                currentDay = timeService.CurrentDay;
            }
            else if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                currentDay = FeaturesTime.DayNightTimeManager.Instance.CurrentDay;
            }
            else if (TimeManager.Instance != null)
            {
                currentDay = TimeManager.Instance.currentDay;
            }

            // On Day 0, gameplay HUD is strictly suppressed regardless of requested visibility
            if (currentDay == 0 && visible)
            {
                visible = false;
            }

            if (!visible)
            {
                _hiddenHudPanels.Clear();
                if (canvas != null)
                {
                    foreach (var name in hudPanelNames)
                    {
                        var t = canvas.transform.Find(name);
                        if (t != null && t.gameObject.activeSelf)
                        {
                            _hiddenHudPanels.Add(t.gameObject);
                            t.gameObject.SetActive(false);
                        }
                    }
                }

                // Explicitly ensure player hotbar container is deactivated during cutscene
                var invUI = UnityEngine.Object.FindFirstObjectByType<InventoryManagerUI>();
                if (invUI != null && invUI.playerHotbarContainer != null)
                {
                    invUI.playerHotbarContainer.gameObject.SetActive(false);
                }
            }
            else
            {
                // Re-enable previously hidden panels
                foreach (var go in _hiddenHudPanels)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
                _hiddenHudPanels.Clear();

                // Failsafe & Guarantee: Ensure core gameplay HUD elements are always active after cutscene
                if (canvas != null)
                {
                    foreach (var name in hudPanelNames)
                    {
                        var t = canvas.transform.Find(name);
                        if (t != null && !t.gameObject.activeSelf)
                        {
                            t.gameObject.SetActive(true);
                        }
                    }
                }

                // Explicitly ensure hotbar follows ShouldHotbarBeVisible()
                var invUI = UnityEngine.Object.FindFirstObjectByType<InventoryManagerUI>();
                if (invUI != null && invUI.playerHotbarContainer != null)
                {
                    invUI.playerHotbarContainer.gameObject.SetActive(invUI.ShouldHotbarBeVisible());
                }
            }
        }

        /// <summary>
        /// Explicit helper to reveal gameplay HUD (e.g., when transitioning from Day 0 to Day 1).
        /// </summary>
        public void RevealGameplayHUD()
        {
            SetGameplayHUDVisible(true);
        }
    }
}
