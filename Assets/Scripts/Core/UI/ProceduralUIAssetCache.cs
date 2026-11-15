using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Shared cache for procedurally generated UI sprites (circle, ring, rounded bar, solid box)
    /// to eliminate duplicate per-pixel CPU texture allocations across UI components.
    /// </summary>
    public static class ProceduralUIAssetCache
    {
        private static Sprite _circleSprite;
        private static Sprite _ringSprite;
        private static Sprite _roundedBarSprite;
        private static Sprite _boxSprite;

        public static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 128;
            float radius = (size - 2) * 0.5f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circleSprite;
        }

        public static Sprite GetRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;

            const int size = 128;
            float radius = (size - 2) * 0.5f;
            float innerRadius = radius * 0.72f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float outerAlpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    float innerAlpha = Mathf.Clamp01((dist - innerRadius) + 0.5f);
                    float ringAlpha = Mathf.Min(outerAlpha, innerAlpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, ringAlpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _ringSprite;
        }

        public static Sprite GetRoundedBarSprite()
        {
            if (_roundedBarSprite != null) return _roundedBarSprite;

            const int w = 32;
            const int h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color solid = Color.white;
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isCorner = (x <= 1 && y <= 1) || (x <= 1 && y >= h - 2) || (x >= w - 2 && y <= 1) || (x >= w - 2 && y >= h - 2);
                    tex.SetPixel(x, y, isCorner ? clear : solid);
                }
            }

            tex.Apply();
            _roundedBarSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
            return _roundedBarSprite;
        }

        public static Sprite GetBoxSprite()
        {
            if (_boxSprite != null) return _boxSprite;

            const int w = 16;
            const int h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color solid = Color.white;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, solid);
                }
            }

            tex.Apply();
            _boxSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
            return _boxSprite;
        }
    }

    /// <summary>
    /// Centralized URP/Built-in fallback shader resolver caching common runtime Shader.Find lookups.
    /// </summary>
    public static class ShaderFallbackResolver
    {
        private static Shader _litShader;
        private static Shader _unlitShader;
        private static Shader _spritesDefaultShader;
        private static Shader _particleUnlitShader;

        public static Shader ResolveLitShader()
        {
            if (_litShader != null) return _litShader;
            _litShader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                      ?? Shader.Find("Standard");
            return _litShader;
        }

        public static Shader ResolveUnlitShader()
        {
            if (_unlitShader != null) return _unlitShader;
            _unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Sprites/Default")
                        ?? Shader.Find("Hidden/Internal-Colored");
            return _unlitShader;
        }

        public static Shader ResolveSpritesDefaultShader()
        {
            if (_spritesDefaultShader != null) return _spritesDefaultShader;
            _spritesDefaultShader = Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Hidden/Internal-Colored");
            return _spritesDefaultShader;
        }

        public static Shader ResolveParticleUnlitShader()
        {
            if (_particleUnlitShader != null) return _particleUnlitShader;
            _particleUnlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                ?? Shader.Find("Sprites/Default");
            return _particleUnlitShader;
        }
    }
}
