using System;
using UnityEngine;

namespace FeaturesRendering.Vision
{
    /// <summary>
    /// Attached to entities (Enemies, Interactables, Drop Items) to toggle their visual renderers 
    /// and world-space UI (such as health bars) based on player vision.
    /// Uses cached state transitions to guarantee zero redundant component activations.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public class VisionTarget : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Target Components")]
        [Tooltip("Renderers to enable/disable when entity enters/leaves vision.")]
        [SerializeField] private Renderer[] renderers;

        [Tooltip("World Space Canvases (e.g. Health Bar, Name Tag) to enable/disable.")]
        [SerializeField] private Canvas[] canvases;

        [Header("Detection Settings")]
        [Tooltip("Radius of this entity added to vision distance test.")]
        [Min(0f)]
        [SerializeField] private float targetRadius = 0.5f;

        [Tooltip("Optional local offset from Transform pivot for distance testing.")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        [Tooltip("Auto find Renderers and Canvases in children if array is empty on Awake/Enable.")]
        [SerializeField] private bool autoFindComponents = true;

        [Tooltip("Initial visibility state on start. Defaults to true so items/enemies are visible in editor.")]
        [SerializeField] private bool startsVisible = true;
        #endregion

        #region State
        private bool m_IsCurrentlyVisible = true;
        private Transform m_Transform;

        public float TargetRadius => targetRadius;
        public Vector3 WorldPosition => m_Transform != null ? m_Transform.position + localOffset : transform.position + localOffset;
        public bool IsVisible => m_IsCurrentlyVisible;

        /// <summary>
        /// Event fired when visibility state changes (true = revealed, false = hidden).
        /// </summary>
        public event Action<bool> OnVisibilityChanged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureInitialized();
            m_IsCurrentlyVisible = startsVisible;
            ApplyVisibilityState(m_IsCurrentlyVisible);
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (VisionManager.Instance != null)
            {
                VisionManager.Instance.RegisterTarget(this);
            }
        }

        private void OnDisable()
        {
            if (VisionManager.Instance != null)
            {
                VisionManager.Instance.UnregisterTarget(this);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Updates visibility state. Only alters component states if visibility has actually changed,
        /// avoiding unnecessary driver state dirtying.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            EnsureInitialized();
            if (m_IsCurrentlyVisible == isVisible) return;

            m_IsCurrentlyVisible = isVisible;
            ApplyVisibilityState(isVisible);
            OnVisibilityChanged?.Invoke(isVisible);
        }

        /// <summary>
        /// Manually refresh component list (useful when equipment or cosmetics are swapped dynamically).
        /// </summary>
        public void RefreshComponents()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            canvases = GetComponentsInChildren<Canvas>(true);
            ApplyVisibilityState(m_IsCurrentlyVisible);
        }
        #endregion

        #region Private Helpers
        private void EnsureInitialized()
        {
            if (m_Transform == null)
            {
                m_Transform = transform;
            }

            if (autoFindComponents)
            {
                if (renderers == null || renderers.Length == 0)
                {
                    renderers = GetComponentsInChildren<Renderer>(true);
                }

                if (canvases == null || canvases.Length == 0)
                {
                    canvases = GetComponentsInChildren<Canvas>(true);
                }
            }
        }

        private void ApplyVisibilityState(bool visible)
        {
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r != null)
                    {
                        r.enabled = visible;
                    }
                }
            }

            if (canvases != null)
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas c = canvases[i];
                    if (c != null)
                    {
                        c.enabled = visible;
                    }
                }
            }
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        private void Reset()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            canvases = GetComponentsInChildren<Canvas>(true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_IsCurrentlyVisible ? Color.green : Color.red;
            Gizmos.DrawWireSphere(WorldPosition, targetRadius);
        }
#endif
        #endregion
    }
}
