using System;
using System.Collections.Generic;
using FeaturesStoryline.Data;
using FeaturesCombat;

namespace FeaturesStoryline
{
    /// <summary>
    /// Pure C# POCO class governing story progression, beat condition evaluation,
    /// journal catalog state, and chapter transitions.
    /// Operates with ZERO Unity lifecycle dependencies and ZERO garbage collection on hot paths.
    /// </summary>
    public class StorylineEngine
    {
        private int _currentChapter = 1;
        private int _maxChapter = 10;
        private readonly HashSet<string> _triggeredBeats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedJournals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

        public IReadOnlyCollection<string> TriggeredBeatIds => _triggeredBeats;
        public IReadOnlyCollection<string> UnlockedJournalIds => _unlockedJournals;

        public event Action<string> OnBeatTriggered;
        public event Action<string> OnJournalUnlocked;
        public event Action<int> OnChapterChanged;
        public event Action OnStoryCompleted;

        public bool HasTriggered(string beatId)
        {
            if (string.IsNullOrEmpty(beatId)) return false;
            return _triggeredBeats.Contains(beatId);
        }

        public bool HasJournalEntry(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            return _unlockedJournals.Contains(entryId);
        }

        /// <summary>
        /// Evaluates whether a story beat satisfies all trigger conditions.
        /// Pure logic: zero allocations, early guard returns.
        /// </summary>
        public bool CanTrigger(StoryBeatData beat, StoryTriggerType triggerType, int day, int wave = 0, EnemyType? bossType = null)
        {
            if (beat == null) return false;
            if (string.IsNullOrEmpty(beat.beatId)) return false;

            // 1. Already triggered check
            if (_triggeredBeats.Contains(beat.beatId)) return false;

            // 2. Trigger type match
            if (beat.triggerType != triggerType) return false;

            // 3. Chapter matching (beat chapter must match or beat chapter <= current chapter)
            if (beat.chapter > _currentChapter) return false;

            // 4. Day matching if specified
            if (beat.triggerDay > 0 && beat.triggerDay != day) return false;

            // 5. Wave matching if specified
            if (beat.triggerWave > 0 && beat.triggerWave != wave) return false;

            // 6. Boss archetype matching if evaluated
            if ((triggerType == StoryTriggerType.BossSpawn || triggerType == StoryTriggerType.BossDeath) && bossType.HasValue)
            {
                if (beat.triggerBossType != bossType.Value) return false;
            }

            // 7. Prerequisite beats check
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

        /// <summary>
        /// Records the completion of a story beat and unlocks any associated journal entry.
        /// Returns true if successfully triggered, false if already executed.
        /// </summary>
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

        /// <summary>
        /// Directly records a beat ID as triggered.
        /// </summary>
        public bool RecordBeatTriggered(string beatId)
        {
            if (string.IsNullOrEmpty(beatId)) return false;
            if (_triggeredBeats.Contains(beatId)) return false;

            _triggeredBeats.Add(beatId);
            OnBeatTriggered?.Invoke(beatId);
            return true;
        }

        /// <summary>
        /// Unlocks a journal entry by ID.
        /// </summary>
        public bool UnlockJournal(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            if (_unlockedJournals.Contains(entryId)) return false;

            _unlockedJournals.Add(entryId);
            OnJournalUnlocked?.Invoke(entryId);
            return true;
        }

        /// <summary>
        /// Sets the current story chapter.
        /// </summary>
        public void SetChapter(int chapter)
        {
            int clamped = Math.Max(1, Math.Min(_maxChapter, chapter));
            if (_currentChapter == clamped) return;

            _currentChapter = clamped;
            OnChapterChanged?.Invoke(_currentChapter);
        }

        /// <summary>
        /// Advances the current chapter by 1 (up to MaxChapter).
        /// </summary>
        public void AdvanceChapter()
        {
            if (_currentChapter < _maxChapter)
            {
                _currentChapter++;
                OnChapterChanged?.Invoke(_currentChapter);
            }
        }

        /// <summary>
        /// Marks the entire storyline as completed (post-epilogue).
        /// </summary>
        public void CompleteStory()
        {
            if (_isStoryComplete) return;
            _isStoryComplete = true;
            OnStoryCompleted?.Invoke();
        }

        /// <summary>
        /// Exports state into a pure DTO for persistence.
        /// </summary>
        public StorylineSavePayload ExportState()
        {
            return new StorylineSavePayload(_currentChapter, _triggeredBeats, _unlockedJournals, _isStoryComplete);
        }

        /// <summary>
        /// Restores state from a persistent DTO.
        /// </summary>
        public void ImportState(StorylineSavePayload payload)
        {
            if (payload == null) return;

            _currentChapter = Math.Max(1, Math.Min(_maxChapter, payload.chapter));
            _triggeredBeats.Clear();
            if (payload.triggeredBeats != null)
            {
                for (int i = 0; i < payload.triggeredBeats.Count; i++)
                {
                    if (!string.IsNullOrEmpty(payload.triggeredBeats[i]))
                        _triggeredBeats.Add(payload.triggeredBeats[i]);
                }
            }

            _unlockedJournals.Clear();
            if (payload.unlockedJournals != null)
            {
                for (int i = 0; i < payload.unlockedJournals.Count; i++)
                {
                    if (!string.IsNullOrEmpty(payload.unlockedJournals[i]))
                        _unlockedJournals.Add(payload.unlockedJournals[i]);
                }
            }

            _isStoryComplete = payload.isComplete;
        }

        /// <summary>
        /// Clears all story progress (for new game reset).
        /// </summary>
        public void ResetProgress()
        {
            _currentChapter = 1;
            _triggeredBeats.Clear();
            _unlockedJournals.Clear();
            _isStoryComplete = false;
        }
    }
}
