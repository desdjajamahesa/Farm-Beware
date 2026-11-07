using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Features.Vehicles.PureLogic;
using FarmBeware.Core.Runtime;

namespace FarmBeware.Features.Vehicles.UI
{
    /// <summary>
    /// Lightweight procedural HUD for vehicle telemetry (Speedometer, Gear, Keybinds).
    /// Dynamically binds or builds a sleek driving interface with zero external prefab dependencies.
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleDrivingHUD : MonoBehaviour
    {
        private static VehicleDrivingHUD _instance;
        public static VehicleDrivingHUD Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        private Canvas _hudCanvas;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _speedText;
        private TextMeshProUGUI _gearText;
        private TextMeshProUGUI _hintText;
        private Image _speedBarFill;

        private float _targetAlpha = 0f;
        private bool _isInitialized = false;
        private bool _isDrivingActive = false;
        public bool IsDrivingActive => _isDrivingActive;

        private ICutsceneService _subscribedCutsceneService;
        private int _lastKmh = -1;
        private VehicleDynamicsCore.GearState _lastGear = (VehicleDynamicsCore.GearState)(-1);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            _instance = this;

            EnsureHUDHierarchy();
            SnapHidden();
        }

        private void OnEnable()
        {
            EnsureHUDHierarchy();
            SubscribeCutsceneEvents();
            if (!ShouldBeVisible())
            {
                SnapHidden();
            }
        }

