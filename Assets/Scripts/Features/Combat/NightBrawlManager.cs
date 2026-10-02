using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PlayerUI;
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

        [Header("Arena Center & Spawn Bounds")]
        [SerializeField] private Vector3 arenaCenter = new Vector3(21f, 0.5f, 36f);
#pragma warning disable 0414
        [SerializeField] private float spawnRadiusMin = 8f;
        [SerializeField] private float spawnRadiusMax = 15f;
#pragma warning restore 0414

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
        /// Batas pagar: X: [4.0, 38.0], Z: [2.0, 48.0].
        /// </summary>
        public static bool IsInsideCompound(Vector3 pos)
        {
            return pos.x >= 4.0f && pos.x <= 38.0f && pos.z >= 2.0f && pos.z <= 48.0f;
        }

        /// <summary>
        /// Menghasilkan titik spawn acak yang dijamin 100% berada di luar rumah (outdoor).
        /// Terdistribusi di 4 sektor pekarangan dan luar gerbang:
        /// 0: Gelombang penyerbu dari luar gerbang utama utara (Z: 49-56, X: 17-25)
        /// 1: Pekarangan timur terbuka dekat sumur (X: 32-36.5, Z: 32-44)
        /// 2: Lorong pekarangan barat antara pagar dan kebun/api unggun (X: 5.5-7.5, Z: 32-42)
        /// 3: Plaza halaman selatan depan teras rumah (X: 18-24, Z: 29.5-33.5)
        /// </summary>
        public Vector3 CalculateRandomSpawnPoint()
        {
            Vector3 candidate = Vector3.zero;
            int attempts = 0;

            while (attempts < 20)
            {
                attempts++;
                int sector = UnityEngine.Random.Range(0, 4);
                switch (sector)
                {
                    case 0: // Sektor Utara Luar: Menyerbu masuk melalui Gerbang Utama (Z: 49 s.d. 56, X: 17 s.d. 25)
                        candidate = new Vector3(UnityEngine.Random.Range(17f, 25f), 10f, UnityEngine.Random.Range(49f, 56f));
                        break;
                    case 1: // Sektor Timur: Pekarangan terbuka di timur sumur (X: 32f s.d. 36.5f, Z: 32f s.d. 44f)
                        candidate = new Vector3(UnityEngine.Random.Range(32f, 36.5f), 10f, UnityEngine.Random.Range(32f, 44f));
                        break;
                    case 2: // Sektor Barat: Lorong barat dekat pagar (X: 5.5f s.d. 7.5f, Z: 32f s.d. 42f)
                        candidate = new Vector3(UnityEngine.Random.Range(5.5f, 7.5f), 10f, UnityEngine.Random.Range(32f, 42f));
                        break;
                    case 3: // Sektor Selatan: Plaza halaman depan teras (X: 18f s.d. 24f, Z: 29.5f s.d. 33.5f)
                        candidate = new Vector3(UnityEngine.Random.Range(18f, 24f), 10f, UnityEngine.Random.Range(29.5f, 33.5f));
                        break;
                }

                if (!IsInsideHouse(candidate))
                {
                    break;
                }
            }

            // Fallback deterministik jika anomali
            if (IsInsideHouse(candidate))
            {
                candidate = new Vector3(21.0f, 10f, 36.0f); // Titik tengah pekarangan depan
            }

            // Raycast ke tanah agar menempel tepat di permukaan terrain
            Vector3 finalPos = candidate;
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 30f))
            {
                finalPos = hit.point + Vector3.up * 0.1f;
            }
            else
            {
                finalPos.y = 0.5f;
            }

            // Snap presisi ke NavMesh walkable area terdekat
            if (NavMesh.SamplePosition(finalPos, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            return finalPos;
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            activeEnemies.Remove(enemy);
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

            currentDay = day;
            totalWaves = total > 0 ? total : Mathf.Clamp(currentDay, 1, 5);
            currentWave = wave;

            bool isDay = TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day;
            bool hasSavedEnemies = (savedEnemies != null && savedEnemies.Count > 0);

            // Check if night encounter is cleared: explicitly cleared, or night brawl inactive with no enemies, or final wave completed with no enemies
            bool isEncounterDone = isCleared || (!isBrawlActive && !hasSavedEnemies) || (currentWave >= totalWaves && !hasSavedEnemies);

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

                var beds = FindObjectsByType<FeaturesInteraction.BedInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in beds)
                {
                    b?.UpdateLabel();
                }
                return;
            }

            // Scenario 2: It is Night and active
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

            var bedsAfter = FindObjectsByType<FeaturesInteraction.BedInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in bedsAfter)
            {
                b?.UpdateLabel();
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
    }
}
