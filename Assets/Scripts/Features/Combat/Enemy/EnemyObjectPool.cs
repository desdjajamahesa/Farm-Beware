using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCombat
{
    /// <summary>
    /// Manager Object Pooling terpusat untuk 6 varian monster di Night Brawl.
    /// Mengeliminasi lag spike dan garbage collection runtime (Zero-Allocation)
    /// dengan mendaur ulang entitas monster yang mati alih-alih Destroy/Instantiate berulang kali.
    /// </summary>
    public class EnemyObjectPool : MonoBehaviour
    {
        public static EnemyObjectPool Instance { get; private set; }

        [Header("Pool Prewarm Configuration")]
        [Tooltip("Jumlah instansiasi awal untuk monster normal di setiap tipe.")]
        [SerializeField] private int prewarmNormalCount = 4;
        [Tooltip("Jumlah instansiasi awal untuk boss di setiap tipe.")]
        [SerializeField] private int prewarmBossCount = 2;

        [Header("Enemy Data Assets (Modular Stats & Evolution)")]
        [SerializeField] private List<FeaturesCombat.Data.EnemyData> registeredEnemyData = new List<FeaturesCombat.Data.EnemyData>();

        private readonly Dictionary<EnemyType, Queue<EnemyBase>> pools = new Dictionary<EnemyType, Queue<EnemyBase>>();
        private readonly Dictionary<EnemyType, GameObject> prefabCache = new Dictionary<EnemyType, GameObject>();
        private readonly Dictionary<EnemyType, FeaturesCombat.Data.EnemyData> enemyDataCache = new Dictionary<EnemyType, FeaturesCombat.Data.EnemyData>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            poolRoot = new GameObject("PooledEnemies_Root").transform;
            poolRoot.SetParent(transform);

            InitializePools();
        }

        private void InitializePools()
        {
            // Cache registered enemy data
            foreach (var data in registeredEnemyData)
            {
                if (data != null)
                {
                    enemyDataCache[data.enemyType] = data;
                }
            }

#if UNITY_EDITOR
            if (enemyDataCache.Count == 0)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyData");
                foreach (var g in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<FeaturesCombat.Data.EnemyData>(path);
                    if (asset != null)
                    {
                        enemyDataCache[asset.enemyType] = asset;
                    }
                }
            }
#endif

            // Inisialisasi antrean pool untuk ke-6 varian
            foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
            {
                if (!pools.ContainsKey(type))
                {
                    pools[type] = new Queue<EnemyBase>();
                }
                LoadPrefabForType(type);
            }

            // Prewarm instansiasi di awal game saat memori masih tenang
            PrewarmAll();
        }

        private void LoadPrefabForType(EnemyType type)
        {
            string prefabName = GetPrefabResourceName(type);
            GameObject prefab = Resources.Load<GameObject>($"Enemies/{prefabName}");
            if (prefab != null)
            {
                prefabCache[type] = prefab;
            }
            else
            {
                Debug.LogWarning($"[EnemyObjectPool] Prefab Resources/Enemies/{prefabName} tidak ditemukan. Fallback ke EnemyPrefabFactory.");
            }
        }

        private string GetPrefabResourceName(EnemyType type)
        {
            return type switch
            {
                EnemyType.TuberMaw => "Enemy_TuberMaw",
                EnemyType.CyclopsTuberMaw => "Boss_CyclopsTuberMaw",
                EnemyType.TaroBrute => "Enemy_TaroBrute",
                EnemyType.TaroColossus => "Boss_TaroColossus",
                EnemyType.CornMusketeer => "Enemy_CornMusketeer",
                EnemyType.TheRanger => "Boss_TheRanger",
                _ => "Enemy_TuberMaw"
            };
        }

        private void PrewarmAll()
        {
            foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
            {
                bool isBoss = type == EnemyType.CyclopsTuberMaw || type == EnemyType.TaroColossus || type == EnemyType.TheRanger;
                int count = isBoss ? prewarmBossCount : prewarmNormalCount;

                for (int i = 0; i < count; i++)
                {
                    EnemyBase enemy = CreateNewInstance(type);
                    if (enemy != null)
                    {
                        enemy.gameObject.SetActive(false);
                        pools[type].Enqueue(enemy);
                    }
                }
            }
        }

        private EnemyBase CreateNewInstance(EnemyType type)
        {
            GameObject obj;
            if (prefabCache.TryGetValue(type, out GameObject prefab) && prefab != null)
            {
                obj = Instantiate(prefab, poolRoot);
            }
            else
            {
                // Fallback runtime factory
                obj = EnemyPrefabFactory.CreateEnemy(type, Vector3.zero);
                obj.transform.SetParent(poolRoot);
            }

            var enemy = obj.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                enemy = obj.AddComponent<EnemyBase>();
                enemy.enemyType = type;
            }

            if (enemyDataCache.TryGetValue(type, out var data))
            {
                enemy.SetEnemyData(data);
            }
            else
            {
                enemy.InitializeStatsByType();
            }

            return enemy;
        }

        /// <summary>
        /// Meminjam monster dari pool atau menginstansiasi baru jika pool kosong.
        /// </summary>
        public EnemyBase Spawn(EnemyType type, Vector3 position)
        {
            if (!pools.TryGetValue(type, out var queue))
            {
                queue = new Queue<EnemyBase>();
                pools[type] = queue;
            }

            EnemyBase enemy = null;

            // Bersihkan objek hancur jika ada
            while (queue.Count > 0 && enemy == null)
            {
                enemy = queue.Dequeue();
            }

            if (enemy == null)
            {
                enemy = CreateNewInstance(type);
            }

            if (enemy != null)
            {
                enemy.transform.SetParent(null); // Lepas dari root saat aktif
                enemy.transform.position = position;
                enemy.gameObject.SetActive(true);
                enemy.ResetEnemyState();
            }

            return enemy;
        }

        /// <summary>
        /// Mengembalikan monster yang telah tereliminasi kembali ke pool (Zero GC allocation).
        /// </summary>
        public void ReturnToPool(EnemyBase enemy)
        {
            if (enemy == null) return;

            enemy.gameObject.SetActive(false);
            if (poolRoot != null)
            {
                enemy.transform.SetParent(poolRoot);
            }

            if (!pools.TryGetValue(enemy.enemyType, out var queue))
            {
                queue = new Queue<EnemyBase>();
                pools[enemy.enemyType] = queue;
            }

            queue.Enqueue(enemy);
        }
    }
}
