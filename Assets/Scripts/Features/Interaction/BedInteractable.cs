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

        public void Interact(GameObject interactor)
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogWarning("TimeManager tidak ditemukan di scene!");
                return;
            }

            // Hanya bisa tidur saat malam hari; siang hari interaksi dibatalkan.
            if (TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day)
            {
                Debug.Log("[BedInteractable] It's still daytime. You can only sleep at night!");
                if (PlayerUI.FloatingCombatTextManager.Instance != null && interactor != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 1.2f,
                        "Can only sleep at night!",
                        new Color(1f, 0.8f, 0.3f));
                }
                return;
            }

            // Kunci gerakan pemain selama proses tidur
            PlayerControl pc = interactor.GetComponent<PlayerControl>();
            if (pc != null)
            {
                pc.StopMovement();
                pc.isInputLocked = true;
            }

            // Malam: pulihkan HP pemain bila komponen PlayerStats tersedia.
            PlayerStats stats = interactor.GetComponent<PlayerStats>();
            if (stats != null)
                stats.Heal(sleepHealAmount);

            // Teruskan ke backend waktu: transisi ke hari berikutnya (fase Day).
            TimeManager.Instance.AdvanceToNextDay();

            // Buka kunci setelah transisi selesai
            if (pc != null)
                pc.isInputLocked = false;
        }
    }
}