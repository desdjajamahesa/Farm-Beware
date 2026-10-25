using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesInteraction
{
    // Kasur hanya bertindak sebagai TRIGGER (pemicu), bukan pengontrol UI.
    // Tidak mengelola layar hitam/efek fade; urusan visual tetap di tangan
    // sistem lain (mis. SleepScreen di masa depan).
    public class BedInteractable : MonoBehaviour, IInteractable, IBedInteractable
    {
        public static event System.Action OnBedSleepExecuted;

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
            if (timeService == null) return;

            if (timeService.CurrentDay == 0)
            {
                worldLabel.displayName = "Sleep (Begin Day 1)";
            }
            else if (timeService.CurrentPhase != DayPhase.Night)
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

            // 0. Interaksi di Day 0 (Hari Kedatangan): Langsung tidur memajukan hari ke Day 1 tanpa Night Brawl
            if (timeService.CurrentDay == 0)
            {
                ExecuteDay0Sleep(timeService, interactor);
                return;
            }

            // 1. Interaksi sebelum malam (Day / Dusk / Dawn): memicu fase malam (Night Brawl)
            if (timeService.CurrentPhase != DayPhase.Night)
            {
                var farmingService = ServiceLocator.Resolve<IFarmingService>();
                string cropWarning = null;
                if (farmingService != null)
                {
                    farmingService.HasCropsAboutToRipen(180f, out cropWarning);
                }

                BedSleepConfirmationUI.Show(cropWarning, () =>
                {
                    StartCoroutine(RoutineDaytimeSleep(timeService, interactor));
                });
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
                pc.AddInputLock("BedSleep");
            }

            // Hapus seluruh food buff aktif saat pemain tidur (sehingga pagi hari bersih dari buff)
            if (interactor != null)
            {
                interactor.SendMessage("ClearAllBuffs", SendMessageOptions.DontRequireReceiver);

                // Malam selesai: pulihkan HP pemain bila komponen PlayerStats tersedia.
                interactor.SendMessage("Heal", sleepHealAmount, SendMessageOptions.DontRequireReceiver);
            }

            int completedDay = timeService.CurrentDay;
            OnBedSleepExecuted?.Invoke();

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
                            $"Day {timeService.CurrentDay} begins!",
                            new Color(1f, 0.9f, 0.3f));
                    }
                    if (pc != null)
                        pc.RemoveInputLock("BedSleep");
                    UpdateLabel();
                });
            }
            else
            {
                timeService.AdvanceToNextDay();
                if (pc != null)
                    pc.RemoveInputLock("BedSleep");
                UpdateLabel();
            }
        }

        private void ExecuteDay0Sleep(ITimeService timeService, GameObject interactor)
        {
            if (!Application.isPlaying)
            {
                ExecuteDay0SleepInstant(timeService, interactor);
                return;
            }

            StartCoroutine(RoutineDay0Sleep(timeService, interactor));
        }

        private void ExecuteDay0SleepInstant(ITimeService timeService, GameObject interactor)
        {
            var pc = interactor != null ? interactor.GetComponent<IPlayerContext>() : ServiceLocator.Resolve<IPlayerContext>();
            if (pc != null) pc.AddInputLock("BedSleep");

            OnBedSleepExecuted?.Invoke();

            if (interactor != null)
            {
                interactor.SendMessage("ClearAllBuffs", SendMessageOptions.DontRequireReceiver);
                interactor.SendMessage("Heal", sleepHealAmount, SendMessageOptions.DontRequireReceiver);
            }

            timeService.AdvanceToNextDay();

            var playerObj = interactor != null ? interactor : GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerObj.SendMessage("EnsureStarterKit", 1, SendMessageOptions.DontRequireReceiver);
                playerObj.SendMessage("EnsureStarterKit", SendMessageOptions.DontRequireReceiver);
            }

            if (pc != null) pc.RemoveInputLock("BedSleep");
            UpdateLabel();
        }

        private System.Collections.IEnumerator RoutineDay0Sleep(ITimeService timeService, GameObject interactor)
        {
            var pc = interactor != null ? interactor.GetComponent<IPlayerContext>() : ServiceLocator.Resolve<IPlayerContext>();
            if (pc != null)
            {
                pc.AddInputLock("BedSleep");
                pc.StopMovement();
            }

            OnBedSleepExecuted?.Invoke();

            // 1. Smooth Fade Out to solid black screen
            var fadeService = ServiceLocator.Resolve<IFadeService>();
            float fadeDuration = 0.5f;
            if (fadeService != null)
            {
                fadeService.FadeIn(fadeDuration);
                yield return new WaitForSecondsRealtime(fadeDuration + 0.05f);
            }
            else
            {
                yield return null;
            }

            // 2. Perform day advance and health/buff updates behind the black curtain
            if (interactor != null)
            {
                interactor.SendMessage("ClearAllBuffs", SendMessageOptions.DontRequireReceiver);
                interactor.SendMessage("Heal", sleepHealAmount, SendMessageOptions.DontRequireReceiver);
            }

            timeService.AdvanceToNextDay();

            // 3. Guarantee Starter Kit on Day 1
            var targetPlayer = interactor != null ? interactor : GameObject.FindWithTag("Player");
            if (targetPlayer != null)
            {
                targetPlayer.SendMessage("EnsureStarterKit", 1, SendMessageOptions.DontRequireReceiver);
                targetPlayer.SendMessage("EnsureStarterKit", SendMessageOptions.DontRequireReceiver);
            }

            // Small pause behind black screen allowing atmosphere and shadow maps to settle
            yield return new WaitForSecondsRealtime(0.25f);

            // 5. Smooth Fade In from black to reveal the sunlit homestead
            if (fadeService != null)
            {
                fadeService.FadeOut(fadeDuration);
                yield return new WaitForSecondsRealtime(fadeDuration + 0.05f);
            }

            // 6. Release input lock and update bed interaction label
            if (pc != null)
            {
                pc.RemoveInputLock("BedSleep");
            }

            UpdateLabel();
        }

        private System.Collections.IEnumerator RoutineDaytimeSleep(ITimeService timeService, GameObject interactor)
        {
            var pc = interactor != null ? interactor.GetComponent<IPlayerContext>() : ServiceLocator.Resolve<IPlayerContext>();
            if (pc != null)
            {
                pc.AddInputLock("BedSleep");
                pc.StopMovement();
            }

            OnBedSleepExecuted?.Invoke();

            // 1. Smooth Fade Out to solid black screen
            var fadeService = ServiceLocator.Resolve<IFadeService>();
            float fadeDuration = 0.5f;
            if (fadeService != null)
            {
                fadeService.FadeIn(fadeDuration);
                yield return new WaitForSecondsRealtime(fadeDuration + 0.05f);
            }
            else
            {
                yield return null;
            }

            // 2. Clear buffs and transition to Night phase behind the black screen
            if (interactor != null)
            {
                interactor.SendMessage("ClearAllBuffs", SendMessageOptions.DontRequireReceiver);
            }

            timeService.StartNightPhase();

            // Small pause behind black screen allowing lighting and atmosphere to settle
            yield return new WaitForSecondsRealtime(0.25f);

            // 3. Smooth Fade In from black to night
            if (fadeService != null)
            {
                fadeService.FadeOut(fadeDuration);
                yield return new WaitForSecondsRealtime(fadeDuration + 0.05f);
            }

            // 4. Release input lock and update bed interaction label
            if (pc != null)
            {
                pc.RemoveInputLock("BedSleep");
            }

            var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
            if (floatingText != null && interactor != null)
            {
                floatingText.SpawnText(
                    transform.position + Vector3.up * 1.2f,
                    "Nightfall begins! Prepare for battle!",
                    new Color(1f, 0.45f, 0.45f));
            }

            UpdateLabel();
        }
    }
}