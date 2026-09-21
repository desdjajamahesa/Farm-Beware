using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesRendering.Vision
{
    /// <summary>
    /// Central manager for Entity Visibility culling against VisionSource bounds.
    /// Event-driven, decoupled, and designed for Zero GC Alloc during runtime updates.
    /// Integrates Day/Night switching and staggered batching.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class VisionManager : MonoBehaviour
    {
        #region Singleton
        private static VisionManager _instance;
        public static VisionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<VisionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                    {
                        _instance = found[0];
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        #endregion

        #region Serialized Fields
        [Header("Vision Source")]
        [Tooltip("Primary vision source (usually Player). Auto-assigned if left null.")]
        [SerializeField] private VisionSource primarySource;

        [Header("Day / Night Control")]
        [Tooltip("When true, vision mask and entity visibility culling are active (Night). When false, all entities are visible and mask is disabled (Day).")]
        [SerializeField] private bool isNight = true;

        [Tooltip("Transition duration in seconds from day to night vision weight.")]
        [Min(0f)]
        [SerializeField] private float transitionDuration = 2.0f;

        [Tooltip("Automatically synchronize night state with TimeManager if present in the scene.")]
        [SerializeField] private bool autoSyncWithTimeManager = true;

        [Header("Performance & Staggered Batching")]
        [Tooltip("Desired evaluation rate per target in Hertz (ticks per second).")]
        [Range(5f, 60f)]
        [SerializeField] private float updateFrequency = 15f;

        [Tooltip("When true, distributes target checks evenly across frames. When false, ticks all targets at fixed intervals.")]
        [SerializeField] private bool useStaggeredBatching = true;

        [Tooltip("Initial capacity for target registry to prevent runtime memory reallocations.")]
        [SerializeField] private int initialCapacity = 256;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region Runtime State
        private readonly List<VisionTarget> m_Targets = new List<VisionTarget>(256);
        private int m_BatchIndex = 0;
        private float m_IntervalTimer = 0f;
        private float currentWeight = 1.0f;
        private bool m_HasRevealedAllForDaytime = false;

        private static readonly int VisionBlendId = Shader.PropertyToID("_VisionBlend");

        public int RegisteredTargetCount => m_Targets.Count;
        public VisionSource PrimarySource => primarySource;
        public bool IsNight => isNight;
        public float VisionWeight => currentWeight;
        public bool IsVisionActive => isNight || currentWeight > 0.001f;

        public event Action<bool> OnNightStateChanged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (m_Targets.Capacity < initialCapacity)
            {
                m_Targets.Capacity = initialCapacity;
            }

            if (primarySource == null)
            {
                primarySource = FindFirstObjectByType<VisionSource>();
            }

            currentWeight = isNight ? 1.0f : 0.0f;
            Shader.SetGlobalFloat(VisionBlendId, currentWeight);
        }

        private void Start()
        {
            if (autoSyncWithTimeManager && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandleTimePhaseChanged;
                SetNightState(TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night, instant: true);
            }
            else
            {
                if (!isNight)
                {
                    RevealAllTargets();
                    m_HasRevealedAllForDaytime = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandleTimePhaseChanged;
            }
        }

        private void Update()
        {
            // Transition weight towards target (1.0 for Night, 0.0 for Day)
            float targetWeight = isNight ? 1.0f : 0.0f;
            if (transitionDuration > 0.001f)
            {
                currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime / transitionDuration);
            }
            else
            {
                currentWeight = targetWeight;
            }

            // Update shader global blend factor
            Shader.SetGlobalFloat(VisionBlendId, currentWeight);

            // Daytime logic: when night is off and transition has reached zero
            if (!isNight && currentWeight <= 0.001f)
            {
                if (!m_HasRevealedAllForDaytime)
                {
                    RevealAllTargets();
                    m_HasRevealedAllForDaytime = true;
                }
                // Zero CPU computation during daytime
                return;
            }

            m_HasRevealedAllForDaytime = false;

            if (primarySource == null)
            {
                primarySource = FindFirstObjectByType<VisionSource>();
                if (primarySource == null) return;
            }

            int targetCount = m_Targets.Count;
            if (targetCount == 0) return;

            Vector3 sourcePos = primarySource.WorldPosition;
            float sourceRadius = primarySource.Radius;

            if (useStaggeredBatching)
            {
                UpdateStaggeredBatch(sourcePos, sourceRadius, targetCount);
            }
            else
            {
                UpdateIntervalBatch(sourcePos, sourceRadius, targetCount);
            }
        }
        #endregion

        #region Day / Night State Control
        /// <summary>
        /// Sets whether night time vision is active.
        /// </summary>
        /// <param name="night">True for Night (Vision active), False for Day (Vision inactive, all targets visible).</param>
        /// <param name="instant">If true, skips smooth transition and immediately applies state.</param>
        public void SetNightState(bool night, bool instant = false)
        {
            isNight = night;

            if (instant)
            {
                currentWeight = night ? 1.0f : 0.0f;
                Shader.SetGlobalFloat(VisionBlendId, currentWeight);

                if (!night)
                {
                    RevealAllTargets();
                    m_HasRevealedAllForDaytime = true;
                }
                else
                {
                    m_HasRevealedAllForDaytime = false;
                }
            }
            else if (night)
            {
                m_HasRevealedAllForDaytime = false;
            }

            OnNightStateChanged?.Invoke(night);
        }

        /// <summary>
        /// Forces all registered targets to become visible.
        /// </summary>
        public void RevealAllTargets()
        {
            for (int i = 0; i < m_Targets.Count; i++)
            {
                VisionTarget target = m_Targets[i];
                if (target != null)
                {
                    target.SetVisible(true);
                }
            }
        }

        private void HandleTimePhaseChanged(TimeManager.DayPhase phase)
        {
            SetNightState(phase == TimeManager.DayPhase.Night, instant: false);
        }
        #endregion

        #region Batch Processing (Zero GC)
        private void UpdateStaggeredBatch(Vector3 sourcePos, float sourceRadius, int targetCount)
        {
            float targetInterval = 1f / Mathf.Max(1f, updateFrequency);
            float dt = Mathf.Max(0.0001f, Time.smoothDeltaTime);
            int framesPerCycle = Mathf.Max(1, Mathf.RoundToInt(targetInterval / dt));

            // Distribute targets across frames in this cycle
            int batchSize = Mathf.CeilToInt((float)targetCount / framesPerCycle);
            batchSize = Mathf.Clamp(batchSize, 1, targetCount);

            for (int i = 0; i < batchSize; i++)
            {
                if (m_BatchIndex >= m_Targets.Count)
                {
                    m_BatchIndex = 0;
                }

                VisionTarget target = m_Targets[m_BatchIndex];
                m_BatchIndex++;

                if (target == null) continue;

                EvaluateTargetVisibility(target, sourcePos, sourceRadius);
            }
        }

        private void UpdateIntervalBatch(Vector3 sourcePos, float sourceRadius, int targetCount)
        {
            m_IntervalTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, updateFrequency);

            if (m_IntervalTimer < interval) return;
            m_IntervalTimer = 0f;

            for (int i = 0; i < targetCount; i++)
            {
                VisionTarget target = m_Targets[i];
                if (target == null) continue;

                EvaluateTargetVisibility(target, sourcePos, sourceRadius);
            }
        }

        /// <summary>
        /// Fast horizontal (XZ plane) squared distance check.
        /// Non-allocating and mathematically consistent with the cylindrical post-process shader.
        /// </summary>
        private static void EvaluateTargetVisibility(VisionTarget target, Vector3 sourcePos, float sourceRadius)
        {
            Vector3 targetPos = target.WorldPosition;
            float dx = targetPos.x - sourcePos.x;
            float dz = targetPos.z - sourcePos.z;
            float distSq = (dx * dx) + (dz * dz);

            float effectiveRadius = sourceRadius + target.TargetRadius;
            bool isVisible = distSq <= (effectiveRadius * effectiveRadius);

            target.SetVisible(isVisible);
        }
        #endregion

        #region Registration API (Event-Driven)
        /// <summary>
        /// Registers a VisionTarget. Event-driven via target's OnEnable.
        /// </summary>
        public void RegisterTarget(VisionTarget target)
        {
            if (target != null && !m_Targets.Contains(target))
            {
                m_Targets.Add(target);

                // If currently daytime, immediately make sure target is revealed
                if (!isNight && currentWeight <= 0.001f)
                {
                    target.SetVisible(true);
                }
            }
        }

        /// <summary>
        /// Unregisters a VisionTarget via fast O(1) swap-back removal to prevent collection shifts.
        /// </summary>
        public void UnregisterTarget(VisionTarget target)
        {
            if (target == null) return;

            int index = m_Targets.IndexOf(target);
            if (index >= 0)
            {
                int lastIndex = m_Targets.Count - 1;
                if (index != lastIndex)
                {
                    m_Targets[index] = m_Targets[lastIndex];
                }
                m_Targets.RemoveAt(lastIndex);

                if (m_BatchIndex >= m_Targets.Count)
                {
                    m_BatchIndex = 0;
                }
            }
        }

        /// <summary>
        /// Registers or updates the active primary vision source.
        /// </summary>
        public void RegisterSource(VisionSource source)
        {
            if (source != null && (primarySource == null || source.IsPrimarySource))
            {
                primarySource = source;
            }
        }

        /// <summary>
        /// Unregisters a vision source when disabled or destroyed.
        /// </summary>
        public void UnregisterSource(VisionSource source)
        {
            if (primarySource == source)
            {
                primarySource = null;
            }
        }

        public void SetPrimarySource(VisionSource source)
        {
            primarySource = source;
        }
        #endregion

        #region Editor GUI
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 250, 110), GUI.skin.box);
            GUILayout.Label($"<b>Vision Manager</b>", GUI.skin.label);
            GUILayout.Label($"Time State: {(isNight ? "Night" : "Day")} (Weight: {currentWeight:F2})");
            GUILayout.Label($"Vision Active: {IsVisionActive}");
            GUILayout.Label($"Targets Registered: {m_Targets.Count}");
            GUILayout.EndArea();
        }
#endif
        #endregion
    }
}
