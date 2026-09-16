using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Backend waktu berbasis fase (Day/Night) yang decoupled (singleton).
// Murni data + event sinyal; tidak menempel pada UI maupun gameplay tertentu.
public class TimeManager : MonoBehaviour
{
    // Fase hari: Day = pagi/siang, Night = malam.
    public enum DayPhase { Day, Night }

    private static TimeManager _instance;

    // Singleton dengan resolver lazy sebagai pengaman: bila static belum ter-set
    // (mis. karena urutan lifecycle tidak terpanggil), Instance tetap berhasil
    // di-resolve dari objek aktif yang ada di scene.
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

    // State waktu (statis berbasis fase, bukan jam yang berjalan).
    public int currentDay { get; private set; } = 1;
    public DayPhase currentPhase { get; private set; } = DayPhase.Day;

    // Event sinyal (data-driven) untuk listener UI/backend lain.
    public event Action<int> OnDayChanged;
    public event Action<DayPhase> OnPhaseChanged;

    private void Awake()
    {
        // Jaga hanya satu instance aktif; buang duplikat bila ada.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // DEBUG SEMENTARA: tekan N untuk melompat ke fase malam (uji tidur Kasur).
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            SkipToNight();
    }

    // Transisi hari: pemain bangun di fase Day pada hari berikutnya.
    public void AdvanceToNextDay()
    {
        currentDay++;
        currentPhase = DayPhase.Day;

        OnDayChanged?.Invoke(currentDay);
        OnPhaseChanged?.Invoke(currentPhase);

        Debug.Log($"Day changed to {currentDay}, Phase: {currentPhase}");
    }

    // Lompat ke fase malam; no-op bila sudah malam.
    public void SkipToNight()
    {
        if (currentPhase == DayPhase.Night)
            return;

        currentPhase = DayPhase.Night;
        OnPhaseChanged?.Invoke(currentPhase);

        Debug.Log($"Phase changed to {currentPhase}");
    }
}