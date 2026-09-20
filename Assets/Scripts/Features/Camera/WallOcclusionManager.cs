using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCamera
{
    public class WallOcclusionManager : MonoBehaviour
    {
        #region Singleton
        private static WallOcclusionManager _instance;
        public static WallOcclusionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<WallOcclusionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        #endregion

        [Header("References")]
        [Tooltip("Main camera (isometric)")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Player transform (center of body)")]
        [SerializeField] private Transform player;

        [Header("Occlusion Settings")]
        [Tooltip("Layer mask for occluding walls")]
        [SerializeField] private LayerMask occluderLayerMask = -1;

        [Tooltip("How often to check for occlusion (seconds)")]
        [SerializeField] private float checkInterval = 0.03f;

        [Header("Occlusion Sampling")]
        [Tooltip("Vertical heights relative to player pivot to test for occlusion (0.5m = lower body/legs, 1.0m = waist/torso, 1.5m = upper body/head)")]
        [SerializeField] private float[] verticalSampleHeights = new float[] { 0.5f, 1.0f, 1.5f };

        [Tooltip("Number of horizontal fan rays for mid/upper samples to cover character width")]
        [Range(1, 5)]
        [SerializeField] private int fanRayCount = 3;

        [Tooltip("Half-angle of fan spread in degrees for mid/upper samples")]
        [Range(1f, 15f)]
        [SerializeField] private float maxFanAngle = 4f;

        [Header("Debug")]
        [SerializeField] private bool debugDrawRays = false;

        // Reusable collections to achieve 0 bytes GC allocation per frame
        private readonly HashSet<WallOccluder> currentlyOccluding = new HashSet<WallOccluder>();
        private readonly HashSet<WallOccluder> newOccluding = new HashSet<WallOccluder>();
        private readonly List<WallOccluder> toRemove = new List<WallOccluder>();
        private readonly RaycastHit[] hitsBuffer = new RaycastHit[16];

        private float lastCheckTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (player == null) {
                var playerObj = GameObject.Find("Player");
                if (playerObj == null) playerObj = GameObject.Find("PlayerCapsule");
                if (playerObj != null) player = playerObj.transform;
            }
        }

        private void LateUpdate()
        {
            // Occlusion only applies to the isometric gameplay view.
            // In Wardrobe/Trophy mode restore all walls and skip checks.
            if (CameraManager.Instance != null &&
                CameraManager.Instance.CurrentMode != CameraManager.CameraMode.Gameplay)
            {
                ClearAllOcclusion();
                return;
            }

            EnsureReferences();
            if (mainCamera == null || player == null) return;

            if (Time.time - lastCheckTime >= checkInterval)
            {
                CheckOcclusion();
                lastCheckTime = Time.time;
            }
        }

        /// <summary>
        /// Industry-standard multi-height true 3D Line-of-Sight occlusion check.
        /// Samples lower body (feet/legs), waist, and upper body so walls fade smoothly
        /// the moment any part of the player is obstructed, not just when the neck/head is covered.
        /// Zero heap allocation (Physics.RaycastNonAlloc and reusable hash sets).
        /// </summary>
        private void CheckOcclusion()
        {
            EnsureReferences();
            if (mainCamera == null || player == null) return;

            Vector3 playerPos = player.position;
            Vector3 camPos = mainCamera.transform.position;

            newOccluding.Clear();

            var heights = (verticalSampleHeights != null && verticalSampleHeights.Length > 0)
                ? verticalSampleHeights
                : new float[] { 0.5f, 1.0f, 1.5f };

            for (int s = 0; s < heights.Length; s++)
            {
                float h = heights[s];
                Vector3 origin = playerPos + Vector3.up * h;

                Vector3 toCam = camPos - origin;
                float distToCam = toCam.magnitude;
                if (distToCam <= 0.05f) continue;

                Vector3 baseDir = toCam / distToCam;
                float maxRayDist = Mathf.Max(0.1f, distToCam - 0.05f);

                // Lower sample (legs/feet) uses a single center ray to avoid catching low side obstacles.
                // Mid and upper samples use the horizontal fan to cover character width.
                int raysForThisHeight = (s == 0 && heights.Length > 1) ? 1 : fanRayCount;

                for (int i = 0; i < raysForThisHeight; i++)
                {
                    Vector3 rayDir;
                    if (raysForThisHeight > 1)
                    {
                        float t = (float)i / (raysForThisHeight - 1);
                        float angle = Mathf.Lerp(-maxFanAngle, maxFanAngle, t);
                        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                        rayDir = rot * baseDir;
                    }
                    else
                    {
                        rayDir = baseDir;
                    }

                    if (debugDrawRays)
                    {
                        Debug.DrawRay(origin, rayDir * maxRayDist, Color.red, checkInterval);
                    }

                    int hitCount = Physics.RaycastNonAlloc(
                        origin,
                        rayDir,
                        hitsBuffer,
                        maxRayDist,
                        occluderLayerMask.value,
                        QueryTriggerInteraction.Collide
                    );

                    for (int k = 0; k < hitCount; k++)
                    {
                        var col = hitsBuffer[k].collider;
                        if (col == null) continue;

                        var occluder = col.GetComponent<WallOccluder>();
                        if (occluder != null)
                        {
                            newOccluding.Add(occluder);
                        }
                    }
                }
            }

            UpdateOcclusionState();
        }

        /// <summary>
        /// Updates currently occluding set without heap allocations.
        /// </summary>
        private void UpdateOcclusionState()
        {
            toRemove.Clear();
            foreach (var occluder in currentlyOccluding)
            {
                if (occluder == null || !newOccluding.Contains(occluder))
                {
                    if (occluder != null) occluder.SetOccluding(false);
                    toRemove.Add(occluder);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                currentlyOccluding.Remove(toRemove[i]);
            }

            foreach (var occluder in newOccluding)
            {
                if (occluder != null && !currentlyOccluding.Contains(occluder))
                {
                    occluder.SetOccluding(true);
                    currentlyOccluding.Add(occluder);
                }
            }
        }

        private void ClearAllOcclusion()
        {
            if (currentlyOccluding.Count == 0) return;

            foreach (var occluder in currentlyOccluding)
            {
                if (occluder != null) occluder.SetOccluding(false);
            }
            currentlyOccluding.Clear();
        }

        public void ForceAllTransparent(bool transparent)
        {
            var allOccluders = FindObjectsByType<WallOccluder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var o in allOccluders) {
                if (o != null) o.SetOccluding(transparent);
            }
        }

        public void RefreshNow()
        {
            lastCheckTime = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawRays || mainCamera == null || player == null) return;

            Vector3 playerPos = player.position;
            Vector3 camPos = mainCamera.transform.position;

            var heights = (verticalSampleHeights != null && verticalSampleHeights.Length > 0)
                ? verticalSampleHeights
                : new float[] { 0.5f, 1.0f, 1.5f };

            Gizmos.color = Color.red;

            for (int s = 0; s < heights.Length; s++)
            {
                float h = heights[s];
                Vector3 origin = playerPos + Vector3.up * h;

                Vector3 toCam = camPos - origin;
                float distToCam = toCam.magnitude;
                if (distToCam <= 0.05f) continue;

                Vector3 baseDir = toCam / distToCam;
                float maxRayDist = Mathf.Max(0.1f, distToCam - 0.05f);

                int raysForThisHeight = (s == 0 && heights.Length > 1) ? 1 : fanRayCount;

                for (int i = 0; i < raysForThisHeight; i++)
                {
                    Vector3 rayDir;
                    if (raysForThisHeight > 1)
                    {
                        float t = (float)i / (raysForThisHeight - 1);
                        float angle = Mathf.Lerp(-maxFanAngle, maxFanAngle, t);
                        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                        rayDir = rot * baseDir;
                    }
                    else
                    {
                        rayDir = baseDir;
                    }

                    Gizmos.DrawRay(origin, rayDir * maxRayDist);
                }
            }
        }
    }
}