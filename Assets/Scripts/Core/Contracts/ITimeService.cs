using System;

namespace FarmBeware.Core.Runtime
{
    public enum DayPhase
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    /// <summary>
    /// Contract defining game time, day progression, and phase change notifications.
    /// </summary>
    public interface ITimeService
    {
        int CurrentDay { get; }
        float TimeOfDay { get; }
        float CurrentHour { get; }
        DayPhase CurrentPhase { get; }
        bool IsNight { get; }
        bool IsNightEncounterCleared { get; }
        event Action<int> OnMinuteChanged;
        event Action<int, DayPhase> OnPhaseChanged;
        event Action<int> OnDayChanged;
        void StartNightPhase();
        void AdvanceToNextDay();
    }
}