        private void OnDisable()
        {
            UnsubscribeCutsceneEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeCutsceneEvents();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void SubscribeCutsceneEvents()
        {
            if (ServiceLocator.TryResolve<ICutsceneService>(out var cutscene) && cutscene != null)
            {
                if (_subscribedCutsceneService != cutscene)
                {
                    if (_subscribedCutsceneService != null)
                    {
                        _subscribedCutsceneService.OnCutsceneStarted -= HandleCutsceneStarted;
                        _subscribedCutsceneService.OnCutsceneEnded -= HandleCutsceneEnded;
                    }
                    _subscribedCutsceneService = cutscene;
                    _subscribedCutsceneService.OnCutsceneStarted += HandleCutsceneStarted;
                    _subscribedCutsceneService.OnCutsceneEnded += HandleCutsceneEnded;
                }
            }

            if (FeaturesStoryline.Cutscenes.OpeningCutsceneController.HasInstance)
            {
                var opening = FeaturesStoryline.Cutscenes.OpeningCutsceneController.Instance;
                opening.OnCutsceneStarted -= HandleCutsceneStarted;
                opening.OnCutsceneStarted += HandleCutsceneStarted;
                opening.OnCutsceneEnded -= HandleCutsceneEnded;
                opening.OnCutsceneEnded += HandleCutsceneEnded;
            }

            if (FeaturesStoryline.Cutscenes.Night1CutsceneController.HasInstance)
            {
                var night1 = FeaturesStoryline.Cutscenes.Night1CutsceneController.Instance;
                night1.OnCutsceneStarted -= HandleCutsceneStarted;
                night1.OnCutsceneStarted += HandleCutsceneStarted;
                night1.OnCutsceneEnded -= HandleCutsceneEnded;
                night1.OnCutsceneEnded += HandleCutsceneEnded;
            }
        }

        private void UnsubscribeCutsceneEvents()
        {
            if (_subscribedCutsceneService != null)
            {
                _subscribedCutsceneService.OnCutsceneStarted -= HandleCutsceneStarted;
                _subscribedCutsceneService.OnCutsceneEnded -= HandleCutsceneEnded;
                _subscribedCutsceneService = null;
            }

            if (FeaturesStoryline.Cutscenes.OpeningCutsceneController.HasInstance)
            {
                var opening = FeaturesStoryline.Cutscenes.OpeningCutsceneController.Instance;
                opening.OnCutsceneStarted -= HandleCutsceneStarted;
                opening.OnCutsceneEnded -= HandleCutsceneEnded;
            }

            if (FeaturesStoryline.Cutscenes.Night1CutsceneController.HasInstance)
            {
                var night1 = FeaturesStoryline.Cutscenes.Night1CutsceneController.Instance;
                night1.OnCutsceneStarted -= HandleCutsceneStarted;
                night1.OnCutsceneEnded -= HandleCutsceneEnded;
            }
        }

        private void HandleCutsceneStarted()
        {
            SnapHidden();
        }

        private void HandleCutsceneEnded()
        {
            if (ShouldBeVisible())
            {
                Show();
            }
        }

        /// <summary>
        /// Inquires whether any cutscene is currently running across known providers.
        /// </summary>
        public bool IsCutsceneActive()
        {
            if (ServiceLocator.TryResolve<ICutsceneService>(out var cutscene) && cutscene != null)
            {
                if (cutscene.IsCutscenePlaying) return true;
            }
            if (FeaturesStoryline.Cutscenes.OpeningCutsceneController.HasInstance &&
                FeaturesStoryline.Cutscenes.OpeningCutsceneController.Instance.IsCutscenePlaying)
            {
                return true;
            }
            if (FeaturesStoryline.Cutscenes.Night1CutsceneController.HasInstance &&
                FeaturesStoryline.Cutscenes.Night1CutsceneController.Instance.IsCutscenePlaying)
            {
                return true;
            }
            if (FeaturesStoryline.StorylineManager.Instance != null &&
                FeaturesStoryline.StorylineManager.Instance.IsCutsceneActive())
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// True strictly when driving is active and no cutscene is currently executing.
        /// </summary>
        public bool ShouldBeVisible()
        {
            return _isDrivingActive && !IsCutsceneActive();
        }

        /// <summary>
        /// Immediately forces the HUD canvas and canvas group completely invisible with zero delay.
        /// </summary>
        public void SnapHidden()
        {
            _targetAlpha = 0f;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            if (_hudCanvas != null)
            {
                _hudCanvas.enabled = false;
                if (_hudCanvas.gameObject.activeSelf)
                {
                    _hudCanvas.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (_subscribedCutsceneService == null)
            {
                SubscribeCutsceneEvents();
            }

            if (!ShouldBeVisible())
            {
                if (_canvasGroup != null && _canvasGroup.alpha > 0f)
                {
                    SnapHidden();
                }
                else if (_hudCanvas != null && _hudCanvas.enabled)
                {
                    _hudCanvas.enabled = false;
                }
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, Time.deltaTime * 6f);
                bool shouldBeActive = _canvasGroup.alpha > 0.001f;
                if (_hudCanvas != null && _hudCanvas.enabled != shouldBeActive)
                {
                    _hudCanvas.enabled = shouldBeActive;
                }
            }
        }

        public void Show()
        {
            _isDrivingActive = true;
            EnsureHUDHierarchy();
            SubscribeCutsceneEvents();

            if (!ShouldBeVisible())
            {
                SnapHidden();
                return;
            }

            _targetAlpha = 1f;
            if (_hudCanvas != null)
            {
                if (!_hudCanvas.gameObject.activeSelf)
                {
                    _hudCanvas.gameObject.SetActive(true);
                }
                _hudCanvas.enabled = true;
            }
        }

        public void Hide()
        {
            _isDrivingActive = false;
            _targetAlpha = 0f;
        }

        public void SetDrivingActive(bool active)
        {
            _isDrivingActive = active;
            if (!active)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void UpdateTelemetry(float speedMps, VehicleDynamicsCore.GearState gear)
        {
            if (!ShouldBeVisible()) return;

            if (!_isInitialized) EnsureHUDHierarchy();
            if (_hudCanvas == null || !_hudCanvas.enabled) return;

            float kmh = Mathf.Abs(speedMps) * 3.6f;
            int roundedKmh = Mathf.RoundToInt(kmh);

            if (_speedText != null && roundedKmh != _lastKmh)
            {
                _lastKmh = roundedKmh;
                _speedText.text = $"{roundedKmh} <size=16>KM/H</size>";
            }

            if (_speedBarFill != null)
            {
                _speedBarFill.fillAmount = Mathf.Clamp01(kmh / 80f);
            }

            if (_gearText != null && gear != _lastGear)
            {
                _lastGear = gear;
                switch (gear)
                {
                    case VehicleDynamicsCore.GearState.Reverse:
                        _gearText.text = "<color=#FF5555>[R]</color>";
                        break;
                    case VehicleDynamicsCore.GearState.Park:
                        _gearText.text = "<color=#EEDD88>[P]</color>";
                        break;
                    default:
                        _gearText.text = "<color=#55FF77>[D]</color>";
                        break;
                }
            }
        }

        private void EnsureHUDHierarchy()
        {
            if (_isInitialized && _hudCanvas != null) return;

            // Search for existing canvas even if inactive, and eliminate any duplicates
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i] != null && allCanvases[i].gameObject.name == "VehicleDrivingCanvas")
                {
                    if (_hudCanvas == null)
                    {
                        _hudCanvas = allCanvases[i];
                    }
                    else if (allCanvases[i] != _hudCanvas)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(allCanvases[i].gameObject);
                        }
                        else
                        {
                            DestroyImmediate(allCanvases[i].gameObject);
                        }
                    }
                }
            }

            GameObject canvasGo = _hudCanvas != null ? _hudCanvas.gameObject : null;
            if (canvasGo == null)
            {
                canvasGo = new GameObject("VehicleDrivingCanvas");
                var uiRoot = GameObject.Find("_UI");
                if (uiRoot != null)
                {
                    canvasGo.transform.SetParent(uiRoot.transform, false);
                }
            }
            else
            {
                var uiRoot = GameObject.Find("_UI");
                if (uiRoot != null && canvasGo.transform.parent == null)
                {
                    canvasGo.transform.SetParent(uiRoot.transform, false);
                }
            }

