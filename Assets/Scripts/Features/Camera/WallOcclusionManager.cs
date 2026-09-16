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
        [SerializeField] private float checkInterval = 0.05f;

        [Tooltip("Height from ground to cast ray (player center)")]
        [SerializeField] private float raycastHeight = 1.5f;

        [Tooltip("Additional distance buffer for raycast")]
        [SerializeField] private float raycastDistanceBuffer = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugDrawRays = false;

        private HashSet<WallOccluder> currentlyOccluding = new HashSet<WallOccluder>();
        private float lastCheckTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null) mainCamera = Camera.main;
            if (player == null) {
                var playerObj = GameObject.Find("Player");
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
                if (currentlyOccluding.Count > 0)
                    UpdateOcclusionState(new HashSet<WallOccluder>());
                return;
            }

            if (mainCamera == null || player == null) return;

            if (Time.time - lastCheckTime >= checkInterval)
            {
                CheckOcclusion();
                lastCheckTime = Time.time;
            }
        }

        private void CheckOcclusion()
        {
            if (mainCamera == null || player == null) return;

            Vector3 playerCenter = player.position + Vector3.up * raycastHeight;
            Vector3 camPos = mainCamera.transform.position;

            Vector3 horizontalDir = camPos - playerCenter;
            horizontalDir.y = 0f;
            horizontalDir.Normalize();

            float playerToCamDist = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), new Vector3(playerCenter.x, 0, playerCenter.z));
            float baseDistance = playerToCamDist + raycastDistanceBuffer;

            HashSet<WallOccluder> newOccluding = new HashSet<WallOccluder>();

            // Fan raycast: multiple rays in a spread to catch corner walls
            int rayCount = 5;
            float maxFanAngle = 12f; // degrees - reduced from 25 for precision
            
            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
                float angle = Mathf.Lerp(-maxFanAngle, maxFanAngle, t);
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rayDir = rot * horizontalDir;

                Vector3 rayOrigin = playerCenter;

                if (debugDrawRays)
                {
                    Debug.DrawRay(rayOrigin, rayDir * baseDistance, Color.red, checkInterval);
                }

                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, baseDistance, occluderLayerMask.value, QueryTriggerInteraction.Collide);

                foreach (var hit in hits)
                {
                    var occluder = hit.collider.GetComponent<WallOccluder>();
                    if (occluder != null)
                    {
                        // Only occlude walls BETWEEN player and camera (hit.distance < playerToCamDist)
                        if (hit.distance < playerToCamDist + 0.5f)
                        {
                            newOccluding.Add(occluder);
                        }
                    }
                }
            }

            UpdateOcclusionState(newOccluding);
        }

        private void UpdateOcclusionState(HashSet<WallOccluder> newOccluding)
        {
            var toRemove = new List<WallOccluder>();
            foreach (var occluder in currentlyOccluding) {
                if (!newOccluding.Contains(occluder)) {
                    occluder.SetOccluding(false);
                    toRemove.Add(occluder);
                }
            }
            foreach (var o in toRemove) currentlyOccluding.Remove(o);

            foreach (var occluder in newOccluding) {
                if (!currentlyOccluding.Contains(occluder)) {
                    occluder.SetOccluding(true);
                    currentlyOccluding.Add(occluder);
                }
            }
        }

        public void ForceAllTransparent(bool transparent)
        {
            var allOccluders = FindObjectsByType<WallOccluder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var o in allOccluders) {
                o.SetOccluding(transparent);
            }
        }

        public void RefreshNow()
        {
            lastCheckTime = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawRays || mainCamera == null || player == null) return;

            Vector3 playerCenter = player.position + Vector3.up * raycastHeight;
            Vector3 camPos = mainCamera.transform.position;

            Vector3 horizontalDir = camPos - playerCenter;
            horizontalDir.y = 0f;
            horizontalDir.Normalize();

            float playerToCamDist = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), new Vector3(playerCenter.x, 0, playerCenter.z));
            float distance = playerToCamDist + raycastDistanceBuffer;

            // Draw fan rays to match CheckOcclusion logic
            int rayCount = 5;
            float maxFanAngle = 12f;
            
            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
                float angle = Mathf.Lerp(-maxFanAngle, maxFanAngle, t);
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rayDir = rot * horizontalDir;
                
                Gizmos.color = Color.red;
                Gizmos.DrawRay(playerCenter, rayDir * distance);
            }
        }
    }
}