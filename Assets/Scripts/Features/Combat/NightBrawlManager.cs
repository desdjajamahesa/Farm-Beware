using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        private bool isWaveInProgress = false;
        private Coroutine waveLoopCoroutine;

        public event System.Action<EnemyBase> OnEnemySpawned;
        public IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;
        public int ActiveEnemiesCount => activeEnemies.Count;
        public bool IsNightBrawlActive => isWaveInProgress;

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
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
            EnemyBase.OnAnyEnemyDied -= HandleEnemyDied;
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

            // Day 1 -> 1 Wave, Day 2 -> 2 Waves ... Day 5 -> 5 Waves
            totalWaves = Mathf.Clamp(currentDay, 1, 5);
            currentWave = 0;
            isWaveInProgress = true;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.isNightEncounterCleared = false;
            }

            if (CombatPhaseTrackerUI.Instance != null)
            {
                CombatPhaseTrackerUI.Instance.SetWaveInfo(1, totalWaves);
            }

            if (waveLoopCoroutine != null)
                StopCoroutine(waveLoopCoroutine);

            waveLoopCoroutine = StartCoroutine(RoutineWaveLoop());
        }

        private IEnumerator RoutineWaveLoop()
        {
            // Beri jeda 2 detik saat malam tiba agar suasana mencekam terasa
            yield return new WaitForSeconds(2.5f);

            while (currentWave < totalWaves)
            {
                currentWave++;

                // Notifikasi wave dimulai
                if (FloatingCombatTextManager.Instance != null)
                {
                    var player = GameObject.FindWithTag("Player");
                    Vector3 notifPos = player != null ? player.transform.position + Vector3.up * 2f : arenaCenter;
                    FloatingCombatTextManager.Instance.SpawnText(
                        notifPos,
                        $"⚠️ WAVE {currentWave} / {totalWaves} INCOMING!",
                        new Color(1f, 0.25f, 0.25f));
                }

                if (CombatPhaseTrackerUI.Instance != null)
                {
                    CombatPhaseTrackerUI.Instance.SetWaveInfo(currentWave, totalWaves);
                }

                // Spawn musuh untuk wave saat ini
                SpawnEnemiesForWave(currentDay, currentWave);

                if (CombatPhaseTrackerUI.Instance != null)
                {
                    CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(activeEnemies.Count);
                }

                // Tunggu sampai semua musuh di wave ini tereliminasi
                while (activeEnemies.Count > 0)
                {
                    // Bersihkan null references jika ada musuh yang hancur mendadak
                    activeEnemies.RemoveAll(e => e == null || e.IsDead);
                    if (CombatPhaseTrackerUI.Instance != null)
                    {
                        CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(activeEnemies.Count);
                    }
                    yield return new WaitForSeconds(0.5f);
                }

                // Wave selesai
                if (currentWave < totalWaves)
                {
                    if (FloatingCombatTextManager.Instance != null)
                    {
                        var player = GameObject.FindWithTag("Player");
                        Vector3 notifPos = player != null ? player.transform.position + Vector3.up * 2f : arenaCenter;
                        FloatingCombatTextManager.Instance.SpawnText(
                            notifPos,
                            $"✨ Wave {currentWave} Cleared! Catch your breath...",
                            new Color(0.4f, 0.9f, 0.4f));
                    }
                    yield return new WaitForSeconds(waveIntermissionDelay);
                }
            }

            // Seluruh wave malam ini selesai!
            OnAllWavesCleared();
        }

        private void SpawnEnemiesForWave(int day, int wave)
        {
            activeEnemies.Clear();
            List<EnemyType> toSpawn = GetEnemiesListForDayAndWave(day, wave);

            foreach (var type in toSpawn)
            {
                Vector3 spawnPos = CalculateRandomSpawnPoint();
                GameObject enemyObj = EnemyPrefabFactory.CreateEnemy(type, spawnPos);
                EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                    OnEnemySpawned?.Invoke(enemy);
                }
            }

            Debug.Log($"[NightBrawlManager] Day {day} Wave {wave} dimulai: {activeEnemies.Count} musuh dibangkitkan.");
        }

        private List<EnemyType> GetEnemiesListForDayAndWave(int day, int wave)
        {
            var list = new List<EnemyType>();

            if (day == 1)
            {
                // Day 1: 1 wave
                list.Add(EnemyType.TuberMaw);
                list.Add(EnemyType.TuberMaw);
                list.Add(EnemyType.TaroBrute);
            }
            else if (day == 2)
            {
                if (wave == 1)
                {
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.CornMusketeer);
                }
                else
                {
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.CornMusketeer);
                }
            }
            else if (day == 3)
            {
                if (wave == 1)
                {
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TuberMaw);
                }
                else if (wave == 2)
                {
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.TuberMaw);
                }
                else
                {
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.CyclopsTuberMaw);
                }
            }
            else if (day == 4)
            {
                if (wave <= 2)
                {
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.TuberMaw);
                }
                else if (wave == 3)
                {
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.TaroBrute);
                }
                else
                {
                    list.Add(EnemyType.TaroColossus);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                    list.Add(EnemyType.TuberMaw);
                }
            }
            else
            {
                // Day 5: 5 Waves
                if (wave <= 3)
                {
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                    if (wave >= 2) list.Add(EnemyType.TaroBrute);
                    if (wave >= 3) list.Add(EnemyType.CornMusketeer);
                }
                else if (wave == 4)
                {
                    // Boss Encounter 1: Taro Colossus + 3 Normal
                    list.Add(EnemyType.TaroColossus);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.CornMusketeer);
                }
                else
                {
                    // Wave 5 Climax: 2 Bosses Simultaneously + 3 Normal Enemies
                    list.Add(EnemyType.CyclopsTuberMaw);
                    list.Add(EnemyType.TheRanger);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                }
            }

            return list;
        }

        /// <summary>
        /// Mengecek apakah sebuah koordinat dunia berada di dalam interior rumah.
        /// Batas rumah: X: [9.5, 31.5], Z: [4.5, 26.5].
        /// </summary>
        public static bool IsInsideHouse(Vector3 pos)
        {
            return pos.x >= 9.0f && pos.x <= 32.0f && pos.z >= 4.0f && pos.z <= 27.0f;
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
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 30f))
            {
                return hit.point + Vector3.up * 0.1f;
            }

            candidate.y = 0.5f;
            return candidate;
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            activeEnemies.Remove(enemy);

            if (CombatPhaseTrackerUI.Instance != null)
            {
                CombatPhaseTrackerUI.Instance.SetEnemiesRemaining(activeEnemies.Count);
            }
        }

        private void OnAllWavesCleared()
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

            Debug.Log("[NightBrawlManager] Seluruh gelombang malam berhasil dituntaskan! Kasur siap untuk transisi hari berikutnya.");
        }

        public void EndNightBrawl(bool cleanupRemaining)
        {
            if (waveLoopCoroutine != null)
            {
                StopCoroutine(waveLoopCoroutine);
                waveLoopCoroutine = null;
            }

            isWaveInProgress = false;

            if (cleanupRemaining)
            {
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null)
                    {
                        Destroy(enemy.gameObject);
                    }
                }
                activeEnemies.Clear();
            }
        }
    }
}
