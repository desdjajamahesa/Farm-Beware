using UnityEngine;

namespace FeaturesInteraction
{
    // Kasur hanya bertindak sebagai TRIGGER (pemicu), bukan pengontrol UI.
    // Tidak mengelola layar hitam/efek fade; urusan visual tetap di tangan
    // sistem lain (mis. SleepScreen di masa depan).
    public class BedInteractable : MonoBehaviour, IInteractable
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
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
            UpdateLabel();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(TimeManager.DayPhase newPhase)
        {
            UpdateLabel();
        }

        public void UpdateLabel()
        {
            if (worldLabel == null) return;

            if (TimeManager.Instance == null || TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day)
            {
                worldLabel.displayName = "Sleep (Start Night Phase)";
            }
            else
            {
                if (TimeManager.Instance.isNightEncounterCleared)
                    worldLabel.displayName = "Sleep (Advance to Next Day)";
                else
                    worldLabel.displayName = "Bed (Monsters Lurking Outside!)";
            }
        }

        public void Interact(GameObject interactor)
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogWarning("TimeManager tidak ditemukan di scene!");
                return;
            }

            PlayerControl pc = interactor != null ? interactor.GetComponent<PlayerControl>() : null;
            PlayerStats stats = interactor != null ? interactor.GetComponent<PlayerStats>() : null;

            // 1. Interaksi di siang hari: memicu fase malam (Night Brawl)
            if (TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day)
            {
                if (pc != null)
                {
                    pc.StopMovement();
                }

                TimeManager.Instance.StartNightPhase();

                if (PlayerUI.FloatingCombatTextManager.Instance != null && interactor != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 1.2f,
                        "🌙 Nightfall begins! Prepare for battle!",
                        new Color(1f, 0.45f, 0.45f));
                }

                UpdateLabel();
                return;
            }

            // 2. Interaksi di malam hari:
            // Cek apakah gelombang musuh sudah tuntas
            if (!TimeManager.Instance.isNightEncounterCleared)
            {
                Debug.Log("[BedInteractable] Monsters are still lurking outside! Clear the wave first.");
                if (PlayerUI.FloatingCombatTextManager.Instance != null && interactor != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
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
                pc.StopMovement();
                pc.isInputLocked = true;
            }

            // Malam selesai: pulihkan HP pemain bila komponen PlayerStats tersedia.
            if (stats != null)
                stats.Heal(sleepHealAmount);

            // Teruskan ke backend waktu: transisi ke hari berikutnya (fase Day).
            TimeManager.Instance.AdvanceToNextDay();

            if (PlayerUI.FloatingCombatTextManager.Instance != null && interactor != null)
            {
                PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.2f,
                    $"☀️ Good morning! Day {TimeManager.Instance.currentDay} begins.",
                    new Color(1f, 0.9f, 0.3f));
            }

            // Buka kunci setelah transisi selesai
            if (pc != null)
                pc.isInputLocked = false;

            UpdateLabel();
        }
    }
}