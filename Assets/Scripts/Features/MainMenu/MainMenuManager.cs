using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MainMenuManager menangani interaksi pengguna pada Main Menu Scene,
/// seperti memulai permainan (berpindah ke StagingScene) dan keluar dari aplikasi.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Nama scene staging/gameplay yang akan dimuat saat menekan Start Game.")]
    [SerializeField] private string stagingSceneName = "StagingScene";

    [Tooltip("Gunakan pemuatan asinkron (Async) untuk mencegah freeze saat loading scene gameplay.")]
    [SerializeField] private bool loadAsynchronously = true;

    [Header("Optional UI References (Auto-Listener)")]
    [Tooltip("Opsional: Jika di-assign di Inspector, listener OnClick akan didaftarkan otomatis.")]
    [SerializeField] private Button startButton;

    [Tooltip("Opsional: Jika di-assign di Inspector, listener OnClick akan didaftarkan otomatis.")]
    [SerializeField] private Button quitButton;

    [Header("Optional Loading UI")]
    [Tooltip("Opsional: GameObject UI loading / panel transisi jika ada.")]
    [SerializeField] private GameObject loadingOverlay;

    // Flag proteksi agar pemain tidak dapat memicu perpindahan scene berulang kali (double click)
    private bool isTransitioning = false;

    private void Awake()
    {
        // Otomatis daftarkan listener jika referensi tombol diberikan di Inspector
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Lepas listener untuk menjaga kebersihan memori (memory leak prevention)
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    /// <summary>
    /// Memulai proses perpindahan ke Staging Scene.
    /// Metode ini dapat dipanggil langsung dari event OnClick() Button di Inspector.
    /// </summary>
    public void StartGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Debug.Log($"[MainMenuManager] Memulai transisi menuju: {stagingSceneName}");

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
        }

        if (loadAsynchronously)
        {
            StartCoroutine(LoadSceneAsyncRoutine(stagingSceneName));
        }
        else
        {
            SceneManager.LoadScene(stagingSceneName);
        }
    }

    /// <summary>
    /// Coroutine untuk memuat scene secara asynchronous di background.
    /// Sangat dianjurkan untuk scene gameplay yang memuat asset 3D / sistem fisika berat.
    /// </summary>
    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Tunggu sampai scene selesai dimuat sepenuhnya
        while (!asyncLoad.isDone)
        {
            // asyncLoad.progress bergerak dari 0.0f hingga 0.9f selama proses pembacaan asset
            yield return null;
        }
    }

    /// <summary>
    /// Menutup aplikasi game.
    /// Menangani perbedaan eksekusi antara Unity Editor dan Standalone Build.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenuManager] Permintaan keluar dari permainan.");

#if UNITY_EDITOR
        // Hentikan mode Play saat dijalankan di dalam Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Keluar dari aplikasi saat dijalankan sebagai standalone executable (.exe / apk)
        Application.Quit();
#endif
    }
}
