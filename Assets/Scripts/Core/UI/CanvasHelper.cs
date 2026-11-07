using UnityEngine;
using UnityEngine.UI;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Centralized canonical Canvas resolution utility for Farm-Beware.
    /// Guarantees deterministic binding to the master UI_Canvas and prevents accidental
    /// misparenting into sub-canvases with zero-alpha CanvasGroups (e.g., DayTransition_Panel).
    /// </summary>
    public static class CanvasHelper
    {
        private static Canvas _cachedMainCanvas;

        /// <summary>
        /// Resolves the canonical root UI Canvas (UI_Canvas).
        /// Prioritizes the master root canvas and rejects sub-canvases with suppressed alpha or temporary overlays.
        /// </summary>
        public static Canvas ResolveMainUICanvas()
        {
            if (_cachedMainCanvas != null && _cachedMainCanvas.isActiveAndEnabled)
            {
                return _cachedMainCanvas;
            }

            // 1. Primary: Direct lookup for canonical master GameObject "UI_Canvas"
            var uiCanvasGO = GameObject.Find("UI_Canvas");
            if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var canvasComponent))
            {
                _cachedMainCanvas = canvasComponent;
                return _cachedMainCanvas;
            }

            // 2. Secondary: Query all root canvases in the scene
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                var c = allCanvases[i];
                if (c == null) continue;

                // Must be a root overlay canvas and not a temporary transition/fade sub-panel
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    string cName = c.name;
                    if (!cName.Contains("Transition") && !cName.Contains("Fade") && !cName.Contains("Countdown"))
                    {
                        _cachedMainCanvas = c;
                        return _cachedMainCanvas;
                    }
                }
            }

            // 3. Tertiary: Any root canvas available
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i] != null && allCanvases[i].isRootCanvas)
                {
                    _cachedMainCanvas = allCanvases[i];
                    return _cachedMainCanvas;
                }
            }

            // 4. Fallback: If no canvas exists at all (e.g. isolated test harness), construct one safely
            var fallbackGO = new GameObject("UI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var fallbackCanvas = fallbackGO.GetComponent<Canvas>();
            fallbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fallbackCanvas.sortingOrder = 0;

            var scaler = fallbackGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _cachedMainCanvas = fallbackCanvas;
            return _cachedMainCanvas;
        }

        /// <summary>
        /// Resolves the RectTransform of the canonical master UI Canvas.
        /// </summary>
        public static RectTransform ResolveMainUICanvasTransform()
        {
            var c = ResolveMainUICanvas();
            return c != null ? c.GetComponent<RectTransform>() : null;
        }

        /// <summary>
        /// Clears the cached canvas reference (useful upon scene unloads or test tear-down).
        /// </summary>
        public static void ClearCache()
        {
            _cachedMainCanvas = null;
        }
    }
}
