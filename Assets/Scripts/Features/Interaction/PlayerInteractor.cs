using FeaturesWardrobe;
using UnityEngine;

namespace FeaturesInteraction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Deteksi Interaksi")]
        // Radius interaksi presisi yang nyaman (default 2.0f).
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
            if (WardrobeManager.IsInWardrobeMode) return;

            currentInteractable = FindClosestInteractable();
        }

        private InteractionZone currentZone;

        private IInteractable FindClosestInteractable()
        {
            Vector3 playerCenter = transform.position + Vector3.up * 0.8f;
            Collider[] hits = Physics.OverlapSphere(playerCenter, interactRadius, interactableLayer.value);

            IInteractable best = null;
            float bestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                // Cek IInteractable di collider ini, di parent-nya, atau di children-nya
                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null)
                    interactable = hit.GetComponentInChildren<IInteractable>();

                if (interactable == null) continue;

                // Hitung posisi representatif objek interaktif
                Transform targetTransform = (interactable is MonoBehaviour mb) ? mb.transform : hit.transform;

                // ZONE CHECK: If interactable is in a zone, player must be in same zone
                if (!IsInSameZone(targetTransform))
                    continue;

                // Pintu (DoorInteractable) tertanam pada kusen/bukaan dinding sehingga dikecualikan dari pemblokiran raycast dinding
                bool isDoor = (interactable is DoorInteractable) || targetTransform.GetComponentInParent<DoorInteractable>() != null;
                if (!isDoor && IsObstructedByWall(targetTransform))
                    continue;

                // Jarak dihitung dari titik terdekat collider target ke pusat tubuh pemain
                Vector3 closestPoint = hit.ClosestPoint(playerCenter);
                float dist = (closestPoint - playerCenter).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
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

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            {
                // Jika terkena collider solid yang bukan bagian dari target dan bukan collider player
                if (hit.collider != null && hit.collider != playerCollider)
                {
                    if (!hit.collider.transform.IsChildOf(target) && !target.IsChildOf(hit.collider.transform))
                    {
                        // Hanya blokir jika permukaan yang tertabrak adalah bidang vertikal/dinding
                        float wallAngle = Vector3.Angle(hit.normal, Vector3.up);
                        if (wallAngle > 45f && wallAngle < 135f)
                        {
                            return true; // Terhalang dinding solid
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

            // Objek berada di dalam zona tertentu (mis. Bedroom).
            // Player harus berada di dalam zona yang sama.
            if (currentZone == targetZone) return true;

            // Fallback: periksa apakah posisi player saat ini berada di dalam bounds collider zone tersebut
            Vector3 playerPos = transform.position;
            if (targetZone.ContainsPoint(playerPos) || targetZone.ContainsPoint(playerPos + Vector3.up * 0.5f))
            {
                currentZone = targetZone;
                return true;
            }

            // Player berada di luar zona kamar/koleksi -> tolak interaksi tembus kamar
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
            if (WardrobeManager.IsInWardrobeMode)
            {
                Debug.LogWarning("[PlayerInteractor] Interaksi dibatalkan karena WardrobeManager.IsInWardrobeMode = true.");
                return;
            }

            // Jika belum terisi, coba lakukan pencarian objek interaksi terdekat sekali lagi
            if (currentInteractable == null)
                currentInteractable = FindClosestInteractable();

            if (currentInteractable == null)
            {
                Debug.LogWarning("[PlayerInteractor] Tombol E ditekan, tapi tidak ada objek interaksi di dekat pemain (atau terhalang zona/layer mask).");
                return;
            }

            Debug.Log($"[PlayerInteractor] Berhasil berinteraksi dengan: {currentInteractable}");
            currentInteractable.Interact(gameObject);
        }

        // Target interaktif terdekat (untuk hover UI). Null bila tidak ada objek interaktif.
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