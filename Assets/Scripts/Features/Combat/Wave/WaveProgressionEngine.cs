using System;
using System.Collections.Generic;

namespace FeaturesCombat.Wave
{
    /// <summary>
    /// Readonly struct encapsulating metadata for a wave completion milestone on the combat progress track.
    /// Zero-GC allocation.
    /// </summary>
    public readonly struct WaveMilestoneData
    {
        public readonly int WaveIndex;
        public readonly int EnemyQuota;
        public readonly int CumulativeThreshold;
        public readonly float NormalizedProgress;
        public readonly bool IsBossWave;
        public readonly string MilestoneTitle;

        public WaveMilestoneData(int waveIndex, int enemyQuota, int cumulativeThreshold, float normalizedProgress, bool isBossWave, string milestoneTitle)
        {
            WaveIndex = waveIndex;
            EnemyQuota = enemyQuota;
            CumulativeThreshold = cumulativeThreshold;
            NormalizedProgress = normalizedProgress;
            IsBossWave = isBossWave;
            MilestoneTitle = milestoneTitle;
        }
    }

    /// <summary>
    /// Readonly summary of the precomputed night wave progression and milestone layout.
    /// </summary>
    public readonly struct NightScheduleSummary
    {
        public readonly int Day;
        public readonly int TotalWaves;
        public readonly int TotalScheduledEnemies;
        public readonly WaveMilestoneData[] Milestones;

        public NightScheduleSummary(int day, int totalWaves, int totalScheduledEnemies, WaveMilestoneData[] milestones)
        {
            Day = day;
            TotalWaves = totalWaves;
            TotalScheduledEnemies = totalScheduledEnemies;
            Milestones = milestones ?? Array.Empty<WaveMilestoneData>();
        }
    }

    /// <summary>
    /// Pure C# POCO class governing the 5-day wave progression rules, enemy scheduling,
    /// and win condition tracking for the Night Brawl combat phase.
    /// Operates without MonoBehaviour lifecycle or per-frame polling.
    /// </summary>
    public class WaveProgressionEngine
    {
        private int _currentDay = 1;
        private int _currentWaveIndex = 0;
        private int _totalWavesForDay = 1;
        private int _activeEnemiesRemaining = 0;
        private bool _isNightActive = false;
        private int _totalScheduledEnemies = 0;
        private int _totalDefeatedEnemies = 0;
        private NightScheduleSummary _currentNightSummary;
        private int _lastReachedMilestoneIndex = -1;

        public int CurrentDay => _currentDay;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalWavesForDay => _totalWavesForDay;
        public int ActiveEnemiesRemaining => _activeEnemiesRemaining;
        public bool IsNightActive => _isNightActive;
        public bool HasMoreWaves => _currentWaveIndex < _totalWavesForDay;
        public int TotalScheduledEnemies => _totalScheduledEnemies;
        public int TotalDefeatedEnemies => _totalDefeatedEnemies;
        public float NightProgress => _totalScheduledEnemies > 0 ? Math.Min(1f, (float)_totalDefeatedEnemies / _totalScheduledEnemies) : 0f;
        public NightScheduleSummary CurrentNightSummary => _currentNightSummary;

        public event Action<int, int> OnWaveStarted;             // (day, waveIndex)
        public event Action<int, int> OnWaveCompleted;           // (day, waveIndex)
        public event Action<int> OnAllWavesCleared;              // (day)
        public event Action<int> OnEnemiesRemainingChanged;      // (remainingCount)
        public event Action<NightScheduleSummary> OnNightInitialized;
        public event Action<float, int, int> OnNightProgressChanged; // (progress [0..1], defeated, total)
        public event Action<WaveMilestoneData> OnWaveMilestoneReached;

        public static bool IsBossType(EnemyType type)
        {
            return type == EnemyType.CyclopsTuberMaw || type == EnemyType.TaroColossus || type == EnemyType.TheRanger;
        }

        public void StartNight(int day)
        {
            _currentDay = Math.Max(1, day);
            _totalWavesForDay = Math.Min(5, Math.Max(1, _currentDay));
            _currentWaveIndex = 0;
            _activeEnemiesRemaining = 0;
            _isNightActive = true;
            _totalDefeatedEnemies = 0;
            _lastReachedMilestoneIndex = -1;

            PrecomputeNightSchedule();

            OnNightInitialized?.Invoke(_currentNightSummary);
            OnNightProgressChanged?.Invoke(0f, 0, _totalScheduledEnemies);
        }