            _hudCanvas = canvasGo.GetComponent<Canvas>();
            if (_hudCanvas == null)
            {
                _hudCanvas = canvasGo.AddComponent<Canvas>();
                _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _hudCanvas.sortingOrder = 95;
            }

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            _canvasGroup = canvasGo.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _targetAlpha = 0f;

            // HUD Container panel at bottom right
            var panelTransform = canvasGo.transform.Find("DrivingPanel");
            GameObject panelGo;
            if (panelTransform == null)
            {
                panelGo = new GameObject("DrivingPanel");
                panelGo.transform.SetParent(canvasGo.transform, false);
                var rect = panelGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-40f, 40f);
                rect.sizeDelta = new Vector2(280f, 130f);

                var bg = panelGo.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.10f, 0.12f, 0.85f);
            }
            else
            {
                panelGo = panelTransform.gameObject;
            }

            // Gear Display Text
            var gearTransform = panelGo.transform.Find("GearText");
            if (gearTransform == null)
            {
                var gearGo = new GameObject("GearText");
                gearGo.transform.SetParent(panelGo.transform, false);
                var rect = gearGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0.35f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = new Vector2(12f, 0f);
                rect.offsetMax = new Vector2(0f, -8f);

                _gearText = gearGo.AddComponent<TextMeshProUGUI>();
                _gearText.fontSize = 36;
                _gearText.fontStyle = FontStyles.Bold;
                _gearText.alignment = TextAlignmentOptions.Center;
                _gearText.text = "<color=#EEDD88>[P]</color>";
            }
            else
            {
                _gearText = gearTransform.GetComponent<TextMeshProUGUI>();
            }

            // Speed Display Text
            var speedTransform = panelGo.transform.Find("SpeedText");
            if (speedTransform == null)
            {
                var speedGo = new GameObject("SpeedText");
                speedGo.transform.SetParent(panelGo.transform, false);
                var rect = speedGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.35f, 0.5f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = new Vector2(0f, 0f);
                rect.offsetMax = new Vector2(-16f, -8f);

                _speedText = speedGo.AddComponent<TextMeshProUGUI>();
                _speedText.fontSize = 32;
                _speedText.fontStyle = FontStyles.Bold;
                _speedText.alignment = TextAlignmentOptions.Right;
                _speedText.text = "0 <size=16>KM/H</size>";
            }
            else
            {
                _speedText = speedTransform.GetComponent<TextMeshProUGUI>();
            }

            // Speed Bar Track & Fill
            var barTrackTransform = panelGo.transform.Find("SpeedBarTrack");
            if (barTrackTransform == null)
            {
                var trackGo = new GameObject("SpeedBarTrack");
                trackGo.transform.SetParent(panelGo.transform, false);
                var rect = trackGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.42f);
                rect.anchorMax = new Vector2(1f, 0.48f);
                rect.offsetMin = new Vector2(16f, 0f);
                rect.offsetMax = new Vector2(-16f, 0f);

                var trackImg = trackGo.AddComponent<Image>();
                trackImg.color = new Color(0.2f, 0.25f, 0.3f, 0.8f);

                var fillGo = new GameObject("SpeedBarFill");
                fillGo.transform.SetParent(trackGo.transform, false);
                var fillRect = fillGo.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;

                _speedBarFill = fillGo.AddComponent<Image>();
                _speedBarFill.color = new Color(0.3f, 0.85f, 1.0f, 0.95f);
                _speedBarFill.type = Image.Type.Filled;
                _speedBarFill.fillMethod = Image.FillMethod.Horizontal;
                _speedBarFill.fillOrigin = 0;
                _speedBarFill.fillAmount = 0f;
            }
            else
            {
                _speedBarFill = barTrackTransform.Find("SpeedBarFill")?.GetComponent<Image>();
            }

            // Hint Keybinds Text
            var hintTransform = panelGo.transform.Find("HintText");
            if (hintTransform == null)
            {
                var hintGo = new GameObject("HintText");
                hintGo.transform.SetParent(panelGo.transform, false);
                var rect = hintGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0.40f);
                rect.offsetMin = new Vector2(12f, 6f);
                rect.offsetMax = new Vector2(-12f, 0f);

                _hintText = hintGo.AddComponent<TextMeshProUGUI>();
                _hintText.fontSize = 12;
                _hintText.color = new Color(0.75f, 0.82f, 0.88f, 0.9f);
                _hintText.alignment = TextAlignmentOptions.Center;
                _hintText.text = "<b>[W/S]</b> Drive  <b>[A/D]</b> Steer  <b>[SPACE]</b> Drift\n<b>[L]</b> Lights  <b>[H]</b> Horn  <b>[E]</b> Exit";
            }
            else
            {
                _hintText = hintTransform.GetComponent<TextMeshProUGUI>();
            }

            _hudCanvas.enabled = false;
            if (_hudCanvas.gameObject.activeSelf)
            {
                _hudCanvas.gameObject.SetActive(false);
            }
            _isInitialized = true;
        }
    }
}
