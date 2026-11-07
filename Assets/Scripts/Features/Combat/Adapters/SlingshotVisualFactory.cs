using UnityEngine;

namespace FeaturesCombat.Adapters
{
    /// <summary>
    /// Procedural 3D model and 2D icon builder for the Homestead Slingshot (Ketapel).
    /// Ensures the ranged weapon renders as an authentic Y-frame wooden slingshot with elastic bands
    /// and leather pouch in the player's hand and UI slots, with zero melee colliders or sword trails.
    /// </summary>
    public static class SlingshotVisualFactory
    {
        private static Sprite _cachedSlingshotSprite;
        private static Material _woodMaterial;
        private static Material _wrapMaterial;
        private static Material _bandMaterial;
        private static Material _pouchMaterial;
        private static Material _ammoMaterial;

        /// <summary>
        /// Generates or returns a cached 64x64 UI Sprite depicting a Y-frame Slingshot (Ketapel).
        /// </summary>
        public static Sprite GetSlingshotIconSprite()
        {
            if (_cachedSlingshotSprite != null) return _cachedSlingshotSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Icon_HomesteadSlingshot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color32 woodDark  = new Color32(92, 54, 28, 255);
            Color32 woodLight = new Color32(156, 98, 52, 255);
            Color32 wrapRed   = new Color32(210, 55, 45, 255);
            Color32 bandGold  = new Color32(235, 195, 85, 255);
            Color32 pouchTan  = new Color32(185, 135, 85, 255);
            Color32 ammoGlow  = new Color32(255, 225, 95, 255);

            // 1. Elastic bands from fork tips to leather pouch (top-right / right side)
            DrawLine(pixels, size, 18, 48, 50, 44, bandGold, 2);
            DrawLine(pixels, size, 40, 26, 50, 44, bandGold, 2);

            // 2. Y-Frame Handle (bottom-left to center)
            DrawLine(pixels, size, 12, 12, 27, 29, woodDark, 5);
            DrawLine(pixels, size, 12, 12, 27, 29, woodLight, 3);

            // 3. Left/Upper Fork Prong
            DrawLine(pixels, size, 27, 29, 17, 49, woodDark, 4);
            DrawLine(pixels, size, 27, 29, 17, 49, woodLight, 2);

            // 4. Right/Lower Fork Prong
            DrawLine(pixels, size, 27, 29, 41, 25, woodDark, 4);
            DrawLine(pixels, size, 27, 29, 41, 25, woodLight, 2);

            // 5. Red Lashing Wraps at fork tips & grip
            DrawCircle(pixels, size, 17, 48, 3, wrapRed);
            DrawCircle(pixels, size, 40, 25, 3, wrapRed);
            DrawLine(pixels, size, 16, 16, 21, 22, wrapRed, 4);

            // 6. Leather Pouch & Glowing Stone/Kernel
            DrawCircle(pixels, size, 51, 45, 5, pouchTan);
            DrawCircle(pixels, size, 53, 47, 4, ammoGlow);

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            _cachedSlingshotSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            _cachedSlingshotSprite.name = "Spr_HomesteadSlingshot";
            return _cachedSlingshotSprite;
        }

