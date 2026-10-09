using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using FarmBeware.Core.Runtime;
using FeaturesTime.UI;

namespace FeaturesCombat
{
    /// <summary>
    /// Manager pengontrol gelombang pertempuran malam hari (Night Brawl).
    /// Mengatur siklus gelombang 5 hari sesuai spesifikasi dokumen MVP:
    /// Day 1 (1 Wave) s.d. Day 5 (5 Waves dengan klimaks 2 Boss sekaligus).
    /// </summary>
    public class NightBrawlManager : MonoBehaviour
    {
        public static NightBrawlManager Instance { get; private set; }

        public FeaturesCombat.Core.PureLogic.AttackTokenDispatcher TokenDispatcher { get; private set; } = new FeaturesCombat.Core.PureLogic.AttackTokenDispatcher();

        [Header("Arena Center & Spawn Bounds")]
        [SerializeField] private Vector3 arenaCenter = new Vector3(21f, 0.5f, 36f);

        [Header("Front Gate Spawn Points")]
        [Tooltip("Spawn points located at the front entrance gate area.")]
        [SerializeField] private Vector3[] frontGateSpawnPoints = new Vector3[3]
        {
            new Vector3(37.4f, 0.08f, 64.2f), // Point 1: Left
            new Vector3(21.9f, 0.08f, 64.6f), // Point 2: Center
            new Vector3(9.6f, 0.08f, 64.5f)   // Point 3: Right
        };

        [Tooltip("Optional transform anchors in the scene. If assigned or found under MonsterSpawnPoints, their positions will override frontGateSpawnPoints.")]
        [SerializeField] private Transform[] frontGateSpawnTransforms;

        [Tooltip("Horizontal scatter radius around each spawn point to prevent overlapping spawns.")]
        [SerializeField] private float spawnScatterRadius = 1.0f;

        public IReadOnlyList<Vector3> FrontGateSpawnPoints => frontGateSpawnPoints;
        public Transform[] FrontGateSpawnTransforms => frontGateSpawnTransforms;
        public float SpawnScatterRadius => spawnScatterRadius;

        /// <summary>
        /// Mengumpulkan seluruh titik spawn aktif dari MonsterSpawnPoints (dan children-nya) atau frontGateSpawnTransforms.
        /// Menghormati 100% posisi yang ditentukan user di scene.
        /// </summary>
        public List<Vector3> GetAllActiveSpawnPoints()
        {
            var points = new List<Vector3>();

            // 1. Cek dari GameObject MonsterSpawnPoints di hierarki scene
            var mspObj = GameObject.Find("MonsterSpawnPoints");
            if (mspObj != null)
            {
                if (mspObj.transform.childCount > 0)
                {
                    for (int i = 0; i < mspObj.transform.childCount; i++)
                    {
                        var child = mspObj.transform.GetChild(i);
                        if (child != null && child.gameObject.activeInHierarchy)
                        {
                            points.Add(child.position);
                        }
                    }
                }
                else
                {
                    points.Add(mspObj.transform.position);
                }
            }

            // 2. Jika tidak ada / kosong, cek frontGateSpawnTransforms yang di-assign di inspector
            if (points.Count == 0 && frontGateSpawnTransforms != null && frontGateSpawnTransforms.Length > 0)
            {
                for (int i = 0; i < frontGateSpawnTransforms.Length; i++)
                {
                    if (frontGateSpawnTransforms[i] != null)
                    {
                        points.Add(frontGateSpawnTransforms[i].position);
                    }
                }
            }

            // 3. Fallback default jika masih kosong
            if (points.Count == 0)
            {
                if (frontGateSpawnPoints != null && frontGateSpawnPoints.Length > 0)
                {
                    points.AddRange(frontGateSpawnPoints);
                }
                else
                {
                    points.Add(new Vector3(37.4f, 0.08f, 64.2f));
                    points.Add(new Vector3(21.9f, 0.08f, 64.6f));
                    points.Add(new Vector3(9.6f, 0.08f, 64.5f));
                }
            }

            return points;
        }

        public Vector3 GetSpawnPoint(int index)
        {
            var points = GetAllActiveSpawnPoints();
            if (points != null && points.Count > 0)
            {
                int safeIndex = Mathf.Clamp(index, 0, points.Count - 1);
                return points[safeIndex];
            }
            return new Vector3(21.9f, 0.08f, 64.6f);
        }

