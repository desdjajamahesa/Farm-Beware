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
        DayPhase CurrentPhase { get; }
        bool IsNight { get; }
        event Action<int, DayPhase> OnPhaseChanged;
        event Action<int> OnDayChanged;
    }
}
