using System;
using FeaturesRendering.Lighting;

namespace FeaturesTime
{
    /// <summary>
    /// Interface contract for the environmental (Day/Night) time simulation service.
    /// Supports Clean Architecture (Inversion of Control) and Dependency Injection (DI).
    /// </summary>
    public interface IDayNightTimeService
    {
        /// <summary>
        /// Current hour in continuous decimal format (0.0f - 24.0f).
        /// </summary>
        float CurrentHour { get; }

        /// <summary>
        /// Current hour as an integer (0 - 23).
        /// </summary>
        int CurrentHourInt { get; }

        /// <summary>
        /// Current minute as an integer (0 - 59).
        /// </summary>
        int CurrentMinuteInt { get; }

        /// <summary>
        /// Current day index in the game calendar cycle (1-based).
        /// </summary>
        int CurrentDay { get; }

        /// <summary>
        /// Current environmental time phase (Dawn, Day, Dusk, Night).
        /// </summary>
        EnvironmentPhase CurrentPhase { get; }

        /// <summary>
        /// Normalized time in [0.0, 1.0] across the 24-hour cycle (0.0 = 00:00, 0.5 = 12:00, 1.0 = 24:00).
        /// </summary>
        float NormalizedTime { get; }

        /// <summary>
        /// Indicates whether time progression is currently paused.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Event emitted when the hour changes (0 - 23).
        /// </summary>
        event Action<int> OnHourChanged;

        /// <summary>
        /// Event emitted when the minute changes (0 - 59).
        /// </summary>
        event Action<int> OnMinuteChanged;

        /// <summary>
        /// Event emitted when an environmental phase transition occurs (Dawn, Day, Dusk, Night).
        /// </summary>
        event Action<EnvironmentPhase> OnTimePhaseChanged;

        /// <summary>
        /// Event emitted when the Dusk Bell rings (15:45 in-game / 13:30 real minutes).
        /// </summary>
        event Action OnDuskWarning;

        /// <summary>
        /// Event emitted when auto-sleep triggers at the end of daytime (18:00 in-game / 16:00 real minutes).
        /// </summary>
        event Action OnAutoSleepTriggered;

        /// <summary>
        /// Remaining real-time duration of the daytime phase in seconds (0 at night).
        /// </summary>
        float DaytimeRemainingSeconds { get; }

        /// <summary>
        /// Event emitted when the calendar day advances (Advance to Next Day).
        /// </summary>
        event Action<int> OnDayChanged;

        /// <summary>
        /// Event emitted whenever the normalized time value changes (only if listeners exist).
        /// </summary>
        event Action<float> OnNormalizedTimeChanged;

        /// <summary>
        /// Sets a specific time immediately (0.0f - 24.0f).
        /// </summary>
        void SetTime(float hour);

        /// <summary>
        /// Pauses or resumes the time simulation.
        /// </summary>
        void SetPaused(bool paused);

        /// <summary>
        /// Advances time by a specified hour delta.
        /// </summary>
        void AdvanceHour(float hours);

        /// <summary>
        /// Skips time directly to the next day at morning start hour (default: 06:00).
        /// </summary>
        void AdvanceToNextDay();
    }
}
