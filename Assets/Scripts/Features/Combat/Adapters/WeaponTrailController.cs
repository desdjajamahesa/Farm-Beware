using UnityEngine;

namespace FeaturesCombat.Adapters
{
    /// <summary>
    /// Stylized weapon slash trail controller inspired by Hades combat aesthetics.
    /// Drives an emissive ribbon TrailRenderer attached to weapons during combo swings,
    /// dynamically switching gradients (Warm Golden Amber for Combo 1 &amp; 2 vs Crimson Flame for Combo 3 Finisher / Heavy strikes)
    /// with zero runtime GC allocations.
    /// </summary>
    public class WeaponTrailController : MonoBehaviour
    {
        [Header("Trail Configuration")]
        [Tooltip("The TrailRenderer used for weapon swings. If null, automatically created/located on initialization.")]
        [SerializeField] private TrailRenderer trailRenderer;

        [Tooltip("Optional custom tip anchor transform. If null, blade position or local offset is used.")]
        [SerializeField] private Transform tipAnchor;

        [Tooltip("Emissive transparent additive material for the trail ribbon.")]
        [SerializeField] private Material trailMaterial;

        [Header("Timing & Geometry")]
        [Tooltip("Duration in seconds that trail segments remain visible (short ribbon for snappy combat).")]
        [SerializeField] private float trailTime = 0.18f;

        [Tooltip("Base ribbon width at the leading blade tip.")]
        [SerializeField] private float baseWidth = 0.55f;

        [Tooltip("Minimum distance between vertices along the trail ribbon arc.")]
        [SerializeField] private float minVertexDistance = 0.04f;

        [Header("Custom Gradient Overrides (Optional)")]
        [SerializeField] private Gradient combo1And2GradientOverride;
        [SerializeField] private Gradient finisherGradientOverride;

        private static Gradient _defaultGoldenAmberGradient;
        private static Gradient _defaultCrimsonFlameGradient;
        private static AnimationCurve _defaultWidthCurve;
        private static Material _cachedDefaultMaterial;

        private void Awake()
        {
            EnsureStaticResources();
            if (trailRenderer == null)
            {
                InitializeForWeapon(gameObject);
            }
        }

        private static void EnsureStaticResources()
        {
            if (_defaultGoldenAmberGradient == null)
            {
                _defaultGoldenAmberGradient = new Gradient();
                _defaultGoldenAmberGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(1.0f, 0.92f, 0.45f), 0.0f),  // Bright incandescent gold
                        new GradientColorKey(new Color(1.0f, 0.65f, 0.12f), 0.45f), // Warm amber body
                        new GradientColorKey(new Color(0.85f, 0.25f, 0.02f), 1.0f)  // Deep ember tail
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.95f, 0.0f),
                        new GradientAlphaKey(0.80f, 0.6f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
            }

            if (_defaultCrimsonFlameGradient == null)
            {
                _defaultCrimsonFlameGradient = new Gradient();
                _defaultCrimsonFlameGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(1.0f, 0.95f, 0.60f), 0.0f),  // White-hot core
                        new GradientColorKey(new Color(1.0f, 0.32f, 0.06f), 0.35f), // Blazing crimson-orange
                        new GradientColorKey(new Color(0.80f, 0.05f, 0.02f), 1.0f)  // Blood flame tail
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.85f, 0.6f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
            }

            if (_defaultWidthCurve == null)
            {
                _defaultWidthCurve = new AnimationCurve(
                    new Keyframe(0.0f, 1.0f, -0.3f, -0.3f),
                    new Keyframe(0.65f, 0.45f, -1.2f, -1.2f),
                    new Keyframe(1.0f, 0.0f, -1.5f, 0.0f)
                );
            }

#if UNITY_EDITOR
            if (_cachedDefaultMaterial == null)
            {
                _cachedDefaultMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Art/Materials/Combat/Mat_WeaponSlashTrail.mat");
            }
#endif
        }

        /// <summary>
        /// Automatically discovers or attaches a child TrailRenderer anchor to the weapon.
        /// </summary>
        public void InitializeForWeapon(GameObject weaponRoot)
        {
            EnsureStaticResources();

            if (trailRenderer == null)
            {
                var existing = GetComponentInChildren<TrailRenderer>();
                if (existing != null)
                {
                    trailRenderer = existing;
                }
                else
                {
                    var trailObj = new GameObject("WeaponSlashTrail");
                    trailObj.transform.SetParent(transform, false);

                    // Locate weapon blade or tip anchor
                    var bladeChild = transform.Find("Blade");
                    if (bladeChild != null)
                    {
                        trailObj.transform.position = bladeChild.position;
                    }
                    else
                    {
                        // Sane tip offset along Y axis for sword/tool models
                        trailObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                    }

                    trailRenderer = trailObj.AddComponent<TrailRenderer>();
                }
            }

            trailRenderer.time = trailTime;
            trailRenderer.minVertexDistance = minVertexDistance;
            trailRenderer.widthCurve = _defaultWidthCurve;
            trailRenderer.widthMultiplier = baseWidth;
            trailRenderer.emitting = false;
            trailRenderer.autodestruct = false;

            if (trailMaterial != null)
            {
                trailRenderer.sharedMaterial = trailMaterial;
            }
            else if (_cachedDefaultMaterial != null)
            {
                trailRenderer.sharedMaterial = _cachedDefaultMaterial;
            }
        }

        /// <summary>
        /// Begins emitting the stylized slash trail ribbon with attack-specific gradient and width.
        /// </summary>
        /// <param name="comboIndex">Current combo hit index (0 = Hit 1, 1 = Hit 2, 2 = Finisher).</param>
        /// <param name="isFinisher">True if performing 360 finisher swing.</param>
        /// <param name="isHeavy">True if performing heavy charge release strike.</param>
        public void BeginTrail(int comboIndex, bool isFinisher = false, bool isHeavy = false)
        {
            if (trailRenderer == null) return;

            EnsureStaticResources();

            bool isFlery = isFinisher || isHeavy || comboIndex >= 2;
            if (isFlery)
            {
                trailRenderer.colorGradient = finisherGradientOverride != null
                    ? finisherGradientOverride
                    : _defaultCrimsonFlameGradient;
                trailRenderer.widthMultiplier = baseWidth * 1.35f;
            }
            else
            {
                trailRenderer.colorGradient = combo1And2GradientOverride != null
                    ? combo1And2GradientOverride
                    : _defaultGoldenAmberGradient;
                trailRenderer.widthMultiplier = baseWidth;
            }

            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }

        /// <summary>
        /// Stops emitting new ribbon segments, allowing existing trail ribbon to smoothly fade out.
        /// </summary>
        public void EndTrail()
        {
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }
        }

        private void OnDisable()
        {
            EndTrail();
        }
    }
}