        private static void DrawLine(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color, int radius)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                int cx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int cy = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                DrawCircle(pixels, size, cx, cy, radius, color);
            }
        }

        private static void DrawCircle(Color32[] pixels, int size, int cx, int cy, int r, Color32 color)
        {
            int rSq = r * r;
            for (int dy = -r; dy <= r; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= size) continue;
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= size) continue;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Builds a 3D Y-Frame Slingshot (Ketapel) model parented to the player's hand socket.
        /// Contains zero colliders and zero melee hitboxes.
        /// </summary>
        public static GameObject CreateSlingshot3DModel(Transform handSocket)
        {
            EnsureMaterials();

            var root = new GameObject("Equipped_HomesteadSlingshot");
            if (handSocket != null)
            {
                root.transform.SetParent(handSocket, false);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;

                Vector3 parentLossy = handSocket.lossyScale;
                root.transform.localScale = new Vector3(
                    Mathf.Abs(parentLossy.x) > 0.0001f ? 1f / parentLossy.x : 1f,
                    Mathf.Abs(parentLossy.y) > 0.0001f ? 1f / parentLossy.y : 1f,
                    Mathf.Abs(parentLossy.z) > 0.0001f ? 1f / parentLossy.z : 1f
                );
            }

            // Pivot offset so the Y-frame sits naturally in the right palm facing forward
            var framePivot = new GameObject("SlingshotFrame").transform;
            framePivot.SetParent(root.transform, false);
            framePivot.localPosition = new Vector3(0.0f, 0.04f, 0.06f);
            framePivot.localRotation = Quaternion.Euler(12f, 0f, 0f);

            // 1. Main Wooden Grip Handle
            CreatePart("GripHandle", PrimitiveType.Cylinder, framePivot,
                new Vector3(0f, -0.06f, 0f),
                Quaternion.identity,
                new Vector3(0.042f, 0.11f, 0.042f),
                _woodMaterial);

            // Grip Leather Wrap
            CreatePart("GripWrap", PrimitiveType.Cylinder, framePivot,
                new Vector3(0f, -0.05f, 0f),
                Quaternion.identity,
                new Vector3(0.046f, 0.055f, 0.046f),
                _wrapMaterial);

            // 2. Left Y-Fork Arm
            CreatePart("LeftForkArm", PrimitiveType.Cylinder, framePivot,
                new Vector3(-0.065f, 0.11f, 0f),
                Quaternion.Euler(0f, 0f, 28f),
                new Vector3(0.034f, 0.095f, 0.034f),
                _woodMaterial);

            // 3. Right Y-Fork Arm
            CreatePart("RightForkArm", PrimitiveType.Cylinder, framePivot,
                new Vector3(0.065f, 0.11f, 0f),
                Quaternion.Euler(0f, 0f, -28f),
                new Vector3(0.034f, 0.095f, 0.034f),
                _woodMaterial);

            // 4. Fork Tip Lashing Wraps
            CreatePart("LeftTipWrap", PrimitiveType.Cylinder, framePivot,
                new Vector3(-0.105f, 0.18f, 0f),
                Quaternion.Euler(0f, 0f, 28f),
                new Vector3(0.040f, 0.022f, 0.040f),
                _wrapMaterial);

            CreatePart("RightTipWrap", PrimitiveType.Cylinder, framePivot,
                new Vector3(0.105f, 0.18f, 0f),
                Quaternion.Euler(0f, 0f, -28f),
                new Vector3(0.040f, 0.022f, 0.040f),
                _wrapMaterial);

            // 5. Elastic Latex Bands (pulled slightly back toward a leather pouch)
            CreatePart("LeftElasticBand", PrimitiveType.Cylinder, framePivot,
                new Vector3(-0.055f, 0.17f, -0.075f),
                Quaternion.Euler(72f, 34f, 0f),
                new Vector3(0.014f, 0.095f, 0.014f),
                _bandMaterial);

            CreatePart("RightElasticBand", PrimitiveType.Cylinder, framePivot,
                new Vector3(0.055f, 0.17f, -0.075f),
                Quaternion.Euler(72f, -34f, 0f),
                new Vector3(0.014f, 0.095f, 0.014f),
                _bandMaterial);

            // 6. Leather Pouch & Loaded Golden Pebble/Kernel
            CreatePart("LeatherPouch", PrimitiveType.Cube, framePivot,
                new Vector3(0f, 0.16f, -0.15f),
                Quaternion.identity,
                new Vector3(0.065f, 0.045f, 0.025f),
                _pouchMaterial);

            CreatePart("LoadedAmmoStone", PrimitiveType.Sphere, framePivot,
                new Vector3(0f, 0.16f, -0.13f),
                Quaternion.identity,
                new Vector3(0.052f, 0.052f, 0.052f),
                _ammoMaterial);

            return root;
        }

        private static void CreatePart(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            Material mat)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            // Remove collider immediately so the visual model never interferes with physics or melee triggers
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void EnsureMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            if (_woodMaterial == null && shader != null)
            {
                _woodMaterial = CreateColorMaterial(shader, "Mat_Slingshot_Wood", new Color(0.45f, 0.26f, 0.12f));
            }
            if (_wrapMaterial == null && shader != null)
            {
                _wrapMaterial = CreateColorMaterial(shader, "Mat_Slingshot_Wrap", new Color(0.78f, 0.18f, 0.15f));
            }
            if (_bandMaterial == null && shader != null)
            {
                _bandMaterial = CreateColorMaterial(shader, "Mat_Slingshot_Band", new Color(0.92f, 0.72f, 0.22f));
            }
            if (_pouchMaterial == null && shader != null)
            {
                _pouchMaterial = CreateColorMaterial(shader, "Mat_Slingshot_Pouch", new Color(0.62f, 0.42f, 0.24f));
            }
            if (_ammoMaterial == null && shader != null)
            {
                _ammoMaterial = CreateColorMaterial(shader, "Mat_Slingshot_Ammo", new Color(1.0f, 0.84f, 0.22f));
            }
        }

        private static Material CreateColorMaterial(Shader shader, string name, Color color)
        {
            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}

