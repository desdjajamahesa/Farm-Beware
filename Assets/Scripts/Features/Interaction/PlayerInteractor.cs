using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesInteraction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Deteksi Interaksi")]
        // Precise, close-range interaction radius (default 2.0f).
        [SerializeField] private float interactRadius = 2.0f;

        [Header("Layer Interactable")]
        public LayerMask interactableLayer = ~0;

        private IInteractable currentInteractable;
        private Collider playerCollider;

        private void Awake()
        {
            this.enabled = true;
            playerCollider = GetComponent<Collider>();
        }

        void Update()
        {
            // Early exit if in Wardrobe Mode (prevents interaction detection)
            var cam = ServiceLocator.Resolve<ICameraService>();
            if (cam != null && cam.CurrentMode == CameraMode.WardrobeMode) return;

            // Do not search for interactions while player input is locked (UI is open)
            var pc = GetComponent<IPlayerContext>();
            if (pc != null && pc.IsInputLocked)
            {
                currentInteractable = null;
                return;
            }

            currentInteractable = FindClosestInteractable();
        }

        private InteractionZone currentZone;
        private static readonly Collider[] _interactHits = new Collider[64];

        private IInteractable FindClosestInteractable()
        {
            Vector3 playerCenter = transform.position + Vector3.up * 0.8f;
            int hitCount = NonAllocPhysics.OverlapSphereNonAlloc(playerCenter, interactRadius, _interactHits, interactableLayer.value);

            IInteractable best = null;
            float bestDist = float.MaxValue;

            for (int h = 0; h < hitCount; h++)
            {
                Collider hit = _interactHits[h];
                if (hit == null) continue;
                // Ignore room zone trigger colliders (e.g., BedroomZone)
                if (hit.GetComponent<InteractionZone>() != null)
                    continue;

                // Check IInteractable on the collider itself or its parents (do not search children to avoid targeting wrong objects inside containers)
                IInteractable interactable = hit.GetComponent<IInteractable>() ?? hit.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                // Compute representative transform of the interactive object
                Transform targetTransform = (interactable is MonoBehaviour mb) ? mb.transform : hit.transform;

                // ZONE CHECK: If interactable is in a zone, player must be in same zone
                if (!IsInSameZone(targetTransform))
                    continue;

                // Objects implementing IObstructionExempt are excluded from wall/furniture raycast blocking
                bool isExempt = (interactable is IObstructionExempt)
                    || targetTransform.GetComponent<IObstructionExempt>() != null
                    || targetTransform.GetComponentInParent<IObstructionExempt>() != null;

                if (!isExempt && IsObstructedByWall(targetTransform))
                    continue;

                // CAN-INTERACT CHECK: Query whether the object can currently be interacted with
                if (!interactable.CanInteract(gameObject))
                    continue;

                // Distance is measured from the closest point on the target collider to the player's body center
                Vector3 closestPoint = (hit is MeshCollider mc && !mc.convex) 
                    ? hit.bounds.ClosestPoint(playerCenter) 
                    : hit.ClosestPoint(playerCenter);
                Vector3 toTarget = closestPoint - playerCenter;
                Vector3 toTargetH = Vector3.ProjectOnPlane(toTarget, Vector3.up);
                float distSq = toTargetH.sqrMagnitude;

                // Player facing orientation: prioritize objects in front of the player
                float dot = 1.0f;
                if (toTargetH.sqrMagnitude > 0.04f)
                {
                    dot = Vector3.Dot(transform.forward, toTargetH.normalized);
                }

                // Ignore objects clearly behind the player (unless very close / overlapping)
                if (dot < -0.15f && distSq > 0.36f)
                {
                    continue;
                }

                // Directional weighting: objects in front get a lower score penalty (higher priority)
                float dirFactor = Mathf.Lerp(1.25f, 0.75f, (dot + 1f) * 0.5f);
                float effectiveScore = distSq * dirFactor;

                if (effectiveScore < bestDist)
                {
                    bestDist = effectiveScore;
                    best = interactable;
                }
            }

            // Clear buffer to prevent memory retention
            for (int h = 0; h < hitCount; h++)
            {
                _interactHits[h] = null;
            }

            return best;
        }

        private bool IsObstructedByWall(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 targetCenter = target.position + Vector3.up * 0.5f;
            Vector3 dir = targetCenter - origin;
            float dist = dir.magnitude;

            if (dist < 0.1f) return false;

            // Check only the wall layer (Layer 12 "Wall") so furniture/chairs/decorations are not misidentified as walls
            int wallMask = LayerMask.GetMask("Wall");
            if (wallMask == 0) wallMask = (1 << 12);

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, wallMask, QueryTriggerInteraction.Ignore))
            {
                // If hitting a solid wall collider that does not belong to the target or the player
                if (hit.collider != null && hit.collider != playerCollider)
                {
                    if (!hit.collider.transform.IsChildOf(target) && !target.IsChildOf(hit.collider.transform))
                    {
                        // Only block if the hit surface is vertical / wall-like
                        float wallAngle = Vector3.Angle(hit.normal, Vector3.up);
                        if (wallAngle > 45f && wallAngle < 135f)
                        {
                            return true; // Obstructed by solid wall
                        }
                    }
                }
            }

            return false;
        }

        private bool IsInSameZone(Transform target)
        {
            // Find zone of target
            var targetZone = target.GetComponentInParent<InteractionZone>();
            if (targetZone == null) return true; // No zone = always accessible

            // Object is inside a specific zone (e.g., Bedroom).
            // Player must be inside the same zone.
            if (currentZone == targetZone) return true;

            // Fallback: check whether the player's current position is within the zone collider's bounds
            Vector3 playerPos = transform.position;
            if (targetZone.ContainsPoint(playerPos) || targetZone.ContainsPoint(playerPos + Vector3.up * 0.5f))
            {
                currentZone = targetZone;
                return true;
            }

            // Player is outside the room/collection zone -> reject cross-room interaction
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<InteractionZone>(out var zone))
                currentZone = zone;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<InteractionZone>(out var zone) && currentZone == zone)
                currentZone = null;
        }

        public void OnInteractInput()
        {
            var camService = ServiceLocator.Resolve<ICameraService>();
            if (camService != null && camService.CurrentMode == CameraMode.WardrobeMode)
            {
                Debug.LogWarning("[PlayerInteractor] Interaksi dibatalkan karena mode Wardrobe aktif.");
                return;
            }

            // If not yet populated, attempt one more search for the nearest interactable object
            if (currentInteractable == null)
                currentInteractable = FindClosestInteractable();

            if (currentInteractable == null)
            {
                Debug.LogWarning("[PlayerInteractor] Tombol E ditekan, tapi tidak ada objek interaksi di dekat pemain (atau terhalang zona/layer mask/tembok).");
                return;
            }

            Debug.Log($"[PlayerInteractor] Berhasil berinteraksi dengan: {currentInteractable}");
            currentInteractable.Interact(gameObject);
        }

        // Nearest interactive target (for hover UI). Null if no interactive object is in range.
        public GameObject CurrentTarget
        {
            get
            {
                if (currentInteractable is MonoBehaviour mb)
                    return mb.gameObject;
                return null;
            }
        }
    }
}