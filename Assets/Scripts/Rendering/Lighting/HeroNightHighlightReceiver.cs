using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesRendering.Lighting
{
    /// <summary>
    /// Receiver component attached to the Player/Hero character to handle dynamic
    /// night silhouette highlight and Half-Lambert diffuse floor modulation.
    ///
    /// Architectural Constraints:
    /// - Zero GC allocations per tick (strictly cached MaterialPropertyBlock and Renderer[] array).
    /// - Decoupled static registry pattern (allows LightingAndAtmosphereDirector to dispatch updates without assembly circularities).
    /// - Optionally drives a ground aura DecalProjector fadeFactor without touching character mesh.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10)]
    public class HeroNightHighlightReceiver : MonoBehaviour
    {
        #region Static Registry & Events

        private static readonly List<HeroNightHighlightReceiver> s_ActiveReceivers = new List<HeroNightHighlightReceiver>(4);

        public static IReadOnlyList<HeroNightHighlightReceiver> ActiveReceivers => s_ActiveReceivers;

        public static event Action<HeroNightHighlightReceiver> OnReceiverRegistered;
        public static event Action<HeroNightHighlightReceiver> OnReceiverUnregistered;

        #endregion

        #region Shader Property IDs (Cached)

        private static readonly int PropNightHighlightWeight = Shader.PropertyToID("_NightHighlightWeight");
        private static readonly int PropNightRimIntensity = Shader.PropertyToID("_NightRimIntensity");
        private static readonly int PropNightAmbientFloor = Shader.PropertyToID("_NightAmbientFloor");
        private static readonly int PropNightRimColor = Shader.PropertyToID("_NightRimColor");
        private static readonly int PropNightRimPower = Shader.PropertyToID("_NightRimPower");

        #endregion

        #region Serialized Fields

        [Header("Receiver Parameters")]
        [SerializeField] private Color customRimColor = new Color(0.55f, 0.78f, 1.25f, 1.0f);
        [Range(0.5f, 10.0f)]
        [SerializeField] private float customRimPower = 3.2f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float customRimIntensity = 2.0f;
        [Range(0.0f, 0.5f)]
        [SerializeField] private float customAmbientFloor = 0.22f;

        [Header("Optional Ground Aura Decal")]
        [Tooltip("Optional URP Decal Projector on ground below feet. Excluded from character mesh layer.")]
        [SerializeField] private UnityEngine.Rendering.Universal.DecalProjector groundAuraDecal;

        #endregion

        #region Private Fields & Cached State

        private Renderer[] cachedRenderers;
        private MaterialPropertyBlock propertyBlock;
        private float currentWeight = -1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();

            if (!s_ActiveReceivers.Contains(this))
            {
                s_ActiveReceivers.Add(this);
            }

            OnReceiverRegistered?.Invoke(this);
        }

        private void OnDisable()
        {
            s_ActiveReceivers.Remove(this);
            OnReceiverUnregistered?.Invoke(this);
        }

        #endregion

        #region Initialization & Caching

        private void CacheComponents()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (groundAuraDecal == null)
            {
                groundAuraDecal = GetComponentInChildren<UnityEngine.Rendering.Universal.DecalProjector>(true);
            }
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                CacheComponents();
                if (currentWeight >= 0f)
                {
                    ApplyHighlightParameters(currentWeight, force: true);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Injects night highlight weight and shading floor into character renderers with 0 Bytes GC.
        /// </summary>
        /// <param name="weight">0.0 (Day / Off) to 1.0 (Night / Full Glow)</param>
        /// <param name="force">If true, forces re-application even if weight hasn't changed.</param>
        public void ApplyHighlightParameters(float weight, bool force = false)
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                CacheComponents();
            }

            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                return;
            }

            if (!force && Mathf.Approximately(currentWeight, weight))
            {
                return;
            }

            currentWeight = weight;

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.SetFloat(PropNightHighlightWeight, weight);
            propertyBlock.SetColor(PropNightRimColor, customRimColor);
            propertyBlock.SetFloat(PropNightRimPower, customRimPower);
            propertyBlock.SetFloat(PropNightRimIntensity, customRimIntensity);
            propertyBlock.SetFloat(PropNightAmbientFloor, customAmbientFloor);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].SetPropertyBlock(propertyBlock);
                }
            }

            if (groundAuraDecal != null)
            {
                groundAuraDecal.fadeFactor = weight;
            }
        }

        #endregion
    }
}
