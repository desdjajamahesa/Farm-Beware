using UnityEngine;
using PlayerUI;

namespace FeaturesCombat
{
    /// <summary>
    /// Item drop fisik di dunia yang memantul dan secara otomatis tertarik (magnet) ke arah pemain saat mendekat.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class WorldItemPickup : MonoBehaviour
    {
        [Header("Drop Data")]
        public ItemData item;
        public int quantity = 1;

        [Header("Pickup Settings")]
        [SerializeField] private float pickupRadius = 2.5f;
        [SerializeField] private float flySpeed = 10f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float bobHeight = 0.1f;

        private Transform playerTransform;
        private InventoryComponent playerInventory;
        private bool isFlyingToPlayer = false;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
            FindPlayer();

            var col = GetComponent<SphereCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = pickupRadius;
            }
        }

        private void FindPlayer()
        {
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerInventory = player.GetComponent<InventoryComponent>();
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (isFlyingToPlayer)
            {
                Vector3 targetPos = playerTransform.position + Vector3.up * 0.9f;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPos) < 0.35f)
                {
                    Collect();
                }
            }
            else
            {
                // Idle gentle hover
                float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = startPos + new Vector3(0f, yOffset, 0f);

                if (dist <= pickupRadius)
                {
                    isFlyingToPlayer = true;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerControl>() != null)
            {
                isFlyingToPlayer = true;
            }
        }

        private void Collect()
        {
            if (playerInventory != null && item != null)
            {
                bool added = playerInventory.AddItem(item, quantity);
                if (added)
                {
                    if (FloatingCombatTextManager.Instance != null && playerTransform != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            playerTransform.position + Vector3.up * 1.5f,
                            $"+{quantity} {item.itemName}",
                            new Color(0.95f, 0.85f, 0.25f));
                    }
                }
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Helper untuk memunculkan item drop di posisi tertentu di arena.
        /// </summary>
        public static WorldItemPickup Spawn(Vector3 position, ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return null;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Drop_{item.itemName}";
            go.transform.position = position + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * 0.35f;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.95f, 0.75f, 0.2f);
            }

            var pickup = go.AddComponent<WorldItemPickup>();
            pickup.item = item;
            pickup.quantity = quantity;

            return pickup;
        }
    }
}
