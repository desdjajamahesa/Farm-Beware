using UnityEngine;

namespace FeaturesInteraction
{
    public class InteractionZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [Tooltip("Nama zona (mis. \"Bedroom\", \"Kitchen\")")]
        [SerializeField] private string zoneName = "Zone";

        [Tooltip("Collider trigger yang mendefinisikan batas zona")]
        [SerializeField] private Collider zoneCollider;

        public string ZoneName => zoneName;

        public bool ContainsPoint(Vector3 point)
        {
            if (zoneCollider == null) return false;
            return zoneCollider.bounds.Contains(point);
        }

        private void OnValidate()
        {
            if (zoneCollider == null) zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null) zoneCollider.isTrigger = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (zoneCollider == null) return;
            
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            Gizmos.DrawCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
        }
    }
}