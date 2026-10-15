using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesInteraction
{
    // Kasur hanya bertindak sebagai TRIGGER (pemicu), bukan pengontrol UI.
    // Tidak mengelola layar hitam/efek fade; urusan visual tetap di tangan
    // sistem lain (mis. SleepScreen di masa depan).
    public class BedInteractable : MonoBehaviour, IInteractable, IBedInteractable
    {
        // Jumlah HP yang dipulihkan saat pemain tidur.
        [SerializeField] private int sleepHealAmount = 100;

        private WorldLabel worldLabel;

        private void Awake()
        {
            worldLabel = GetComponent<WorldLabel>();
        }

        private void OnEnable()
        {
            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null)
            {
                timeService.OnPhaseChanged += HandlePhaseChanged;
            }
            UpdateLabel();
        }

        private void OnDisable()
        {
            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null)
            {
                timeService.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(int day, DayPhase newPhase)
        {
            UpdateLabel();
        }

        public void UpdateLabel()
        {
            if (worldLabel == null)
                worldLabel = GetComponent<WorldLabel>();

            if (worldLabel == null) return;

            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService == null || timeService.CurrentPhase != DayPhase.Night)
            {
                worldLabel.displayName = "Sleep (Start Night Phase)";
            }
            else
            {
                if (timeService.IsNightEncounterCleared)
                    worldLabel.displayName = "Sleep (Advance to Next Day)";
                else
                    worldLabel.displayName = "Bed (Monsters Lurking Outside!)";
            }
        }

        public void Interact(GameObject interactor)
        {
            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService == null)
            {
                Debug.LogWarning("ITimeService tidak ditemukan via ServiceLocator!");
                return;
            }

            var pc = interactor != null ? interactor.GetComponent<IPlayerContext>() : ServiceLocator.Resolve<IPlayerContext>();
            var floatingText = ServiceLocator.Resolve<IFloatingTextService>();

            // 1. Interaksi sebelum malam (Day / Dusk / Dawn): memicu fase malam (Night Brawl)
            if (timeService.CurrentPhase != DayPhase.Night)
            {
                var farmingService = ServiceLocator.Resolve<IFarmingService>();
                if (farmingService != null && farmingService.HasCropsAboutToRipen(180f, out string cropWarning))
                {
                    BedSleepConfirmationUI.Show(cropWarning, () =>
                    {
                        ExecuteDaytimeSleep(timeService, interactor);
                    });
                    return;
                }

                ExecuteDaytimeSleep(timeService, interactor);
                return;
            }

            // 2. Interaksi di malam hari:
            // Cek apakah gelombang musuh sudah tuntas
            if (!timeService.IsNightEncounterCleared)
            {
                Debug.Log("[BedInteractable] Monsters are still lurking outside! Clear the wave first.");
                if (floatingText != null && interactor != null)
                {
                    floatingText.SpawnText(
                        transform.position + Vector3.up * 1.2f,
                        "Monsters are still outside! Clear the wave first.",
                        new Color(1f, 0.4f, 0.4f));
                }
                UpdateLabel();
                return;
            }

            // Kunci gerakan pemain selama proses tidur
            if (pc != null)
            {
                pc.IsInputLocked = true;
            }

            // Malam selesai: pulihkan HP pemain bila komponen PlayerStats tersedia.
            if (interactor != null)
            {
                interactor.SendMessage("Heal", sleepHealAmount, SendMessageOptions.DontRequireReceiver);
            }

            int completedDay = timeService.CurrentDay;

            // Tampilkan laporan pagi operasi perkebunan & hasil Night Brawl sebelum transisi hari baru
            var dailyReport = ServiceLocator.Resolve<IDailyReportService>();
            if (dailyReport != null)
            {
                dailyReport.ShowReport(completedDay, () =>
                {
                    timeService.AdvanceToNextDay();
                    if (floatingText != null && interactor != null)
                    {
                        floatingText.SpawnText(
                            transform.position + Vector3.up * 1.2f,
                            $"☀️ Day {timeService.CurrentDay} begins!",
                            new Color(1f, 0.9f, 0.3f));
                    }
                    if (pc != null)
                        pc.IsInputLocked = false;
                    UpdateLabel();
                });
            }
            else
            {
                timeService.AdvanceToNextDay();
                if (pc != null)
                    pc.IsInputLocked = false;
                UpdateLabel();
            }
        }

        private void ExecuteDaytimeSleep(ITimeService timeService, GameObject interactor)
        {
            timeService.StartNightPhase();

            var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
            if (floatingText != null && interactor != null)
            {
                floatingText.SpawnText(
                    transform.position + Vector3.up * 1.2f,
                    "🌙 Nightfall begins! Prepare for battle!",
                    new Color(1f, 0.45f, 0.45f));
            }

            UpdateLabel();
        }
    }
}