        [Header("Wave Progress")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int currentWave = 0;
        [SerializeField] private int totalWaves = 1;
        [SerializeField] private float waveIntermissionDelay = 4f;

        private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();
        private readonly Wave.WaveProgressionEngine waveEngine = new Wave.WaveProgressionEngine();
        private bool isWaveInProgress = false;
        private Coroutine intermissionCoroutine;

        public event System.Action<EnemyBase> OnEnemySpawned;
        public event System.Action<EnemyBase> OnEnemyDied;
        public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;
        public int ActiveEnemiesCount => activeEnemies.Count;
        public bool IsNightBrawlActive => isWaveInProgress;
        public Wave.WaveProgressionEngine WaveEngine => waveEngine;
        public int CurrentWave => currentWave;
        public int TotalWaves => totalWaves;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            EnemyBase.OnAnyEnemyDied += HandleEnemyDied;

            waveEngine.OnWaveStarted += HandleWaveStarted;
            waveEngine.OnWaveCompleted += HandleWaveCompleted;
            waveEngine.OnAllWavesCleared += HandleAllWavesCleared;
            waveEngine.OnEnemiesRemainingChanged += HandleEnemiesRemainingChanged;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
            EnemyBase.OnAnyEnemyDied -= HandleEnemyDied;

            waveEngine.OnWaveStarted -= HandleWaveStarted;
            waveEngine.OnWaveCompleted -= HandleWaveCompleted;
            waveEngine.OnAllWavesCleared -= HandleAllWavesCleared;
            waveEngine.OnEnemiesRemainingChanged -= HandleEnemiesRemainingChanged;
        }

        private void Start()
        {
            EnsureHealthBarManagers();

            if (TimeManager.Instance != null)
            {
                currentDay = TimeManager.Instance.currentDay;
                if (TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
                {
                    StartNightBrawl();
                }
            }
        }

        private void HandleDayChanged(int day)
        {
            currentDay = day;
        }

        private void HandlePhaseChanged(TimeManager.DayPhase phase)
        {
            if (phase == TimeManager.DayPhase.Night)
            {
                StartNightBrawl();
            }
            else
            {
                EndNightBrawl(cleanupRemaining: true);
            }
        }

        public void StartNightBrawl()
        {
            if (isWaveInProgress) return;

            EnsureHealthBarManagers();

            isWaveInProgress = true;
            waveEngine.StartNight(currentDay);
            totalWaves = waveEngine.TotalWavesForDay;
            currentWave = 0;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.isNightEncounterCleared = false;
            }

            if (CombatPhaseTrackerUI.Instance != null)
            {
                CombatPhaseTrackerUI.Instance.SetWaveInfo(1, totalWaves);
            }

            if (intermissionCoroutine != null)
            {
                StopCoroutine(intermissionCoroutine);
                intermissionCoroutine = null;
            }

            intermissionCoroutine = StartCoroutine(RoutineInitialSuspenseDelay());
        }

        private IEnumerator RoutineInitialSuspenseDelay()
        {
            yield return new WaitForSeconds(2.5f);
            SpawnNextWave();
            intermissionCoroutine = null;
        }

        private void SpawnNextWave()
        {
            activeEnemies.Clear();
            List<EnemyType> toSpawn = waveEngine.PrepareNextWave();
            currentWave = waveEngine.CurrentWaveIndex;

            foreach (var type in toSpawn)
            {
                Vector3 spawnPos = CalculateRandomSpawnPoint();
                EnemyBase enemy = EnemyObjectPool.Instance != null
                    ? EnemyObjectPool.Instance.Spawn(type, spawnPos)
                    : EnemyPrefabFactory.CreateEnemy(type, spawnPos).GetComponent<EnemyBase>();

                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                    OnEnemySpawned?.Invoke(enemy);
                }
            }

            Debug.Log($"[NightBrawlManager] Day {currentDay} Wave {currentWave} spawned: {activeEnemies.Count} enemies.");
        }

        private void HandleWaveStarted(int day, int wave)
        {
            if (FloatingCombatTextManager.Instance != null)
            {
                var player = GameObject.FindWithTag("Player");
                Vector3 notifPos = player != null ? player.transform.position + Vector3.up * 2f : arenaCenter;
                FloatingCombatTextManager.Instance.SpawnText(
                    notifPos,
                    $"⚠️ WAVE {wave} / {totalWaves} INCOMING!",
                    new Color(1f, 0.25f, 0.25f));
            }

            if (CombatPhaseTrackerUI.Instance != null)
            {
                CombatPhaseTrackerUI.Instance.SetWaveInfo(wave, totalWaves);
            }
        }

