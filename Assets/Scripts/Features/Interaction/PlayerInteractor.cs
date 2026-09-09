using FeaturesWardrobe;
using UnityEngine;

namespace FeaturesInteraction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Deteksi Interaksi")]
        // Radius diperkecil agar interaksi lebih presisi (default 2.5f).
        [SerializeField] private float interactRadius = 2.5f;

        [Header("Layer Interactable")]
        public LayerMask interactableLayer = ~0;

        private IInteractable currentInteractable;

        private void Awake()
        {
            this.enabled = true;
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

                float dist = (targetTransform.position - transform.position).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }

            return best;
        }

        private bool IsInSameZone(Transform target)
        {
            // Find zone of target
            var targetZone = target.GetComponentInParent<InteractionZone>();
            if (targetZone == null) return true; // No zone = always accessible

            // Jika player belum di dalam zona khusus mana pun atau di zona yang sama, izinkan
            if (currentZone == null || currentZone == targetZone) return true;

            // Fallback: periksa apakah posisi player saat ini berada di dalam bounds collider zone tersebut
            Vector3 playerPos = transform.position;
            if (targetZone.ContainsPoint(playerPos) || targetZone.ContainsPoint(playerPos + Vector3.up * 0.5f))
            {
                currentZone = targetZone;
                return true;
            }

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