        private void PrecomputeNightSchedule()
        {
            var milestones = new WaveMilestoneData[_totalWavesForDay];
            int cumulative = 0;

            // Pass 1: Compute total scheduled enemies
            for (int w = 1; w <= _totalWavesForDay; w++)
            {
                var schedule = GetEnemiesScheduleForDayAndWave(_currentDay, w);
                cumulative += schedule.Count;
            }

            _totalScheduledEnemies = cumulative;
            float invTotal = _totalScheduledEnemies > 0 ? (1.0f / _totalScheduledEnemies) : 1.0f;

            // Pass 2: Calculate strictly normalized anchor positions [0.0, 1.0] and metadata
            cumulative = 0;
            for (int w = 1; w <= _totalWavesForDay; w++)
            {
                var schedule = GetEnemiesScheduleForDayAndWave(_currentDay, w);
                cumulative += schedule.Count;

                bool isBoss = false;
                string bossName = null;
                for (int i = 0; i < schedule.Count; i++)
                {
                    if (IsBossType(schedule[i]))
                    {
                        isBoss = true;
                        bossName = schedule[i].ToString();
                    }
                }

                float normalizedPos = (w == _totalWavesForDay) ? 1.0f : (cumulative * invTotal);
                string title = isBoss
                    ? (w == 5 && _currentDay == 5 ? "Final Climax: Dual Bosses" : $"Boss Encounter: {bossName}")
                    : $"Wave {w}";

                milestones[w - 1] = new WaveMilestoneData(w, schedule.Count, cumulative, normalizedPos, isBoss, title);
            }

            _currentNightSummary = new NightScheduleSummary(_currentDay, _totalWavesForDay, _totalScheduledEnemies, milestones);
        }

        public void EndNight()
        {
            _isNightActive = false;
            _activeEnemiesRemaining = 0;
            _totalDefeatedEnemies = 0;
            OnEnemiesRemainingChanged?.Invoke(0);
            OnNightProgressChanged?.Invoke(0f, 0, 0);
        }

        /// <summary>
        /// Advances to the next wave, populating and returning the list of enemies to spawn.
        /// </summary>
        public List<EnemyType> PrepareNextWave()
        {
            if (!_isNightActive) return new List<EnemyType>();

            _currentWaveIndex++;
            List<EnemyType> enemies = GetEnemiesScheduleForDayAndWave(_currentDay, _currentWaveIndex);
            _activeEnemiesRemaining = enemies.Count;

            OnWaveStarted?.Invoke(_currentDay, _currentWaveIndex);
            OnEnemiesRemainingChanged?.Invoke(_activeEnemiesRemaining);

            return enemies;
        }

        /// <summary>
        /// Registers dynamically summoned minions (e.g. from boss abilities).
        /// </summary>
        public void RegisterDynamicEnemy()
        {
            if (!_isNightActive) return;

            _activeEnemiesRemaining++;
            OnEnemiesRemainingChanged?.Invoke(_activeEnemiesRemaining);
        }

        /// <summary>
        /// Records an enemy defeat. When count hits zero, automatically fires wave completion or victory events.
        /// </summary>
        public void RecordEnemyDefeated()
        {
            if (!_isNightActive || _activeEnemiesRemaining <= 0) return;

            _activeEnemiesRemaining--;
            _totalDefeatedEnemies = Math.Min(_totalDefeatedEnemies + 1, _totalScheduledEnemies);

            OnEnemiesRemainingChanged?.Invoke(_activeEnemiesRemaining);
            OnNightProgressChanged?.Invoke(NightProgress, _totalDefeatedEnemies, _totalScheduledEnemies);

            // Check milestone flags
            if (_currentNightSummary.Milestones != null)
            {
                for (int i = _lastReachedMilestoneIndex + 1; i < _currentNightSummary.Milestones.Length; i++)
                {
                    if (_totalDefeatedEnemies >= _currentNightSummary.Milestones[i].CumulativeThreshold)
                    {
                        _lastReachedMilestoneIndex = i;
                        OnWaveMilestoneReached?.Invoke(_currentNightSummary.Milestones[i]);
                    }
                }
            }

            if (_activeEnemiesRemaining == 0)
            {
                OnWaveCompleted?.Invoke(_currentDay, _currentWaveIndex);

                if (!HasMoreWaves)
                {
                    _isNightActive = false;
                    _totalDefeatedEnemies = _totalScheduledEnemies;
                    OnNightProgressChanged?.Invoke(1.0f, _totalScheduledEnemies, _totalScheduledEnemies);
                    OnAllWavesCleared?.Invoke(_currentDay);
                }
            }
        }

        /// <summary>
        /// Returns the predetermined enemy composition for a given day and wave according to the design specification.
        /// </summary>
        public static List<EnemyType> GetEnemiesScheduleForDayAndWave(int day, int wave)
        {
            var list = new List<EnemyType>();

            if (day == 1)
            {
                // Day 1: 1 wave introductory combat
                list.Add(EnemyType.TuberMaw);
                list.Add(EnemyType.TuberMaw);
                list.Add(EnemyType.TaroBrute);
            }
            else if (day == 2)
            {
                // Day 2: 2 waves introducing ranged musketeers
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
                // Day 3: 3 waves, ending with mini-boss Cyclops Tuber Maw
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
                // Day 4: 4 waves, ending with first Boss Taro Colossus
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
                // Day 5: 5 Waves, culminating in Climax Dual Boss fight
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
                    // Mini Encounter: Taro Colossus + 3 Minions
                    list.Add(EnemyType.TaroColossus);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.CornMusketeer);
                }
                else
                {
                    // Wave 5 Grand Climax: 2 Bosses simultaneously + 3 Minions
                    list.Add(EnemyType.CyclopsTuberMaw);
                    list.Add(EnemyType.TheRanger);
                    list.Add(EnemyType.TuberMaw);
                    list.Add(EnemyType.TaroBrute);
                    list.Add(EnemyType.CornMusketeer);
                }
            }

            return list;
        }
    }
}
