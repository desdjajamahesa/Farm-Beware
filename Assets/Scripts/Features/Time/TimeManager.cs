using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Decoupled phase-based (Day/Night) time backend singleton.
// Pure data and event signaling; independent of UI or specific gameplay features.
public class TimeManager : MonoBehaviour
{
    // Day phase: Day = morning/afternoon, Night = night.
    public enum DayPhase { Day, Night }

    private static TimeManager _instance;

    // Singleton with lazy fallback resolver: if static reference is not yet assigned
    // (e.g. due to lifecycle execution order), resolve from active or inactive scene instances.
    public static TimeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                TimeManager[] found = FindObjectsByType<TimeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                    _instance = found[0];
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    // Time state (discrete phase-based rather than continuous clock progression).
    public int currentDay { get; private set; } = 1;
    public DayPhase currentPhase { get; private set; } = DayPhase.Day;

    // Night encounter (Night Brawl) completion state.
    // Reset to false when night starts and set true when all night waves are cleared.
    public bool isNightEncounterCleared { get; set; } = false;

    // Data-driven event signals for UI and backend listeners.
    public static event Action<int> OnAnyDayChanged;
    public static event Action<DayPhase> OnAnyPhaseChanged;
    public event Action<int> OnDayChanged;
    public event Action<DayPhase> OnPhaseChanged;

    private void Awake()
    {
        // Enforce single active instance; destroy duplicates if present.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (FarmBeware.Core.Runtime.GameSessionState.IsPendingNewGameIntroCutscene)
        {
            currentDay = 0;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
    }

    // Day transition: player wakes up in Day phase on the next day.
    public void AdvanceToNextDay()
    {
        currentDay++;
        currentPhase = DayPhase.Day;
        isNightEncounterCleared = false;

        OnDayChanged?.Invoke(currentDay);
        OnAnyDayChanged?.Invoke(currentDay);
        OnPhaseChanged?.Invoke(currentPhase);
        OnAnyPhaseChanged?.Invoke(currentPhase);

        Debug.Log($"Day changed to {currentDay}, Phase: {currentPhase}");
    }

    // Starts the night phase (invoked from bed during daytime or spawners).
    public void StartNightPhase()
    {
        if (currentPhase == DayPhase.Night)
            return;

        currentPhase = DayPhase.Night;
        isNightEncounterCleared = false;
        OnPhaseChanged?.Invoke(currentPhase);
        OnAnyPhaseChanged?.Invoke(currentPhase);

        Debug.Log($"Phase changed to {currentPhase} (Night Brawl begins!)");
    }

    // Skips to night phase; no-op if already night.
    public void SkipToNight()
    {
        StartNightPhase();
    }

    /// <summary>
    /// Restores full time state (used by Save/Load system).
    /// Does NOT invoke OnDayChanged, as OnDayChanged indicates advancing to a new day.
    /// Invoking it on load erroneously triggers DayTransitionUI ('Morning' banner), FarmingManager (advancing crop days),
    /// and DailyEconomyManager (resetting stats).
    /// </summary>
    public void SetTimeState(int day, DayPhase phase, bool cleared, bool notifyPhaseChanged = false, bool notifyDayChanged = false)
    {
        currentDay = Mathf.Max(0, day);
        currentPhase = phase;
        isNightEncounterCleared = cleared;

        if (notifyDayChanged)
        {
            OnDayChanged?.Invoke(currentDay);
            OnAnyDayChanged?.Invoke(currentDay);
        }

        if (notifyPhaseChanged)
        {
            OnPhaseChanged?.Invoke(currentPhase);
            OnAnyPhaseChanged?.Invoke(currentPhase);
        }
    }
}