using System;
using System.Collections.Generic;

namespace FeaturesCombat.Wave
{
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

        public int CurrentDay => _currentDay;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalWavesForDay => _totalWavesForDay;
        public int ActiveEnemiesRemaining => _activeEnemiesRemaining;
        public bool IsNightActive => _isNightActive;
        public bool HasMoreWaves => _currentWaveIndex < _totalWavesForDay;

        public event Action<int, int> OnWaveStarted;             // (day, waveIndex)
        public event Action<int, int> OnWaveCompleted;           // (day, waveIndex)
        public event Action<int> OnAllWavesCleared;              // (day)
        public event Action<int> OnEnemiesRemainingChanged;      // (remainingCount)

        public void StartNight(int day)
        {
            _currentDay = Math.Max(1, day);
            _totalWavesForDay = Math.Min(5, Math.Max(1, _currentDay));
            _currentWaveIndex = 0;
            _activeEnemiesRemaining = 0;
            _isNightActive = true;
        }

        public void EndNight()
        {
            _isNightActive = false;
            _activeEnemiesRemaining = 0;
            OnEnemiesRemainingChanged?.Invoke(0);
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
            OnEnemiesRemainingChanged?.Invoke(_activeEnemiesRemaining);

            if (_activeEnemiesRemaining == 0)
            {
                OnWaveCompleted?.Invoke(_currentDay, _currentWaveIndex);

                if (!HasMoreWaves)
                {
                    _isNightActive = false;
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
