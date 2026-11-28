using UnityEngine;
using FarmBeware.Core.Runtime;

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
        [SerializeField] private float trailTime = 0.15f;

        [Tooltip("Base ribbon width at the leading blade tip.")]
        [SerializeField] private float baseWidth = 0.30f;

        [Tooltip("Minimum distance between vertices along the trail ribbon arc.")]
        [SerializeField] private float minVertexDistance = 0.025f;

        public TrailRenderer ActiveTrailRenderer => trailRenderer;
        public float BaseWidth => baseWidth;

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
                    new Keyframe(0.0f, 1.0f, -0.4f, -0.4f),
                    new Keyframe(0.35f, 0.68f, -1.0f, -1.0f),
                    new Keyframe(0.75f, 0.22f, -1.1f, -1.1f),
                    new Keyframe(1.0f, 0.0f, -0.8f, 0.0f)
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
                    trailObj.transform.localPosition = ResolveWeaponTrailLocalPosition(weaponRoot);
                    trailRenderer = trailObj.AddComponent<TrailRenderer>();
                }
            }

            ConfigureTrailRenderer();
        }

        /// <summary>
        /// Configures this controller as a compact fist trail for bare-hands melee attacks.
        /// </summary>
        public void InitializeForBareHands(float fistWidth = 0.14f, float fistTrailTime = 0.12f)
        {
            EnsureStaticResources();
            baseWidth = fistWidth;
            trailTime = fistTrailTime;
            minVertexDistance = 0.025f;

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
                    trailObj.transform.localPosition = Vector3.zero;
                    trailRenderer = trailObj.AddComponent<TrailRenderer>();
                }
            }

            ConfigureTrailRenderer();
        }

        private Vector3 ResolveWeaponTrailLocalPosition(GameObject weaponRoot)
        {
            if (tipAnchor != null)
            {
                return transform.InverseTransformPoint(tipAnchor.position) * 0.65f;
            }

            Transform bladeChild = transform.Find("BladeMain") ?? transform.Find("Blade");
            if (bladeChild != null)
            {
                return transform.InverseTransformPoint(bladeChild.position);
            }

            if (weaponRoot != null)
            {
                var renderers = weaponRoot.GetComponentsInChildren<MeshRenderer>(true);
                if (renderers.Length > 0)
                {
                    float minY = float.MaxValue;
                    float maxY = float.MinValue;
                    bool found = false;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var mr = renderers[i];
                        if (mr == null) continue;
                        Bounds b = mr.bounds;
                        Vector3 localMin = transform.InverseTransformPoint(b.min);
                        Vector3 localMax = transform.InverseTransformPoint(b.max);
                        float low = Mathf.Min(localMin.y, localMax.y);
                        float high = Mathf.Max(localMin.y, localMax.y);
                        if (low < minY) minY = low;
                        if (high > maxY) maxY = high;
                        found = true;
                    }

                    if (found && maxY > minY)
                    {
                        float targetY = Mathf.Clamp(Mathf.Lerp(minY, maxY, 0.68f), 0f, 0.75f);
                        return new Vector3(0f, targetY, 0f);
                    }
                }
            }

            return Vector3.zero;
        }

        private void ConfigureTrailRenderer()
        {
            if (trailRenderer == null) return;

            trailRenderer.time = trailTime;
            trailRenderer.minVertexDistance = minVertexDistance;
            trailRenderer.widthCurve = _defaultWidthCurve;
            trailRenderer.widthMultiplier = baseWidth;
            trailRenderer.emitting = false;
            trailRenderer.autodestruct = false;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.numCornerVertices = 2;
            trailRenderer.numCapVertices = 2;

            if (trailMaterial != null)
            {
                trailRenderer.sharedMaterial = trailMaterial;
            }
            else if (_cachedDefaultMaterial != null)
            {
                trailRenderer.sharedMaterial = _cachedDefaultMaterial;
            }
        }

        [Header("Time Gate Configuration")]
        [Tooltip("When true, allows the trail to emit during daytime (e.g., for debug or tests). Defaults to false (night only).")]
        [SerializeField] private bool allowDaytimeTrail = false;

        public bool AllowDaytimeTrail
        {
            get => allowDaytimeTrail;
            set => allowDaytimeTrail = value;
        }

        public bool IsNightTime()
        {
            if (allowDaytimeTrail) return true;

            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                return FeaturesTime.DayNightTimeManager.Instance.IsNight;
            }
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night;
            }

            return false;
        }

        /// <summary>
        /// Begins emitting the stylized slash trail ribbon with attack-specific gradient and width.
        /// </summary>
        /// <param name="comboIndex">Current combo hit index (0 = Hit 1, 1 = Hit 2, 2 = Finisher).</param>
        /// <param name="isFinisher">True if performing 360 finisher swing.</param>
        /// <param name="isHeavy">True if performing heavy charge release strike.</param>
        public void BeginTrail(int comboIndex, bool isFinisher = false, bool isHeavy = false)
        {
            // Attack trail is enabled only at night per design specification
            if (!IsNightTime())
            {
                return;
            }

            if (trailRenderer == null)
            {
                InitializeForWeapon(gameObject);
            }

            if (trailRenderer == null) return;

            EnsureStaticResources();

            bool isFiery = isFinisher || isHeavy || comboIndex >= 2;
            if (isFiery)
            {
                trailRenderer.colorGradient = finisherGradientOverride != null
                    ? finisherGradientOverride
                    : _defaultCrimsonFlameGradient;
                trailRenderer.widthMultiplier = isHeavy ? baseWidth * 1.35f : baseWidth * 1.20f;
                trailRenderer.time = isHeavy ? trailTime * 1.30f : trailTime * 1.15f;
            }
            else
            {
                trailRenderer.colorGradient = combo1And2GradientOverride != null
                    ? combo1And2GradientOverride
                    : _defaultGoldenAmberGradient;
                trailRenderer.widthMultiplier = baseWidth;
                trailRenderer.time = trailTime;
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
