using FeaturesWardrobe;
using UnityEngine;

namespace FeaturesInteraction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Deteksi Interaksi")]
        // Radius diperkecil agar interaksi lebih presisi.
        [SerializeField] private float interactRadius = 1.8f;

        [Header("Layer Interactable")]
        public LayerMask interactableLayer = ~0;

        [Header("Line-of-Sight (Wall Occlusion)")]
        [Tooltip("Layer mask tembok/dinding untuk pengecekan garis pandang.")]
        [SerializeField] private LayerMask wallLayerMask = 0;

        [Tooltip("Tinggi titik asal raycast dari posisi player (setinggi dada/mata).")]
        [SerializeField] private float losEyeHeight = 1.0f;

        private IInteractable currentInteractable;

        private void Awake()
        {
            this.enabled = true;

            // Auto-detect Wall layer jika belum di-set di Inspector
            if (wallLayerMask.value == 0)
            {
                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0)
                    wallLayerMask = 1 << wallLayer;
            }
        }

        void Update()
        {
            // Early exit if in Wardrobe Mode (prevents interaction detection)
            if (WardrobeManager.IsInWardrobeMode) return;

            // Jangan cari interaksi saat player input terkunci (UI sedang terbuka)
            var pc = GetComponent<PlayerControl>();
            if (pc != null && pc.isInputLocked)
            {
                currentInteractable = null;
                return;
            }

            currentInteractable = FindClosestInteractable();
        }

        private InteractionZone currentZone;

        private IInteractable FindClosestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactableLayer.value);

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

                // LINE-OF-SIGHT CHECK: Pastikan tidak ada tembok menghalangi
                if (!HasLineOfSight(hit, targetTransform, interactable))
                    continue;

                // CAN-INTERACT CHECK: Tanyakan ke objek apakah bisa diinteraksikan saat ini
                if (!interactable.CanInteract(gameObject))
                    continue;

                Vector3 chestPosition = transform.position + Vector3.up * losEyeHeight;
                Vector3 targetPoint = hit != null ? hit.bounds.center : targetTransform.position;
                float dist = (targetPoint - chestPosition).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }

            return best;
        }

        /// <summary>
        /// Memeriksa apakah ada garis pandang langsung (tidak terhalang tembok/dinding)
        /// dari posisi mata pemain ke titik pusat collider target.
        /// Untuk DoorInteractable (pintu yang tertanam pada dinding), dinding tempat pintu terpasang
        /// diabaikan agar pintu tetap dapat diakses, namun dinding lain tetap memblokir.
        /// </summary>
        private bool HasLineOfSight(Collider targetCollider, Transform targetTransform, IInteractable interactable)
        {
            if (wallLayerMask.value == 0)
                return true; // Tidak ada wall layer, skip check

            Vector3 eyePosition = transform.position + Vector3.up * losEyeHeight;
            Vector3 targetPoint = targetCollider != null ? targetCollider.bounds.center : targetTransform.position;

            Vector3 dir = targetPoint - eyePosition;
            float dist = dir.magnitude;
            if (dist < 0.001f)
                return true;

            // Gunakan RaycastAll untuk memeriksa semua dinding yang terlalui oleh garis pandang
            RaycastHit[] hits = Physics.RaycastAll(eyePosition, dir.normalized, dist, wallLayerMask.value, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return true;

            // Jika objek interaksi adalah DoorInteractable (pintu):
            // Dinding yang bersinggungan langsung dengan kusen/pintu ini adalah bagian dari bukaan pintu,
            // sehingga tidak memblokir interaksi ke pintu itu sendiri.
            if (interactable is DoorInteractable)
            {
                Bounds doorBounds = targetCollider != null ? targetCollider.bounds : new Bounds(targetTransform.position, Vector3.one);
                foreach (var hit in hits)
                {
                    if (!hit.collider.bounds.Intersects(doorBounds))
                    {
                        return false; // Ada dinding lain yang menghalangi pandangan ke pintu
                    }
                }
                return true;
            }

            // Untuk objek non-pintu (furniture, kasur, dsb):
            // Setiap dinding yang tertabrak berarti objek berada di balik tembok
            return false;
        }

        private bool IsInSameZone(Transform target)
        {
            // Find zone of target
            var targetZone = target.GetComponentInParent<InteractionZone>();
            if (targetZone == null) return true; // No zone = always accessible

            // Jika target berada di dalam zona, pemain HARUS berada di zona yang sama
            if (currentZone == null)
            {
                // Pemain tidak di zona mana pun — cek fallback apakah posisi pemain ada di bounds zona target
                Vector3 playerPos = transform.position;
                if (targetZone.ContainsPoint(playerPos) || targetZone.ContainsPoint(playerPos + Vector3.up * 0.5f))
                {
                    currentZone = targetZone;
                    return true;
                }
                // Pemain di luar zona target → tolak interaksi
                return false;
            }

            // Pemain di zona tertentu, izinkan hanya jika zona sama
            return currentZone == targetZone;
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
                Debug.LogWarning("[PlayerInteractor] Tombol E ditekan, tapi tidak ada objek interaksi di dekat pemain (atau terhalang zona/layer mask/tembok).");
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