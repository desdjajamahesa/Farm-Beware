using System;
using System.Collections.Generic;
using FeaturesStoryline.Data;
using FeaturesCombat;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline
{
    /// <summary>
    /// Pure C# POCO class governing story progression, salience-based rule evaluation,
    /// journal catalog state, and modal queue safety.
    /// Operates with ZERO Unity lifecycle dependencies and ZERO garbage collection on hot paths.
    /// Synthesizes Valve's Response System (Elan Ruskin) and Quality-Based Narrative (Emily Short).
    /// </summary>
    public class StorylineEngine
    {
        private int _currentChapter = 1;
        private int _maxChapter = 10;
        private readonly HashSet<string> _triggeredBeats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedJournals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _lastPlayedDayMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<StoryBeatData> _pendingModalBeats = new Queue<StoryBeatData>();
        private bool _isStoryComplete = false;

        public int CurrentChapter => _currentChapter;
        public int MaxChapter
        {
            get => _maxChapter;
            set => _maxChapter = Math.Max(1, value);
        }
        public bool IsStoryComplete => _isStoryComplete;
        public int UnlockedJournalCount => _unlockedJournals.Count;
        public int TriggeredBeatCount => _triggeredBeats.Count;
        public int PendingModalCount => _pendingModalBeats.Count;
        public bool HasPendingModalBeats => _pendingModalBeats.Count > 0;

        public IReadOnlyCollection<string> TriggeredBeatIds => _triggeredBeats;
        public IReadOnlyCollection<string> UnlockedJournalIds => _unlockedJournals;
        public IReadOnlyDictionary<string, int> LastPlayedDayMap => _lastPlayedDayMap;

        public event Action<string> OnBeatTriggered;
        public event Action<string> OnJournalUnlocked;
        public event Action<int> OnChapterChanged;
        public event Action OnStoryCompleted;

        public static string NormalizeJournalId(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return entryId;
            return entryId switch
            {
                "LOG_NEXAPHYTO_01" => "JOURNAL_01_WAIVER",
                "LOG_NEXAPHYTO_02" => "JOURNAL_02_REAGENT7",
                "LOG_NEXAPHYTO_03" => "JOURNAL_03_INCIDENT042",
                "LOG_NEXAPHYTO_04" => "JOURNAL_04_APEX_BIO",
                "LOG_NEXAPHYTO_05" => "JOURNAL_05_FAILSAFE",
                _ => entryId
            };
        }

        public bool HasTriggered(string beatId)
        {
            if (string.IsNullOrEmpty(beatId)) return false;
            return _triggeredBeats.Contains(beatId);
        }

        public bool HasJournalEntry(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            return _unlockedJournals.Contains(NormalizeJournalId(entryId));
        }

        /// <summary>
        /// Evaluates all candidate story beats against the current world state on INarrativeFactBoard
        /// using specificity scoring: S(R_i) = P_base * 100 + specificityScore.
        /// Returns the most specific matching rule with zero allocations, applying mutations if matched.
        /// </summary>
        public StoryBeatData EvaluateTrigger(
            StoryTriggerType triggerType,
            INarrativeFactBoard facts,
            IReadOnlyList<StoryBeatData> candidates,
            int currentDay,
            int wave = 0,
            EnemyType? bossType = null)
        {
            if (candidates == null || candidates.Count == 0) return null;

            StoryBeatData bestBeat = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var beat = candidates[i];
                if (beat == null || string.IsNullOrEmpty(beat.beatId)) continue;

                // 1. Trigger type match
                if (beat.triggerType != triggerType) continue;

                // 2. RepeatPolicy gate
                switch (beat.repeatPolicy)
                {
                    case RepeatPolicy.MatchOnce:
                        if (_triggeredBeats.Contains(beat.beatId)) continue;
                        break;
                    case RepeatPolicy.OncePerDay:
                        if (_lastPlayedDayMap.TryGetValue(beat.beatId, out int lastDay) && lastDay == currentDay) continue;
                        break;
                    case RepeatPolicy.CooldownDays:
                        if (_lastPlayedDayMap.TryGetValue(beat.beatId, out int cooldownDay) && (currentDay - cooldownDay < beat.cooldownDays)) continue;
                        break;
                    case RepeatPolicy.AlwaysRepeatable:
                        break;
                }

                // 3. Chapter matching
                if (beat.chapter > _currentChapter) continue;

                // 4. Day matching if specified
                if (beat.triggerDay > 0 && beat.triggerDay != currentDay) continue;

                // 5. Wave matching if specified
                if (beat.triggerWave > 0 && beat.triggerWave != wave) continue;

                // 6. Boss archetype matching if evaluated
                if ((triggerType == StoryTriggerType.BossSpawn || triggerType == StoryTriggerType.BossDeath) && bossType.HasValue)
                {
                    if (beat.triggerBossType != bossType.Value) continue;
                }

                // 7. Prerequisite beats check
                if (beat.prerequisiteBeatIds != null && beat.prerequisiteBeatIds.Length > 0)
                {
                    bool prereqsMet = true;
                    for (int p = 0; p < beat.prerequisiteBeatIds.Length; p++)
                    {
                        string prereq = beat.prerequisiteBeatIds[p];
                        if (!string.IsNullOrEmpty(prereq) && !_triggeredBeats.Contains(prereq))
                        {
                            prereqsMet = false;
                            break;
                        }
                    }
                    if (!prereqsMet) continue;
                }

                // 8. Zero-GC Narrative Fact Blackboard Preconditions evaluation
                int conditionScore = 0;
                if (facts != null && beat.criteria != null && beat.criteria.Length > 0)
                {
                    if (!facts.EvaluateConditions(beat.criteria, out conditionScore))
                    {
                        continue;
                    }
                }

                // 9. Specificity Scoring: S(R_i) = P_base * 100 + conditionScore
                int totalScore = (beat.saliencePriority * 100) + conditionScore;
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestBeat = beat;
                }
            }

            // If a rule won the evaluation, execute state transitions and mutations
            if (bestBeat != null)
            {
                _lastPlayedDayMap[bestBeat.beatId] = currentDay;
                _triggeredBeats.Add(bestBeat.beatId);

                // Apply fact mutations to blackboard
                if (facts != null && bestBeat.mutations != null && bestBeat.mutations.Length > 0)
                {
                    for (int m = 0; m < bestBeat.mutations.Length; m++)
                    {
                        facts.ApplyMutation(bestBeat.mutations[m]);
                    }
                }

                if (bestBeat.unlocksJournalEntry && bestBeat.journalEntry != null)
                {
                    UnlockJournal(bestBeat.journalEntry.entryId);
                }

                OnBeatTriggered?.Invoke(bestBeat.beatId);
            }

            return bestBeat;
        }

        /// <summary>
        /// Legacy boolean evaluation preserved for backward compatibility.
        /// </summary>
        public bool CanTrigger(StoryBeatData beat, StoryTriggerType triggerType, int day, int wave = 0, EnemyType? bossType = null)
        {
            if (beat == null || string.IsNullOrEmpty(beat.beatId)) return false;
            if (beat.triggerType != triggerType) return false;
            if (beat.chapter > _currentChapter) return false;
            if (beat.triggerDay > 0 && beat.triggerDay != day) return false;
            if (beat.triggerWave > 0 && beat.triggerWave != wave) return false;

            switch (beat.repeatPolicy)
            {
                case RepeatPolicy.MatchOnce:
                    if (_triggeredBeats.Contains(beat.beatId)) return false;
                    break;
                case RepeatPolicy.OncePerDay:
                    if (_lastPlayedDayMap.TryGetValue(beat.beatId, out int lastDay) && lastDay == day) return false;
                    break;
                case RepeatPolicy.CooldownDays:
                    if (_lastPlayedDayMap.TryGetValue(beat.beatId, out int cooldownDay) && (day - cooldownDay < beat.cooldownDays)) return false;
                    break;
                case RepeatPolicy.AlwaysRepeatable:
                    break;
            }

            if ((triggerType == StoryTriggerType.BossSpawn || triggerType == StoryTriggerType.BossDeath) && bossType.HasValue)
            {
                if (beat.triggerBossType != bossType.Value) return false;
            }

            if (beat.prerequisiteBeatIds != null && beat.prerequisiteBeatIds.Length > 0)
            {
                for (int i = 0; i < beat.prerequisiteBeatIds.Length; i++)
                {
                    string prereq = beat.prerequisiteBeatIds[i];
                    if (!string.IsNullOrEmpty(prereq) && !_triggeredBeats.Contains(prereq))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool RecordBeatTriggered(StoryBeatData beat)
        {
            if (beat == null || string.IsNullOrEmpty(beat.beatId)) return false;
            if (_triggeredBeats.Contains(beat.beatId)) return false;

            _triggeredBeats.Add(beat.beatId);
            OnBeatTriggered?.Invoke(beat.beatId);

            if (beat.unlocksJournalEntry && beat.journalEntry != null)
            {
                UnlockJournal(beat.journalEntry.entryId);
            }

            return true;
        }

        public bool RecordBeatTriggered(string beatId)
        {
            if (string.IsNullOrEmpty(beatId)) return false;
            if (_triggeredBeats.Contains(beatId)) return false;

            _triggeredBeats.Add(beatId);
            OnBeatTriggered?.Invoke(beatId);
            return true;
        }

        public bool UnlockJournal(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            string canonicalId = NormalizeJournalId(entryId);
            if (_unlockedJournals.Contains(canonicalId)) return false;

            _unlockedJournals.Add(canonicalId);
            OnJournalUnlocked?.Invoke(canonicalId);
            return true;
        }

        public void SetChapter(int chapter)
        {
            int clamped = Math.Max(1, Math.Min(_maxChapter, chapter));
            if (_currentChapter == clamped) return;

            _currentChapter = clamped;
            OnChapterChanged?.Invoke(_currentChapter);

            if (_currentChapter >= 5 && !_isStoryComplete)
            {
                // Completed story threshold
            }
        }

        public void AdvanceChapter()
        {
            if (_currentChapter < _maxChapter)
            {
                SetChapter(_currentChapter + 1);
            }
        }

        public void MarkStoryComplete()
        {
            if (_isStoryComplete) return;
            _isStoryComplete = true;
            OnStoryCompleted?.Invoke();
        }

        // Modal Queue Management (Hard Safety Invariant)
        public void EnqueuePendingModalBeat(StoryBeatData beat)
        {
            if (beat == null || string.IsNullOrEmpty(beat.beatId)) return;
            foreach (var existing in _pendingModalBeats)
            {
                if (existing != null && string.Equals(existing.beatId, beat.beatId, StringComparison.OrdinalIgnoreCase))
                    return; // Prevent duplicate modal queue entries
            }
            _pendingModalBeats.Enqueue(beat);
        }

        public StoryBeatData DequeuePendingModalBeat()
        {
            if (_pendingModalBeats.Count == 0) return null;
            return _pendingModalBeats.Dequeue();
        }

        public void ClearPendingModalBeats()
        {
            _pendingModalBeats.Clear();
        }

        public StorylineSavePayload CreateSavePayload(INarrativeFactBoard facts = null)
        {
            int[] rawFacts = new int[(int)NarrativeFactKey.MaxCount];
            facts?.CopyRawFacts(rawFacts);

            var pendingIds = new List<string>(_pendingModalBeats.Count);
            foreach (var b in _pendingModalBeats)
            {
                if (b != null && !string.IsNullOrEmpty(b.beatId))
                    pendingIds.Add(b.beatId);
            }

            return new StorylineSavePayload(
                _currentChapter,
                _triggeredBeats,
                _unlockedJournals,
                _isStoryComplete,
                rawFacts,
                _lastPlayedDayMap,
                pendingIds
            );
        }

        public void RestoreState(StorylineSavePayload payload, INarrativeFactBoard facts = null, IReadOnlyList<StoryBeatData> beatCatalog = null)
        {
            if (payload == null) return;

            _currentChapter = Math.Max(1, payload.chapter);
            _isStoryComplete = payload.isComplete;

            _triggeredBeats.Clear();
            if (payload.triggeredBeats != null)
            {
                for (int i = 0; i < payload.triggeredBeats.Count; i++)
                {
                    _triggeredBeats.Add(payload.triggeredBeats[i]);
                }
            }

            _unlockedJournals.Clear();
            if (payload.unlockedJournals != null)
            {
                for (int i = 0; i < payload.unlockedJournals.Count; i++)
                {
                    string canonicalId = NormalizeJournalId(payload.unlockedJournals[i]);
                    if (!string.IsNullOrEmpty(canonicalId))
                    {
                        _unlockedJournals.Add(canonicalId);
                    }
                }
            }

            _lastPlayedDayMap.Clear();
            if (payload.lastPlayedBeatKeys != null && payload.lastPlayedDays != null)
            {
                int count = Math.Min(payload.lastPlayedBeatKeys.Count, payload.lastPlayedDays.Count);
                for (int i = 0; i < count; i++)
                {
                    _lastPlayedDayMap[payload.lastPlayedBeatKeys[i]] = payload.lastPlayedDays[i];
                }
            }

            _pendingModalBeats.Clear();
            if (payload.pendingModalBeatIds != null && beatCatalog != null)
            {
                for (int p = 0; p < payload.pendingModalBeatIds.Count; p++)
                {
                    string id = payload.pendingModalBeatIds[p];
                    for (int c = 0; c < beatCatalog.Count; c++)
                    {
                        var candidate = beatCatalog[c];
                        if (candidate != null && string.Equals(candidate.beatId, id, StringComparison.OrdinalIgnoreCase))
                        {
                            EnqueuePendingModalBeat(candidate);
                            break;
                        }
                    }
                }
            }

            if (facts != null && payload.rawFacts != null && payload.rawFacts.Length > 0)
            {
                facts.LoadRawFacts(payload.rawFacts);
            }
        }

        public void ImportState(StorylineSavePayload payload)
        {
            RestoreState(payload, null);
        }

        /// <summary>Backward compatible alias for CreateSavePayload.</summary>
        public StorylineSavePayload ExportState(INarrativeFactBoard facts = null) => CreateSavePayload(facts);

        /// <summary>Backward compatible alias for MarkStoryComplete.</summary>
        public void CompleteStory() => MarkStoryComplete();

        public void Reset()
        {
            _currentChapter = 1;
            _isStoryComplete = false;
            _triggeredBeats.Clear();
            _unlockedJournals.Clear();
            _lastPlayedDayMap.Clear();
            _pendingModalBeats.Clear();
        }
    }
}
