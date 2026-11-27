using UnityEngine;
using UnityEngine.UI;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Shared styling, sprite resolution, and exact normalized compartment anchors
    /// for the 1024x572 two-column rustic wooden UI board (WoodenBoard_TwoColumn).
    /// Eliminates duplicated geometry math and color constants across feature UI panels.
    /// </summary>
    public static class WoodenBoardStyle
    {
        public const string BoardResourcePath = "UI/WoodenBoard_TwoColumn";

        // Exact normalized UI coordinates (bottom-left = 0,0; top-right = 1,1)
        // measured against the 1024x572 wooden board artwork's gold trim and top plaque.
        public static readonly Vector2 PlaqueMin = new Vector2(0.345f, 0.885f);
        public static readonly Vector2 PlaqueMax = new Vector2(0.710f, 0.972f);

        public static readonly Vector2 LeftPaneMin = new Vector2(0.118f, 0.068f);
        public static readonly Vector2 LeftPaneMax = new Vector2(0.348f, 0.865f);

        public static readonly Vector2 RightPaneMin = new Vector2(0.376f, 0.068f);
        public static readonly Vector2 RightPaneMax = new Vector2(0.928f, 0.865f);

        public static readonly Vector2 CloseButtonAnchor = new Vector2(0.946f, 0.935f);

        // Warm Homestead Wood & Brass Color Palette
        public static readonly Color DimBackdrop       = new Color(0.03f, 0.02f, 0.01f, 0.78f);
        public static readonly Color DarkOakOverlay    = new Color(0.14f, 0.08f, 0.04f, 0.62f);
        public static readonly Color DarkOakSubtle     = new Color(0.18f, 0.11f, 0.05f, 0.72f);
        public static readonly Color DarkOakHeader     = new Color(0.12f, 0.07f, 0.03f, 0.78f);
        public static readonly Color DarkOakInput      = new Color(0.10f, 0.06f, 0.03f, 0.90f);
        public static readonly Color GoldTrim          = new Color(0.78f, 0.60f, 0.25f, 0.92f);

        public static readonly Color TabActive         = new Color(0.58f, 0.36f, 0.12f, 0.95f);
        public static readonly Color TabInactive       = new Color(0.20f, 0.12f, 0.06f, 0.82f);

        public static readonly Color BtnPrimary        = new Color(0.22f, 0.54f, 0.28f, 1.00f);
        public static readonly Color BtnSecondary      = new Color(0.42f, 0.27f, 0.12f, 0.95f);
        public static readonly Color BtnDanger         = new Color(0.68f, 0.18f, 0.15f, 0.95f);
        public static readonly Color BtnDisabled       = new Color(0.22f, 0.17f, 0.13f, 0.65f);

        public static readonly Color TextPlaqueTitle   = new Color(1.00f, 0.93f, 0.75f, 1.00f);
        public static readonly Color TextGoldAccent    = new Color(1.00f, 0.84f, 0.36f, 1.00f);
        public static readonly Color TextCream         = new Color(0.97f, 0.93f, 0.84f, 1.00f);
        public static readonly Color TextMutedWarm     = new Color(0.80f, 0.72f, 0.58f, 0.95f);

        private static Sprite _cachedBoardSprite;

        /// <summary>
        /// Loads and caches the WoodenBoard_TwoColumn sprite from Resources.
        /// Includes a Texture2D fallback if Unity has not yet reimported the asset as Sprite.
        /// </summary>
        public static Sprite GetTwoColumnBoardSprite()
        {
            if (_cachedBoardSprite != null) return _cachedBoardSprite;

            _cachedBoardSprite = Resources.Load<Sprite>(BoardResourcePath);
            if (_cachedBoardSprite != null) return _cachedBoardSprite;

            Texture2D tex = Resources.Load<Texture2D>(BoardResourcePath);
            if (tex != null)
            {
                _cachedBoardSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                _cachedBoardSprite.name = "WoodenBoard_TwoColumn_Runtime";
            }

            return _cachedBoardSprite;
        }

        /// <summary>
        /// Applies the wooden two-column board sprite onto an Image component.
        /// </summary>
        public static void ApplyBoardBackground(Image image)
        {
            if (image == null) return;

            Sprite boardSprite = GetTwoColumnBoardSprite();
            if (boardSprite != null)
            {
                image.sprite = boardSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.28f, 0.18f, 0.09f, 0.98f);
            }
        }

        /// <summary>
        /// Positions a RectTransform within normalized anchor bounds of the wooden board.
        /// </summary>
        public static void SetAnchorRegion(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float padLeft = 0f, float padBottom = 0f, float padRight = 0f, float padTop = 0f)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(padLeft, padBottom);
            rt.offsetMax = new Vector2(-padRight, -padTop);
        }

        /// <summary>
        /// Ensures a centered graphic host named "PlaqueTitleText" anchored cleanly inside the wooden header plaque.
        /// Keeps Core.Runtime decoupled from TextMeshPro assembly dependencies.
        /// </summary>
        public static T EnsurePlaqueTitle<T>(Transform boardRoot) where T : MaskableGraphic
        {
            if (boardRoot == null) return null;

            Transform existing = boardRoot.Find("PlaqueTitleText");
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject("PlaqueTitleText", typeof(RectTransform), typeof(T));

            if (existing == null)
            {
                go.transform.SetParent(boardRoot, false);
            }

            SetAnchorRegion(go.GetComponent<RectTransform>(), PlaqueMin, PlaqueMax, 8f, 2f, 8f, 2f);

            T graphic = go.GetComponent<T>() ?? go.AddComponent<T>();
            graphic.color = TextPlaqueTitle;
            graphic.raycastTarget = false;
            return graphic;
        }
    }
}

