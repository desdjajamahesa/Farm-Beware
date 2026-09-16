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
                Debug.Log("Masih siang, belum bisa tidur!");
                return;
            }

            // Malam: pulihkan HP pemain bila komponen PlayerStats tersedia.
            PlayerStats stats = interactor.GetComponent<PlayerStats>();
            if (stats != null)
                stats.Heal(sleepHealAmount);

            // Teruskan ke backend waktu: transisi ke hari berikutnya (fase Day).
            TimeManager.Instance.AdvanceToNextDay();
        }
    }
}