        private void HandleEnemiesRemainingChanged(int count)
        {
            if (CombatPhaseTrackerUI.Instance != null)
            {
                CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(count);
            }
        }

        private void HandleWaveCompleted(int day, int wave)
        {
            if (waveEngine.HasMoreWaves)
            {
                if (FloatingCombatTextManager.Instance != null)
                {
                    var player = GameObject.FindWithTag("Player");
                    Vector3 notifPos = player != null ? player.transform.position + Vector3.up * 2f : arenaCenter;
                    FloatingCombatTextManager.Instance.SpawnText(
                        notifPos,
                        $"✨ Wave {wave} Cleared! Catch your breath...",
                        new Color(0.4f, 0.9f, 0.4f));
                }

                if (intermissionCoroutine != null)
                    StopCoroutine(intermissionCoroutine);

                intermissionCoroutine = StartCoroutine(RoutineIntermissionCountdown());
            }
        }

        private IEnumerator RoutineIntermissionCountdown()
        {
            yield return new WaitForSeconds(waveIntermissionDelay);
            SpawnNextWave();
            intermissionCoroutine = null;
        }

        /// <summary>
        /// Mendaftarkan musuh baru hasil pemanggilan (summon) bos ke dalam daftar musuh aktif wave saat ini.
        /// </summary>
        public void RegisterDynamicEnemy(EnemyBase enemy)
        {
            if (enemy == null || activeEnemies.Contains(enemy)) return;
            activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
            waveEngine.RegisterDynamicEnemy();
        }

        private List<EnemyType> GetEnemiesListForDayAndWave(int day, int wave)
        {
            return Wave.WaveProgressionEngine.GetEnemiesScheduleForDayAndWave(day, wave);
        }

        /// <summary>
        /// Mengecek apakah sebuah koordinat dunia berada di dalam interior rumah.
        /// Batas rumah: X: [9.5, 31.5], Z: [4.5, 26.5].
        /// </summary>
        public static bool IsInsideHouse(Vector3 pos)
        {
            return pos.x >= 9.5f && pos.x <= 31.5f && pos.z >= 4.5f && pos.z <= 26.5f;
        }

        /// <summary>
        /// Menghasilkan titik outdoor terdekat di luar perimeter rumah dengan margin aman.
        /// Digunakan untuk mengusir monster keluar atau membatasi pergerakan skill agar tidak menembus rumah.
        /// </summary>
        public static Vector3 GetNearestOutdoorPosition(Vector3 pos, float margin = 1.5f)
        {
            float minX = 9.5f - margin;
            float maxX = 31.5f + margin;
            float minZ = 4.5f - margin;
            float maxZ = 26.5f + margin;

            float distNorth = Mathf.Abs(maxZ - pos.z);
            float distSouth = Mathf.Abs(pos.z - minZ);
            float distWest = Mathf.Abs(pos.x - minX);
            float distEast = Mathf.Abs(maxX - pos.x);

            float minDist = Mathf.Min(distNorth, Mathf.Min(distSouth, Mathf.Min(distWest, distEast)));

            Vector3 outdoor = pos;
            if (Mathf.Approximately(minDist, distNorth)) outdoor.z = maxZ;
            else if (Mathf.Approximately(minDist, distSouth)) outdoor.z = minZ;
            else if (Mathf.Approximately(minDist, distWest)) outdoor.x = minX;
            else outdoor.x = maxX;

            outdoor.y = Mathf.Max(0.1f, pos.y);
            return outdoor;
        }

        /// <summary>
        /// Mengecek apakah sebuah koordinat dunia berada di dalam batas kompleks pekarangan (Homestead Perimeter).
        /// Batas pagar baru (42m x 65m): X: [0.0, 42.0], Z: [-17.0, 48.0].
        /// </summary>
        public static bool IsInsideCompound(Vector3 pos)
        {
            return pos.x >= 0.0f && pos.x <= 42.0f && pos.z >= -17.0f && pos.z <= 48.0f;
        }

