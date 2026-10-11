using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCombat.Projectiles
{
    /// <summary>
    /// Zero-GC Object Pool for combat projectiles (Corn Musketeer kernels, magic bolts, energy missiles).
    /// Pre-allocates and recycles projectile GameObjects to eliminate runtime heap churn,
    /// GC spikes, and memory leaks during horde combat encounters.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatProjectilePool : MonoBehaviour
    {
        public static CombatProjectilePool Instance { get; private set; }

        [Header("Pool Configuration")]
        [Tooltip("The projectile prefab template to instantiate. If null, automatically loaded from resources/assets.")]
        [SerializeField] private CombatProjectile projectilePrefab;

        [Tooltip("Initial number of pre-warmed projectiles instantiated upon Awake.")]
        [SerializeField] private int initialPoolSize = 32;

        [Tooltip("Maximum allowed pool capacity before capping growth.")]
        [SerializeField] private int maxPoolCapacity = 96;

        private readonly Queue<CombatProjectile> _availablePool = new Queue<CombatProjectile>(64);
        private readonly List<CombatProjectile> _activeProjectiles = new List<CombatProjectile>(64);
        private readonly List<CombatProjectile> _allInstantiated = new List<CombatProjectile>(64);
        private Transform _poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _poolRoot = transform;

            PrewarmPool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Ensures an active instance of the CombatProjectilePool exists in the scene,
        /// auto-instantiating one under '_SYSTEMS' or as a root GameObject if absent.
        /// </summary>
        public static CombatProjectilePool EnsureInstanceExists()
        {
            if (Instance != null) return Instance;

            Instance = FindAnyObjectByType<CombatProjectilePool>();
            if (Instance != null) return Instance;

            var systemsGo = GameObject.Find("_SYSTEMS");
            var poolGo = new GameObject("CombatProjectilePool");
            if (systemsGo != null)
            {
                poolGo.transform.SetParent(systemsGo.transform, false);
            }

            Instance = poolGo.AddComponent<CombatProjectilePool>();
            return Instance;
        }

        private void PrewarmPool()
        {
            EnsurePrefabReference();

            if (projectilePrefab == null)
            {
                Debug.LogWarning("[CombatProjectilePool] No projectilePrefab assigned or found. Prewarm deferred.");
                return;
            }

            int countToSpawn = initialPoolSize - _allInstantiated.Count;
            for (int i = 0; i < countToSpawn; i++)
            {
                CreateNewInstance();
            }
        }

        private void EnsurePrefabReference()
        {
            if (projectilePrefab != null) return;

#if UNITY_EDITOR
            projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<CombatProjectile>(
                "Assets/Prefabs/Combat/CombatProjectilePrefab.prefab");
#endif
        }

        private CombatProjectile CreateNewInstance()
        {
            EnsurePrefabReference();

            CombatProjectile instance;
            if (projectilePrefab != null)
            {
                instance = Instantiate(projectilePrefab, _poolRoot);
            }
            else
            {
                // Fallback runtime generation if prefab is missing
                var tempGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tempGo.name = "CombatProjectile_Fallback";
                tempGo.transform.SetParent(_poolRoot, false);
                tempGo.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

                var col = tempGo.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var rb = tempGo.GetComponent<Rigidbody>() ?? tempGo.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                instance = tempGo.AddComponent<CombatProjectile>();
            }

            instance.gameObject.SetActive(false);
            _allInstantiated.Add(instance);
            _availablePool.Enqueue(instance);
            return instance;
        }

        /// <summary>
        /// Retrieves an inactive projectile from the pool, activates it at position, and launches it toward direction.
        /// </summary>
        public CombatProjectile Spawn(GameObject shooter, Vector3 position, Vector3 direction, int damage, float speed, Color color)
        {
            CombatProjectile proj = null;

            while (_availablePool.Count > 0)
            {
                var candidate = _availablePool.Dequeue();
                if (candidate != null)
                {
                    proj = candidate;
                    break;
                }
            }

            if (proj == null)
            {
                if (_allInstantiated.Count < maxPoolCapacity)
                {
                    proj = CreateNewInstance();
                    _availablePool.Dequeue(); // Remove the newly enqueued instance to use it now
                }
                else
                {
                    // Recycle oldest active projectile if hard cap is reached
                    if (_activeProjectiles.Count > 0)
                    {
                        proj = _activeProjectiles[0];
                        _activeProjectiles.RemoveAt(0);
                    }
                    else
                    {
                        proj = CreateNewInstance();
                        _availablePool.Dequeue();
                    }
                }
            }

            if (!_activeProjectiles.Contains(proj))
            {
                _activeProjectiles.Add(proj);
            }

            proj.transform.position = position;
            proj.transform.forward = direction.normalized;
            proj.gameObject.SetActive(true);
            proj.Launch(shooter, direction, color, damage, speed, this);

            return proj;
        }

        /// <summary>
        /// Recycles a spent projectile back into the pool with zero memory allocations.
        /// </summary>
        public void ReturnToPool(CombatProjectile projectile)
        {
            if (projectile == null) return;

            _activeProjectiles.Remove(projectile);
            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(_poolRoot, false);

            if (!_availablePool.Contains(projectile))
            {
                _availablePool.Enqueue(projectile);
            }
        }

        /// <summary>
        /// Recycles all currently active projectiles back into the pool.
        /// Useful during session resets or wave transitions.
        /// </summary>
        public void ReturnAll()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                var proj = _activeProjectiles[i];
                if (proj != null)
                {
                    ReturnToPool(proj);
                }
            }
            _activeProjectiles.Clear();
        }
    }
}
