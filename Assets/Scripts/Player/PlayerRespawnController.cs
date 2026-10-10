using System.Collections;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesCombat;
using FeaturesTime;
using FeaturesSaveSystem;

/// <summary>
/// Orchestrates the player defeat and respawn lifecycle when PlayerStats.OnPlayerDied is raised.
/// Decouples visual fading, time progression, combat cleanup, and bed repositioning from core player stats.
/// </summary>
public class PlayerRespawnController : MonoBehaviour
{
    public static PlayerRespawnController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerRespawnController>(FindObjectsInactive.Include);
            }
            return instance;
        }
        private set => instance = value;
    }
    private static PlayerRespawnController instance;

    public bool IsRespawning => isRespawning;

    private PlayerStats playerStats;
    private PlayerControl playerControl;
    private Rigidbody playerRb;
    private bool isRespawning = false;
    private bool isDefeated = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;

        playerStats = GetComponent<PlayerStats>();
        playerControl = GetComponent<PlayerControl>();
        playerRb = GetComponent<Rigidbody>();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>() ?? FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.OnPlayerDied -= HandlePlayerDied;
                playerStats.OnPlayerDied += HandlePlayerDied;
            }
        }

        if (playerControl == null)
        {
            playerControl = GetComponent<PlayerControl>() ?? FindFirstObjectByType<PlayerControl>();
        }

        if (playerRb == null)
        {
            playerRb = GetComponent<Rigidbody>() ?? (playerStats != null ? playerStats.GetComponent<Rigidbody>() : null);
        }

        // Pre-initialize DeathScreenUI hierarchy so modal is ready in memory
        if (DeathScreenUI.Instance != null)
        {
            DeathScreenUI.Instance.EnsureUIHierarchy();
        }
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDied += HandlePlayerDied;
        }

        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.OnLoadCompleted += HandleSaveLoaded;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDied -= HandlePlayerDied;
        }

        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.OnLoadCompleted -= HandleSaveLoaded;
        }
    }

    private void HandleSaveLoaded(GameSaveData data)
    {
        isDefeated = false;
        isRespawning = false;
        if (DeathScreenUI.Instance != null)
        {
            DeathScreenUI.Instance.CloseInstant();
        }
    }

    private void HandlePlayerDied()
    {
        if (isRespawning || isDefeated) return;
        isDefeated = true;

        // 1. Kunci input pergerakan & aksi pemain
        if (playerControl != null)
        {
            playerControl.SetInputActive(false);
        }

        // Tampilkan teks feedback kekalahan seketika di posisi pingsan
        var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
        if (floatingText != null)
        {
            floatingText.SpawnText(
                transform.position + Vector3.up * 1.8f,
                "💀 COLLAPSED!",
                new Color(1f, 0.25f, 0.25f));
        }

        // Buka kursor dan tampilkan Death Screen Menu secara instan tanpa delay
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (DeathScreenUI.Instance != null)
        {
            DeathScreenUI.Instance.Open();
        }
    }

    /// <summary>
    /// Menjalankan alur pemulihan di kasur kamar tidur (Checkpoint):
    /// Fade hitam -> Hentikan Night Brawl -> Majukan waktu ke 06:00 -> Pulihkan 50% HP -> Teleportasi ke kasur -> Fade terang -> Buka kontrol.
    /// </summary>
    public void ExecuteBedRespawn(System.Action onComplete = null)
    {
        if (isRespawning) return;
        StartCoroutine(RoutinePlayerDefeatSequence(onComplete));
    }

    private IEnumerator RoutinePlayerDefeatSequence(System.Action onComplete = null)
    {
        isRespawning = true;
        isDefeated = false;

        // Pulihkan kecepatan simulasi game
        Time.timeScale = 1f;

        // Kunci input kontrol pemain selama transisi respawn
        if (playerControl != null)
        {
            playerControl.SetInputActive(false);
        }

        // Tutup Death Screen jika masih terbuka
        if (DeathScreenUI.Instance != null && DeathScreenUI.Instance.IsOpen)
        {
            DeathScreenUI.Instance.Close();
        }

        // 2. Transisi layar menghitam (Fade In to black)
        var fadeService = ServiceLocator.Resolve<IFadeService>();
        if (fadeService != null)
        {
            bool fadeDone = false;
            fadeService.FadeIn(0.6f, () => fadeDone = true);
            while (!fadeDone)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        // 3. Hentikan pertarungan malam dan bersihkan seluruh sisa musuh yang masih aktif
        if (NightBrawlManager.Instance != null)
        {
            NightBrawlManager.Instance.AbortBrawl();
        }

        // 4. Kembalikan waktu ke pagi hari pada hari yang sama (sebelum night terpicu)
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SetTimeState(TimeManager.Instance.currentDay, TimeManager.DayPhase.Day, false, notifyPhaseChanged: true);
        }

        if (DayNightTimeManager.Instance != null)
        {
            DayNightTimeManager.Instance.SetTime(DayNightTimeManager.Instance.DayStartHour);
        }

        // 5. Pulihkan vital pemain sama seperti waktu pagi hari (100% HP, stamina, hunger, thirst)
        if (playerStats != null)
        {
            playerStats.RestoreStats(
                playerStats.maxHealth,
                playerStats.maxStamina,
                playerStats.maxHunger,
                playerStats.maxThirst);
        }

        // 6. Teleportasikan karakter pemain ke kasur kamar tidur secara aman
        Vector3 respawnPos = new Vector3(27.1f, 0.5f, 21.04f); // Fallback bedroom coordinates sanitized to y = 0.5f
        Quaternion respawnRot = Quaternion.identity;

        var bed = FindFirstObjectByType<FeaturesInteraction.BedInteractable>(FindObjectsInactive.Include);
        if (bed != null)
        {
            respawnPos = bed.transform.position + bed.transform.forward * 0.9f + Vector3.up * 0.5f;
            respawnRot = Quaternion.LookRotation(bed.transform.forward, Vector3.up);
        }

        if (playerControl != null)
        {
            playerControl.Teleport(respawnPos, respawnRot);
        }
        else if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = respawnPos;
            playerRb.rotation = respawnRot;
        }
        else
        {
            transform.position = respawnPos;
            transform.rotation = respawnRot;
        }
        Physics.SyncTransforms();

        // Jeda sejenak dalam keadaan gelap agar kamera dan fisika stabil
        yield return new WaitForSeconds(0.4f);

        // 7. Transisi layar memudar kembali terang (Fade Out from black)
        if (fadeService != null)
        {
            bool fadeDone = false;
            fadeService.FadeOut(0.6f, () => fadeDone = true);
            while (!fadeDone)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        // 8. Buka kembali kunci kontrol pemain & beri pesan informatif
        if (playerControl != null)
        {
            playerControl.SetInputActive(true);
        }

        var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
        if (floatingText != null)
        {
            int currentDayNum = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;
            floatingText.SpawnText(
                transform.position + Vector3.up * 1.8f,
                $"💤 You collapsed and recovered in bed (Day {currentDayNum} Morning)...",
                new Color(1f, 0.85f, 0.35f));
        }

        isRespawning = false;
        onComplete?.Invoke();
    }
}