        /// <summary>
        /// Menghasilkan posisi spawn acak dari salah satu spawn point yang ditentukan (MonsterSpawnPoints dan children-nya).
        /// Menerapkan scatter radius ringan dan memastikan posisi berada di atas permukaan tanah (ground raycast & NavMesh).
        /// </summary>
        public Vector3 CalculateRandomSpawnPoint()
        {
            var points = GetAllActiveSpawnPoints();
            if (points == null || points.Count == 0)
            {
                return new Vector3(21.9f, 0.08f, 64.6f);
            }

            int pointIndex = UnityEngine.Random.Range(0, points.Count);
            Vector3 basePoint = points[pointIndex];

            // Sedikit scatter offset agar monster yang spawn serempak tidak menumpuk di titik yang sama persis
            Vector2 scatter = UnityEngine.Random.insideUnitCircle * spawnScatterRadius;
            Vector3 candidate = basePoint + new Vector3(scatter.x, 0f, scatter.y);
            candidate.y += 10f; // Elevate for ground raycast

            Vector3 finalPos = candidate;
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 30f, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            {
                finalPos = hit.point + Vector3.up * 0.05f;
            }
            else
            {
                finalPos.y = basePoint.y;
            }

            // Snap ke NavMesh walkable terdekat di sekitar titik spawn
            if (NavMesh.SamplePosition(finalPos, out NavMeshHit navHit, 3.5f, NavMesh.AllAreas))
            {
                finalPos = navHit.position;
            }

            // Jaga agar scatter/navmesh tidak memindahkan monster lebih dari 2.5m dari titik spawn yang ditentukan user
            float distFromBase = Vector2.Distance(new Vector2(finalPos.x, finalPos.z), new Vector2(basePoint.x, basePoint.z));
            if (distFromBase > 2.5f)
            {
                finalPos.x = basePoint.x + scatter.x;
                finalPos.z = basePoint.z + scatter.y;
            }

            return finalPos;
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            activeEnemies.Remove(enemy);
            OnEnemyDied?.Invoke(enemy);
            waveEngine.RecordEnemyDefeated();
        }

        private void HandleAllWavesCleared(int day)
        {
            isWaveInProgress = false;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.isNightEncounterCleared = true;
            }

            if (FloatingCombatTextManager.Instance != null)
            {
                var player = GameObject.FindWithTag("Player");
                Vector3 notifPos = player != null ? player.transform.position + Vector3.up * 2.2f : arenaCenter;
                FloatingCombatTextManager.Instance.SpawnText(
                    notifPos,
                    "🏆 NIGHT BRAWL CLEARED! Sleep in bed to start the next day.",
                    new Color(0.2f, 0.95f, 0.4f));
            }

            if (CombatPhaseTrackerUI.Instance != null)
            {
                CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(0);
            }

