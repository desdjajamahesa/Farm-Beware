using System;
using FeaturesRendering.Lighting;

namespace FeaturesTime
{
    /// <summary>
    /// Kontrak antarmuka (Interface) untuk layanan waktu simulasi lingkungan (Day/Night).
    /// Mendukung prinsip Clean Architecture (Inversion of Control) dan Dependency Injection (DI).
    /// </summary>
    public interface IDayNightTimeService
    {
        /// <summary>
        /// Jam saat ini dalam format desimal kontinu (0.0f - 24.0f).
        /// </summary>
        float CurrentHour { get; }

        /// <summary>
        /// Jam saat ini dalam bilangan bulat (0 - 23).
        /// </summary>
        int CurrentHourInt { get; }

        /// <summary>
        /// Menit saat ini dalam bilangan bulat (0 - 59).
        /// </summary>
        int CurrentMinuteInt { get; }

        /// <summary>
        /// Hari ke-n dalam siklus kalender permainan (mulai dari 1).
        /// </summary>
        int CurrentDay { get; }

        /// <summary>
        /// Fase waktu lingkungan saat ini (Dawn, Day, Dusk, Night).
        /// </summary>
        EnvironmentPhase CurrentPhase { get; }

        /// <summary>
        /// Waktu ternormalisasi dalam rentang [0.0, 1.0] sepanjang siklus 24 jam (0.0 = 00:00, 0.5 = 12:00, 1.0 = 24:00).
        /// </summary>
        float NormalizedTime { get; }

        /// <summary>
        /// Menandakan apakah progresi waktu saat ini sedang dijeda (paused).
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Event dipancarkan saat jam berganti (0 - 23).
        /// </summary>
        event Action<int> OnHourChanged;

        /// <summary>
        /// Event dipancarkan saat menit berganti (0 - 59).
        /// </summary>
        event Action<int> OnMinuteChanged;

        /// <summary>
        /// Event dipancarkan saat terjadi transisi fase lingkungan (Dawn, Day, Dusk, Night).
        /// </summary>
        event Action<EnvironmentPhase> OnTimePhaseChanged;

        /// <summary>
        /// Event dipancarkan saat hari kalender berganti (Advance to Next Day).
        /// </summary>
        event Action<int> OnDayChanged;

        /// <summary>
        /// Event dipancarkan setiap kali nilai waktu ternormalisasi berubah (hanya jika ada listener).
        /// </summary>
        event Action<float> OnNormalizedTimeChanged;

        /// <summary>
        /// Mengatur waktu spesifik secara instan (0.0f - 24.0f).
        /// </summary>
        void SetTime(float hour);

        /// <summary>
        /// Menjeda atau melanjutkan simulasi waktu.
        /// </summary>
        void SetPaused(bool paused);

        /// <summary>
        /// Memajukan waktu sebesar delta jam tertentu.
        /// </summary>
        void AdvanceHour(float hours);

        /// <summary>
        /// Melompati waktu langsung ke hari berikutnya pada jam awal pagi (default: 06:00).
        /// </summary>
        void AdvanceToNextDay();
    }
}
