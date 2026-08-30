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
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable == null) continue;

                // ZONE CHECK: If interactable is in a zone, player must be in same zone
                if (!IsInSameZone(hit.transform))
                    continue;

                float dist = (hit.transform.position - transform.position).sqrMagnitude;
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

            // Check if player is in same zone
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
            if (WardrobeManager.IsInWardrobeMode) return;

            if (currentInteractable == null) return;

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