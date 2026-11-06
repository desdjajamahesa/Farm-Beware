using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using FarmBeware.Core.Runtime;

namespace FarmBeware.Player.UI
{
    /// <summary>
    /// Thin UI Adapter for the World Map system.
    /// Implements IModalWindow for strict LIFO modal stacking, automatically halting player input
    /// while the map is active, and providing real-time player location, orientation tracking,
    /// and smooth mouse wheel zoom in / zoom out with panning.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class WorldMapUI : MonoBehaviour, IModalWindow
    {
        [Header("UI Structure")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private RectTransform mapViewport;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private Image mapImage;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private Transform poiContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private TextMeshProUGUI coordinatesText;
        [SerializeField] private TextMeshProUGUI zoomLevelText;

        [Header("Projection Spatial Bounds (Style C Orthographic)")]
        [SerializeField] private float minWorldX = -185.55556f;
        [SerializeField] private float maxWorldX = 45.55556f;
        [SerializeField] private float minWorldZ = -30.0f;
        [SerializeField] private float maxWorldZ = 100.0f;

        [Header("Input Key")]
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        [Header("Visual Animation")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private bool pulsePlayerMarker = true;

        [Header("Zoom and Pan Settings")]
        [SerializeField] private float minZoom = 1.0f;
        [SerializeField] private float maxZoom = 4.0f;
        [SerializeField] private float zoomSensitivity = 0.35f;
        [SerializeField] private float zoomSmoothing = 18.0f;

        [System.Serializable]
        public struct POIEntry
        {
            public string name;
            public Vector3 worldPosition;
            public Sprite icon;
            public Color tint;
        }

        [Header("Points of Interest")]
        [SerializeField] private List<POIEntry> defaultPOIs = new List<POIEntry>();

        private bool _isOpen = false;
        private Transform _cachedPlayerTransform;
        private IPlayerContext _cachedPlayerContext;
        private readonly List<GameObject> _spawnedPoiPins = new List<GameObject>();

        private float _currentZoom = 1.0f;
        private float _targetZoom = 1.0f;
        private Vector2 _panOffset = Vector2.zero;
        private Vector2 _targetPanOffset = Vector2.zero;
        private bool _isDragging = false;
        private Vector2 _lastMousePosition = Vector2.zero;

        public bool IsOpen => _isOpen;
        public bool CanDismissWithEscape => true;
        public float CurrentZoom => _currentZoom;
        public float TargetZoom => _targetZoom;
        public Vector2 PanOffset => _panOffset;

        public MapBounds2D MapBounds => new MapBounds2D(minWorldX, maxWorldX, minWorldZ, maxWorldZ);

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (closeButton != null) closeButton.onClick.AddListener(CloseModal);

            if (mapContent == null)
            {
                mapContent = mapContainer;
            }

            // Initially hidden
            SetVisibilityInstant(false);
        }

        private void Start()
        {
            ResolvePlayerReference();
            SetupDefaultPOIsIfEmpty();
            SpawnPOIMarkers();
        }

        private void OnDestroy()
        {
            if (_isOpen && ModalStackManager.HasInstance)
            {
                ModalStackManager.Instance.PopSpecific(this);
            }
        }

        private void Update()
        {
            HandleHotkeyInput();

            if (_isOpen)
            {
                HandleZoomAndPan();
                UpdatePlayerPinTracking();
                AnimatePlayerPinPulse();
                UpdateFadeAnimation();
            }
            else if (canvasGroup.alpha > 0f)
            {
                UpdateFadeAnimation();
            }
        }

        private void HandleHotkeyInput()
        {
            bool togglePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                togglePressed = true;
            }
#endif
            if (!togglePressed && toggleKey != KeyCode.None)
            {
                try
                {
                    if (Input.GetKeyDown(toggleKey))
                    {
                        togglePressed = true;
                    }
                }
                catch
                {
                    // Ignore legacy input manager exceptions when active input handler is new input system
                }
            }

            if (togglePressed)
            {
                if (_isOpen)
                {
                    CloseModal();
                }
                else
                {
                    OpenModal();
                }
            }
        }

        private void HandleZoomAndPan()
        {
            var contentTarget = mapContent != null ? mapContent : mapContainer;
            if (contentTarget == null) return;

            // 1. Mouse Scroll Wheel input for Zoom In / Zoom Out
            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.ReadValue().y;
            }
#endif
            if (Mathf.Abs(scroll) < 0.001f)
            {
                try { scroll = Input.mouseScrollDelta.y; } catch { }
            }

            Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }
#endif
            if (mousePos == Vector2.zero)
            {
                try { mousePos = Input.mousePosition; } catch { }
            }

