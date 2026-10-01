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
        [SerializeField] private Vector3 arenaCenter = new Vector3(20f, 0.5f, 30f);
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
        /// Batas rumah: X: [9.0, 32.0], Z: [4.0, 27.0].
        /// </summary>
        public static bool IsInsideHouse(Vector3 pos)
        {
            return pos.x >= 9.0f && pos.x <= 32.0f && pos.z >= 4.0f && pos.z <= 27.0f;
        }

        /// <summary>
        /// Menghasilkan titik outdoor terdekat di luar perimeter rumah dengan margin aman.
        /// Digunakan untuk mengusir monster keluar atau membatasi pergerakan skill agar tidak menembus rumah.
        /// </summary>
        public static Vector3 GetNearestOutdoorPosition(Vector3 pos, float margin = 1.5f)
        {
            float minX = 9.0f - margin;
            float maxX = 32.0f + margin;
            float minZ = 4.0f - margin;
            float maxZ = 27.0f + margin;

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
        /// Menghasilkan titik spawn acak yang dijamin 100% berada di luar rumah (outdoor).
        /// Memilih dari 4 sektor outdoor di sekitar kebun dan pekarangan, lalu memproyeksikannya ke permukaan tanah.
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
                    case 0: // Sektor Utara: Pekarangan & Perkebunan Jagung/Ubi (Z: 33 s.d. 44, X: 12 s.d. 30)
                        candidate = new Vector3(UnityEngine.Random.Range(12f, 30f), 10f, UnityEngine.Random.Range(33f, 44f));
                        break;
                    case 1: // Sektor Timur: Rimba Liar (X: 34 s.d. 44, Z: 10 s.d. 32)
                        candidate = new Vector3(UnityEngine.Random.Range(34f, 44f), 10f, UnityEngine.Random.Range(10f, 32f));
                        break;
                    case 2: // Sektor Barat: Kebun Buah Luar Garasi (X: 2 s.d. 8, Z: 12 s.d. 28)
                        candidate = new Vector3(UnityEngine.Random.Range(2f, 8f), 10f, UnityEngine.Random.Range(12f, 28f));
                        break;
                    case 3: // Sektor Selatan: Hutan Belakang (X: 12 s.d. 30, Z: -2 s.d. 3.5)
                        candidate = new Vector3(UnityEngine.Random.Range(12f, 30f), 10f, UnityEngine.Random.Range(-2f, 3.5f));
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
                candidate = new Vector3(21.5f, 10f, 38f); // Jalur utara perkebunan
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
            }
        }
    }
}
