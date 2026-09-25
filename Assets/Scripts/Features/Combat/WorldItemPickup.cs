using System.Collections;
using UnityEngine;
using PlayerUI;

namespace FeaturesCombat
{
    /// <summary>
    /// Item drop fisik di dunia yang terlempar ke belakang saat monster mati, memantul,
    /// menampilkan visual ikon sprite 2D billboard, dan tertarik secara magnetis ke arah pemain.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class WorldItemPickup : MonoBehaviour
    {
        [Header("Drop Data")]
        public ItemData item;
        public int quantity = 1;

        [Header("Pickup Settings")]
        [SerializeField] private float pickupRadius = 2.8f;
        [SerializeField] private float flySpeed = 11f;
        [SerializeField] private float bobSpeed = 3.5f;
        [SerializeField] private float bobHeight = 0.08f;

        [Header("Visual Settings")]
        [Tooltip("Target ukuran diameter ikon drop di dunia (meter). Default: 0.40m (40 cm) agar proporsional dan tidak lebih besar dari player.")]
        [SerializeField] private float desiredIconSize = 0.40f;

        private Transform playerTransform;
        private InventoryComponent playerInventory;
        private Camera mainCam;
        private bool isFlyingToPlayer = false;
        private bool isTossing = false;
        private Vector3 startPos;
        private GameObject iconBillboardGO;

        private void Start()
        {
            startPos = transform.position;
            mainCam = Camera.main;
            FindPlayer();

            var col = GetComponent<SphereCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = pickupRadius;
            }

            SetupVisual();
        }

        public void SetupVisual()
        {
            if (item == null || item.itemIcon == null) return;

            SpriteRenderer sr;
            if (iconBillboardGO == null)
            {
                iconBillboardGO = new GameObject("IconBillboard");
                iconBillboardGO.transform.SetParent(transform, false);
                iconBillboardGO.transform.localPosition = Vector3.up * 0.25f;

                sr = iconBillboardGO.AddComponent<SpriteRenderer>();
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sr.receiveShadows = false;
                sr.sortingOrder = 5;
            }
            else
            {
                sr = iconBillboardGO.GetComponent<SpriteRenderer>();
            }

            if (sr != null)
            {
                sr.sprite = item.itemIcon;

                // Hitung skala agar ukuran ikon di dunia game proporsional (~0.40m / 40 cm)
                // terlepas dari resolusi sprite (1024x1024 dsb) maupun scale root GameObject.
                float spriteUnscaledSize = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                if (spriteUnscaledSize > 0.001f)
                {
                    float parentScale = Mathf.Max(transform.lossyScale.x, 0.001f);
                    float targetScale = (desiredIconSize / parentScale) / spriteUnscaledSize;
                    iconBillboardGO.transform.localScale = Vector3.one * targetScale;
                }
                else
                {
                    iconBillboardGO.transform.localScale = Vector3.one * 0.04f;
                }
            }

            // Sembunyikan mesh renderer bola agar ikon menjadi visual utama
            var meshR = GetComponent<MeshRenderer>();
            if (meshR != null)
            {
                meshR.enabled = false;
            }
        }

        public void Toss(Vector3 tossDirection, float tossDistance = 2.4f, float duration = 0.48f)
        {
            StartCoroutine(RoutineTossArc(tossDirection, tossDistance, duration));
        }

        private IEnumerator RoutineTossArc(Vector3 tossDirection, float tossDistance, float duration)
        {
            isTossing = true;
            Vector3 origin = transform.position;

            // Pastikan arah horizontal
            tossDirection.y = 0f;
            if (tossDirection.sqrMagnitude < 0.001f)
            {
                tossDirection = -Vector3.forward;
            }
            tossDirection.Normalize();

            Vector3 destination = origin + tossDirection * tossDistance;

            // Cari elevasi tanah melalui raycast
            if (Physics.Raycast(destination + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f))
            {
                destination.y = hit.point.y + 0.15f;
            }
            else
            {
                destination.y = origin.y;
            }

            float arcHeight = 1.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Parabola: 4 * h * t * (1 - t)
                float currentY = Mathf.Sin(t * Mathf.PI) * arcHeight;
                Vector3 currentXZ = Vector3.Lerp(origin, destination, t);
                transform.position = new Vector3(currentXZ.x, Mathf.Lerp(origin.y, destination.y, t) + currentY, currentXZ.z);

                yield return null;
            }

            // Mini bounce setelah jatuh ke tanah
            float bounceDuration = 0.16f;
            float bounceHeight = 0.30f;
            float bounceElapsed = 0f;
            Vector3 bounceOrigin = destination;
            Vector3 bounceDestination = destination + tossDirection * 0.35f;

            while (bounceElapsed < bounceDuration)
            {
                bounceElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(bounceElapsed / bounceDuration);
                float currentY = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                Vector3 currentXZ = Vector3.Lerp(bounceOrigin, bounceDestination, t);
                transform.position = new Vector3(currentXZ.x, bounceDestination.y + currentY, currentXZ.z);
                yield return null;
            }

            transform.position = bounceDestination;
            startPos = transform.position;
            isTossing = false;
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
            // Billboard ikon menghadap kamera
            if (iconBillboardGO != null)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    iconBillboardGO.transform.rotation = mainCam.transform.rotation;
                }
            }

            if (isTossing) return;

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
            if (isTossing) return;

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
        /// Helper untuk memunculkan item drop di posisi tertentu dengan lemparan ke arah belakang.
        /// </summary>
        public static WorldItemPickup Spawn(Vector3 position, ItemData item, int quantity, Vector3 tossDirection = default)
        {
            if (item == null || quantity <= 0) return null;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Drop_{item.itemName}";
            go.transform.position = position + Vector3.up * 0.4f;
            go.transform.localScale = Vector3.one;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.95f, 0.75f, 0.2f);
            }

            var pickup = go.AddComponent<WorldItemPickup>();
            pickup.item = item;
            pickup.quantity = quantity;
            pickup.SetupVisual();

            if (tossDirection != default)
            {
                pickup.Toss(tossDirection, Random.Range(1.8f, 2.7f), Random.Range(0.42f, 0.52f));
            }

            return pickup;
        }
    }
}