            Debug.Log("[NightBrawlManager] All night brawl waves cleared! Next day transition ready.");
        }

        /// <summary>
        /// Restores complete night brawl state from save data.
        /// </summary>
        public void RestoreNightBrawlState(int day, int wave, int total, bool isCleared, bool isBrawlActive, List<FeaturesSaveSystem.SavedEnemyData> savedEnemies)
        {
            if (intermissionCoroutine != null)
            {
                StopCoroutine(intermissionCoroutine);
                intermissionCoroutine = null;
            }

            // Cleanup any existing active enemies
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    if (EnemyObjectPool.Instance != null) EnemyObjectPool.Instance.ReturnToPool(enemy);
                    else Destroy(enemy.gameObject);
                }
            }
            activeEnemies.Clear();

            // Also clean up any orphan EnemyBase in scene
            var allEnemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in allEnemies)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }

            // Also clean up stray projectiles and dropped item pickups from previous sessions
            var projectiles = FindObjectsByType<Projectiles.CombatProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in projectiles)
            {
                if (p != null) Destroy(p.gameObject);
            }

            var pickups = FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pi in pickups)
            {
                if (pi != null) Destroy(pi.gameObject);
            }

            // Also clean up UI overhead health bars
            UI.EnemyHealthBarManager.Instance?.ReleaseAllBars();
            UI.BossHealthBarManager.Instance?.ClearAllBosses();

            currentDay = day;
            totalWaves = total > 0 ? total : Mathf.Clamp(currentDay, 1, 5);
            currentWave = wave;

            bool isDay = TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day;
            bool hasSavedEnemies = (savedEnemies != null && savedEnemies.Count > 0);

            // Check if night encounter is cleared: explicitly cleared, or final wave completed with no enemies
            bool isEncounterDone = isCleared || (currentWave >= totalWaves && !hasSavedEnemies && currentWave > 0);

            // Scenario 1: Night is cleared OR day phase
            if (isDay || isEncounterDone)
            {
                isWaveInProgress = false;
                waveEngine.EndNight();

                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.isNightEncounterCleared = isEncounterDone;
                }

                if (CombatPhaseTrackerUI.Instance != null)
                {
                    CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(0);
                    if (isEncounterDone)
                    {
                        CombatPhaseTrackerUI.Instance.SetWaveInfo(totalWaves, totalWaves);
                    }
                }

                var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in allBehaviours)
                {
                    if (b is FarmBeware.Core.Runtime.IBedInteractable bed)
                    {
                        bed.UpdateLabel();
                    }
                }
                return;
            }

            // Scenario 2: It is Night and encounter is active/not cleared
            isWaveInProgress = false; // Reset lock so wave spawning is never blocked

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.isNightEncounterCleared = false;
            }

            // Are there specific active saved enemies?
            if (hasSavedEnemies)
            {
                isWaveInProgress = true;
                currentWave = Mathf.Max(1, wave);

                waveEngine.RestoreNightState(currentDay, currentWave, savedEnemies.Count);

                foreach (var se in savedEnemies)
                {
                    Vector3 pos = new Vector3(se.posX, se.posY, se.posZ);
                    GameObject go = EnemyPrefabFactory.CreateEnemy((EnemyType)se.enemyType, pos);
                    go.transform.rotation = Quaternion.Euler(0f, se.rotY, 0f);

                    EnemyBase enemy = go.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.currentHealth = Mathf.Clamp(se.currentHealth, 1, se.maxHealth);
                        activeEnemies.Add(enemy);
                        OnEnemySpawned?.Invoke(enemy);
                    }
                }

                if (CombatPhaseTrackerUI.Instance != null)
                {
                    CombatPhaseTrackerUI.Instance.SetWaveInfo(currentWave, totalWaves);
                    CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(activeEnemies.Count);
                }
            }
            else if (isBrawlActive && currentWave > 0 && currentWave < totalWaves)
            {
                // Intermission between waves: resume countdown to next wave
                isWaveInProgress = true;
                waveEngine.RestoreNightState(currentDay, currentWave, 0);

                if (CombatPhaseTrackerUI.Instance != null)
                {
                    CombatPhaseTrackerUI.Instance.SetWaveInfo(currentWave, totalWaves);
                    CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(0);
                }

                intermissionCoroutine = StartCoroutine(RoutineIntermissionCountdown());
            }
            else
            {
                // Night just started from beginning
                StartNightBrawl();
            }

            var allBehavioursAfter = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allBehavioursAfter)
            {
                if (b is FarmBeware.Core.Runtime.IBedInteractable bed)
                {
                    bed.UpdateLabel();
                }
            }
        }

        public void EndNightBrawl(bool cleanupRemaining)
        {
            if (intermissionCoroutine != null)
            {
                StopCoroutine(intermissionCoroutine);
                intermissionCoroutine = null;
            }

            isWaveInProgress = false;
            waveEngine.EndNight();

            if (cleanupRemaining)
            {
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null)
                    {
                        if (EnemyObjectPool.Instance != null)
                        {
                            EnemyObjectPool.Instance.ReturnToPool(enemy);
                        }
                        else
                        {
                            Destroy(enemy.gameObject);
                        }
                    }
                }
                activeEnemies.Clear();

                var allEnemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var enemy in allEnemies)
                {
                    if (enemy != null)
                    {
                        Destroy(enemy.gameObject);
                    }
                }
            }
        }

        private void EnsureHealthBarManagers()
        {
            if (FeaturesCombat.UI.EnemyHealthBarManager.Instance == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    var mgrObj = new GameObject("EnemyHealthBarManager", typeof(FeaturesCombat.UI.EnemyHealthBarManager));
                    mgrObj.transform.SetParent(canvas.transform, false);
                }
            }

            if (FeaturesCombat.UI.BossHealthBarManager.Instance == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    var bossMgrObj = new GameObject("BossHealthBarManager", typeof(FeaturesCombat.UI.BossHealthBarManager));
                    bossMgrObj.transform.SetParent(canvas.transform, false);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.85f);
            var points = GetAllActiveSpawnPoints();
            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Gizmos.DrawWireSphere(points[i], spawnScatterRadius);
                    Gizmos.DrawSphere(points[i], 0.35f);
                }
            }
        }
    }
}
