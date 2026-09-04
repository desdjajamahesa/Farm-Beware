using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Panel Pause Menu yang akan dimunculkan/disembunyikan")]
    [SerializeField] private GameObject pauseMenuPanel;

    public bool IsPaused { get; private set; }

    private CanvasGroup panelCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (pauseMenuPanel != null)
        {
            panelCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // Tutup menu saat awal game
        ResumeGame();
    }

    private void Update()
    {
        bool escPressed = false;

        // Cek New Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }

        // Fallback Old Input System jika New Input System belum aktif
        #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }
        #endif

        if (escPressed)
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        IsPaused = true;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        // Hentikan waktu game
        Time.timeScale = 0f;

        // Munculkan dan buka kunci kursor mouse agar bisa klik tombol
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        IsPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Kembalikan waktu game ke normal
        Time.timeScale = 1f;

        // Kunci kursor kembali untuk gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ExitGame()
    {
        // Kembalikan timeScale sebelum keluar/pindah scene
        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
