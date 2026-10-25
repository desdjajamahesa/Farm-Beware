using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Features.Vehicles.PureLogic;

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

        private Canvas _hudCanvas;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _speedText;
        private TextMeshProUGUI _gearText;
        private TextMeshProUGUI _hintText;
        private Image _speedBarFill;

        private float _targetAlpha = 0f;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureHUDHierarchy();
        }

        private void Update()
        {
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
            EnsureHUDHierarchy();
            _targetAlpha = 1f;
            if (_hudCanvas != null) _hudCanvas.enabled = true;
        }

        public void Hide()
        {
            _targetAlpha = 0f;
        }

        public void UpdateTelemetry(float speedMps, VehicleDynamicsCore.GearState gear)
        {
            if (!_isInitialized) EnsureHUDHierarchy();

            float kmh = Mathf.Abs(speedMps) * 3.6f;
            if (_speedText != null)
            {
                _speedText.text = $"{Mathf.RoundToInt(kmh)} <size=16>KM/H</size>";
            }

            if (_speedBarFill != null)
            {
                _speedBarFill.fillAmount = Mathf.Clamp01(kmh / 80f);
            }

            if (_gearText != null)
            {
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

            var canvasGo = GameObject.Find("VehicleDrivingCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("VehicleDrivingCanvas");
                DontDestroyOnLoad(canvasGo);
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
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

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
            _isInitialized = true;
        }
    }
}