            bool mouseInViewport = true;
            if (mapViewport != null)
            {
                mouseInViewport = RectTransformUtility.RectangleContainsScreenPoint(mapViewport, mousePos, null);
            }

            if (mouseInViewport && Mathf.Abs(scroll) > 0.01f)
            {
                float prevTargetZoom = _targetZoom;
                float zoomDelta = Mathf.Sign(scroll) * zoomSensitivity;
                _targetZoom = Mathf.Clamp(_targetZoom + zoomDelta, minZoom, maxZoom);

                // Zoom towards mouse cursor point inside viewport
                if (mapViewport != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport, mousePos, null, out var localCursor))
                {
                    float factor = _targetZoom / prevTargetZoom;
                    _targetPanOffset = localCursor + (_targetPanOffset - localCursor) * factor;
                }
            }

            // 2. Drag to Pan
            bool mouseDown = false;
            bool mousePressed = false;
            bool mouseReleased = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mouseDown = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;
                mousePressed = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame;
                mouseReleased = Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame || Mouse.current.middleButton.wasReleasedThisFrame;
            }
#endif
            try
            {
                if (!mouseDown)
                {
                    mouseDown = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                    mousePressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                    mouseReleased = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);
                }
            }
            catch { }

            if (mousePressed && mouseInViewport)
            {
                _isDragging = true;
                _lastMousePosition = mousePos;
            }

            if (_isDragging)
            {
                if (mouseDown)
                {
                    Vector2 delta = mousePos - _lastMousePosition;
                    _targetPanOffset += delta;
                    _lastMousePosition = mousePos;
                }
                else
                {
                    _isDragging = false;
                }
            }
            if (mouseReleased)
            {
                _isDragging = false;
            }

            // 3. Clamp Pan bounds
            Vector2 viewportSize = (mapViewport != null) ? mapViewport.rect.size : ((mapContainer != null) ? mapContainer.rect.size : new Vector2(1600f, 900f));
            Vector2 contentBaseSize = (mapContainer != null) ? mapContainer.rect.size : new Vector2(1600f, 900f);

            float maxPanX = Mathf.Max(0f, (contentBaseSize.x * _targetZoom - viewportSize.x) * 0.5f);
            float maxPanY = Mathf.Max(0f, (contentBaseSize.y * _targetZoom - viewportSize.y) * 0.5f);

            _targetPanOffset.x = Mathf.Clamp(_targetPanOffset.x, -maxPanX, maxPanX);
            _targetPanOffset.y = Mathf.Clamp(_targetPanOffset.y, -maxPanY, maxPanY);

            // 4. Smooth interpolation
            float t = (zoomSmoothing > 0.001f) ? (Time.unscaledDeltaTime * zoomSmoothing) : 1f;
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, t);
            _panOffset = Vector2.Lerp(_panOffset, _targetPanOffset, t);

            // Apply transformations
            contentTarget.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            contentTarget.anchoredPosition = _panOffset;

            // 5. Update UI text
            if (zoomLevelText != null)
            {
                zoomLevelText.text = $"Zoom: {Mathf.RoundToInt(_currentZoom * 100f)}%";
            }
        }

        public void SetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            _currentZoom = _targetZoom;
            var target = mapContent != null ? mapContent : mapContainer;
            if (target != null) target.localScale = Vector3.one * _currentZoom;
        }

        public void SetPan(Vector2 pan)
        {
            _targetPanOffset = pan;
            _panOffset = pan;
            var target = mapContent != null ? mapContent : mapContainer;
            if (target != null) target.anchoredPosition = _panOffset;
        }

        public void OpenModal()
        {
            if (_isOpen) return;

            _isOpen = true;
            ResolvePlayerReference();

            // Reset zoom & pan on open for a clean, centered full overview
            _targetZoom = 1.0f;
            _currentZoom = 1.0f;
            _targetPanOffset = Vector2.zero;
            _panOffset = Vector2.zero;
            _isDragging = false;
            var contentTarget = mapContent != null ? mapContent : mapContainer;
            if (contentTarget != null)
            {
                contentTarget.localScale = Vector3.one;
                contentTarget.anchoredPosition = Vector2.zero;
            }

            if (ModalStackManager.HasInstance)
            {
                ModalStackManager.Instance.Push(this);
            }

            if (_cachedPlayerContext != null)
            {
                _cachedPlayerContext.AddInputLock("WorldMapUI");
            }

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Immediate initial positioning
            UpdatePlayerPinTracking();
        }

        public void CloseModal()
        {
            if (!_isOpen) return;

            _isOpen = false;

            if (ModalStackManager.HasInstance)
            {
                ModalStackManager.Instance.PopSpecific(this);
            }

            if (_cachedPlayerContext != null)
            {
                _cachedPlayerContext.RemoveInputLock("WorldMapUI");
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            HideTooltip();
        }

        public void ToggleModal()
        {
            if (_isOpen) CloseModal();
            else OpenModal();
        }

        private void SetVisibilityInstant(bool visible)
        {
            _isOpen = visible;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        private void UpdateFadeAnimation()
        {
            if (canvasGroup == null) return;
            float targetAlpha = _isOpen ? 1f : 0f;
            float step = (fadeDuration > 0.001f) ? (Time.unscaledDeltaTime / fadeDuration) : 1f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, step);
        }

        public void SetPlayerTarget(Transform playerTransform, IPlayerContext playerContext = null)
        {
            _cachedPlayerTransform = playerTransform;
            _cachedPlayerContext = playerContext ?? (playerTransform != null ? playerTransform.GetComponent<PlayerControl>() : null);
        }

        private void ResolvePlayerReference()
        {
            if (_cachedPlayerTransform != null && _cachedPlayerContext != null)
            {
                if (_cachedPlayerTransform is UnityEngine.Object uObj && uObj == null)
                {
                    _cachedPlayerTransform = null;
                    _cachedPlayerContext = null;
                }
                else return;
            }

            var ctx = ServiceLocator.Resolve<IPlayerContext>();
            if (ctx != null && ctx.Transform != null)
            {
                _cachedPlayerContext = ctx;
                _cachedPlayerTransform = ctx.Transform;
                return;
            }

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                _cachedPlayerTransform = playerGo.transform;
                _cachedPlayerContext = playerGo.GetComponent<PlayerControl>() ?? (playerGo.GetComponent<IPlayerContext>() as IPlayerContext);
            }
        }

        public void ForceUpdateTracking()
        {
            UpdatePlayerPinTracking();
        }

        private void UpdatePlayerPinTracking()
        {
            if (playerMarker == null) return;

            if (_cachedPlayerTransform == null)
            {
                ResolvePlayerReference();
            }

            if (_cachedPlayerTransform != null)
            {
                playerMarker.gameObject.SetActive(true);

                Vector2 containerSize = (mapContainer != null) ? mapContainer.rect.size : new Vector2(1600f, 900f);
                Vector3 playerPos = _cachedPlayerTransform.position;

                Vector2 anchoredPos = MapProjectionMath.WorldToMapAnchoredPosition(playerPos, MapBounds, containerSize);
                playerMarker.anchoredPosition = anchoredPos;

                float yaw = _cachedPlayerTransform.eulerAngles.y;
                float uiRotZ = MapProjectionMath.WorldYawToUIRotationZ(yaw);
                playerMarker.localEulerAngles = new Vector3(0f, 0f, uiRotZ);

                if (coordinatesText != null)
                {
                    coordinatesText.text = $"X: {playerPos.x:F1}  Z: {playerPos.z:F1}";
                }
            }
            else
            {
                playerMarker.gameObject.SetActive(false);
                if (coordinatesText != null)
                {
                    coordinatesText.text = "GPS: Searching...";
                }
            }
        }

        private void AnimatePlayerPinPulse()
        {
            if (!pulsePlayerMarker || playerMarker == null) return;

            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
            playerMarker.localScale = new Vector3(pulse, pulse, 1f);
        }

        private void SetupDefaultPOIsIfEmpty()
        {
            if (defaultPOIs.Count > 0) return;

            defaultPOIs.Add(new POIEntry { name = "Homestead Farmhouse", worldPosition = new Vector3(21.49f, 1.75f, 42.91f), tint = new Color(0.9f, 0.75f, 0.4f) });
            defaultPOIs.Add(new POIEntry { name = "Farmland Plots", worldPosition = new Vector3(31.7f, 0f, 39.5f), tint = new Color(0.4f, 0.85f, 0.4f) });
            defaultPOIs.Add(new POIEntry { name = "Fresh Water Well", worldPosition = new Vector3(38.38f, 0f, 39.5f), tint = new Color(0.3f, 0.65f, 0.95f) });
            defaultPOIs.Add(new POIEntry { name = "Natural Pond & Pier", worldPosition = new Vector3(6.5f, 0.01f, 42f), tint = new Color(0.2f, 0.7f, 0.9f) });
            defaultPOIs.Add(new POIEntry { name = "Old Farm Pickup Truck", worldPosition = new Vector3(20.4f, 0.04f, 53.5f), tint = new Color(0.8f, 0.5f, 0.3f) });
            defaultPOIs.Add(new POIEntry { name = "Crafting Shelter & Storage", worldPosition = new Vector3(3.74f, 0f, -7.52f), tint = new Color(0.75f, 0.65f, 0.5f) });
            defaultPOIs.Add(new POIEntry { name = "Pine Valley Rest Stop", worldPosition = new Vector3(-103.6f, 0f, 50.4f), tint = new Color(0.6f, 0.8f, 0.6f) });
            defaultPOIs.Add(new POIEntry { name = "Trading Post Outpost", worldPosition = new Vector3(-175f, 0f, 70f), tint = new Color(0.95f, 0.85f, 0.3f) });
            defaultPOIs.Add(new POIEntry { name = "North Threat Perimeter", worldPosition = new Vector3(22.2f, 0f, 52.2f), tint = new Color(0.9f, 0.3f, 0.3f) });
        }

        private void SpawnPOIMarkers()
        {
            if (poiContainer == null) return;

            foreach (var pin in _spawnedPoiPins)
            {
                if (pin != null) Destroy(pin);
            }
            _spawnedPoiPins.Clear();

            Vector2 containerSize = (mapContainer != null) ? mapContainer.rect.size : new Vector2(1600f, 900f);

            foreach (var poi in defaultPOIs)
            {
                var pinGo = new GameObject($"POI_{poi.name}", typeof(RectTransform), typeof(Image));
                pinGo.transform.SetParent(poiContainer, false);

                var rt = pinGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(28f, 28f);
                rt.anchoredPosition = MapProjectionMath.WorldToMapAnchoredPosition(poi.worldPosition, MapBounds, containerSize);

                var img = pinGo.GetComponent<Image>();
                if (poi.icon != null) img.sprite = poi.icon;
                img.color = poi.tint;

                var eventTrigger = pinGo.AddComponent<EventTrigger>();

                var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                string poiName = poi.name;
                entryEnter.callback.AddListener((_) => ShowTooltip(poiName, rt.anchoredPosition));
                eventTrigger.triggers.Add(entryEnter);

                var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((_) => HideTooltip());
                eventTrigger.triggers.Add(entryExit);

                _spawnedPoiPins.Add(pinGo);
            }
        }

        public void ShowTooltip(string text, Vector2 pinAnchoredPos)
        {
            if (tooltipPanel == null || tooltipText == null) return;
            tooltipText.text = text;
            var rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = pinAnchoredPos + new Vector2(0f, 32f);
            }
            tooltipPanel.SetActive(true);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
    }
}
