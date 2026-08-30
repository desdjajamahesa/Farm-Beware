using FeaturesCamera;
using FeaturesInteraction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using FarmBeware.Logic;
using FeaturesWardrobe;

namespace FeaturesWardrobe
{
    /// <summary>
    /// Wardrobe manager - delegates camera switching to CameraManager.
    /// Handles UI fading, player positioning, outfit changes, and mirror coordination.
    /// </summary>
    public class WardrobeManager : MonoBehaviour
    {
        #region Singleton
        private static WardrobeManager _instance;
        public static WardrobeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<WardrobeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        #endregion

        [Header("Wardrobe System")]
        [Tooltip("Parent container (WardrobeRoot).")]
        [SerializeField] private Transform wardrobeRoot;

        [Tooltip("MirrorCamera component on the Mirror GameObject.")]
        [SerializeField] private MirrorCamera mirrorCamera;

        [Header("Fallback Camera (if CameraManager not available)")]
        [Tooltip("Main gameplay camera (untuk fallback disable).")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Wardrobe screen camera (untuk fallback enable/disable saja — pose diatur di scene).")]
        [SerializeField] private Camera wardrobeCamera;

        [Header("Mirror Positioning")]
        [Tooltip("Jarak player dari permukaan cermin saat buka wardrobe.")]
        [SerializeField] private float playerMirrorDistance = 3.5f;

        [Tooltip("Geser lateral player ke kiri dari sumbu cermin (meter).")]
        [SerializeField] private float lateralShift = 0.8f;

        [Header("Animation")]
        [Tooltip("Durasi UI fade in/out (detik).")]
        [SerializeField] private float uiFadeDuration = 0.3f;

        [Header("Mirror Fallback")]
        [Tooltip("Fallback anchor untuk posisi player jika MirrorCamera/MirrorSurface tidak ada.")]
        [SerializeField] private Transform mirrorFallbackAnchor;

        [Header("Player & Outfit")]
        [SerializeField] private PlayerControl playerControl;
        
        [SerializeField] private PlayerOutfit playerOutfit;
        
        [SerializeField] private HoverLabelController hoverLabelController;
        
        [SerializeField] private PlayerInteractor playerInteractor;
        
        public PlayerOutfit PlayerOutfitProp => playerOutfit;
        [SerializeField] private Transform playerHead;

        [Header("UI")]
        [SerializeField] private GameObject wardrobeUIPanel;
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private WardrobeUI wardrobeUI;

        [Header("Wardrobe Items Data")]
        [Tooltip("All available wardrobe items organized by category.")]
        [SerializeField] private List<FarmBeware.Logic.WardrobeItemData> allWardrobeItems = new List<FarmBeware.Logic.WardrobeItemData>();

        [Header("Chest Animation")]
        [Tooltip("Animator on the chest lid (child 'lid' of Wardrobe). Controls open/close animation via 'IsOpen' bool.")]
        [SerializeField] private Animator chestLidAnimator;

        [Header("Debug")]
        [SerializeField] private bool debugCameraAudit = false;

        private bool isInWardrobeMode;
        public static bool IsInWardrobeMode { get; private set; }
        private Coroutine fadeCoroutine;
        private Vector3 playerOriginalPosition;
        private Quaternion playerOriginalRotation;

        #region Public API

        public bool IsInWardrobeModeInstance => isInWardrobeMode;

        public void EnterWardrobeMode()
        {
            Debug.Log("[WardrobeManager] EnterWardrobeMode CALLED");

            if (isInWardrobeMode) return;

            isInWardrobeMode = true;
            IsInWardrobeMode = true;

            // Resync live player + mirror with persisted outfit state on entry.
            if (playerOutfit != null && playerOutfit.currentOutfit != null)
            {
                playerOutfit.ApplyOutfit(playerOutfit.currentOutfit);
                // Direct hat toggle — no OutfitMeshSwapper dependency.
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "hat")
                        {
                            t.gameObject.SetActive(playerOutfit.isHatEquipped);
                            break;
                        }
                    }
                }
            }

            // --- FIX: Initialize currentOutfit sebelum UI dibangun ---
            if (playerOutfit != null && playerOutfit.currentOutfit == null && playerOutfit.unlockedOutfits.Count > 0)
            {
                // Ambil outfit pertama sebagai currentOutfit default
                playerOutfit.currentOutfit = Instantiate(playerOutfit.unlockedOutfits[0]);
                Debug.Log($"[WardrobeManager] currentOutfit initialized from unlockedOutfits[0]: {playerOutfit.currentOutfit.outfitName}");
            }
            if (playerOutfit != null && playerOutfit.currentOutfit == null)
            {
                // Fallback: outfit baru dengan variant 0 semua
                var defaultOutfit = ScriptableObject.CreateInstance<OutfitData>();
                defaultOutfit.topVariant = 0;
                defaultOutfit.bottomVariant = 0;
                defaultOutfit.shoesVariant = 0;
                defaultOutfit.hatVariant = 0;
                playerOutfit.currentOutfit = defaultOutfit;
                Debug.Log("[WardrobeManager] currentOutfit initialized as default (all variants 0)");
            }

            // Initialize wardrobe items data if not already done
            InitializeWardrobeItems();

            // Re-resolve in case a prefab swap invalidated earlier lookups.
            if (playerControl == null) playerControl = FindObjectOfType<PlayerControl>();

            if (playerControl != null)
            {
                playerOriginalPosition = playerControl.transform.position;
                playerOriginalRotation = playerControl.transform.rotation;
            }
            else
            {
                Debug.LogError("[WardrobeManager] playerControl still null at EnterWardrobeMode line 140 — wardrobe entry aborted gracefully.");
                return;
            }

            // Position player in front of mirror
            PositionPlayerToMirror();

            // Initialize MirrorCamera BEFORE camera mode switch
            try
            {
                if (mirrorCamera != null)
                {
                    mirrorCamera.EnsureInitialized();
                    if (playerHead != null)
                        mirrorCamera.SetPlayerTarget(playerHead);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] MirrorCamera init failed: {e.Message}");
            }

            // Delegate camera switching to CameraManager (preferred)
            try
            {
                if (CameraManager.Instance != null)
                {
                    CameraManager.Instance.SetMode(CameraManager.CameraMode.WardrobeMode, wardrobeRoot);
                }
                else
                {
                    Debug.LogWarning("[WardrobeManager] CameraManager not found! Using fallback camera control.");

                    // FALLBACK: Manual camera control
                    if (mainCamera != null)
                        mainCamera.enabled = false;

                    // Camera pose is authored in the scene — fallback only toggles enabled state.
                    if (wardrobeCamera != null)
                        wardrobeCamera.enabled = true;

                    // Lock input and cursor manually
                    if (playerControl != null)
                        playerControl.isInputLocked = true;

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] Camera switching failed: {e.Message}");
            }

            // Enable MirrorInnerCam (renders to RawImage texture)
            try
            {
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.enabled = true;
                    Debug.Log("[WardrobeManager] MirrorInnerCam enabled");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] MirrorInnerCam enable failed: {e.Message}");
            }

            // Open chest lid animation
            if (chestLidAnimator != null)
                chestLidAnimator.SetBool("IsOpen", true);

            // UI fade in
            try
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                
                if (wardrobeUIPanel != null)
                    wardrobeUIPanel.SetActive(true);
                
                // Ensure WardrobeUI component's GameObject is also active (safeguard)
                if (wardrobeUI != null && wardrobeUI.gameObject != null)
                    wardrobeUI.gameObject.SetActive(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] UI panel activate failed: {e.Message}");
            }
            
            // Regenerate item grid slots to ensure button listeners are wired up
            try
            {
                if (wardrobeUI != null)
                    wardrobeUI.RefreshItemGrid();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] RefreshItemGrid failed: {e.Message}");
            }
            
            // Force-hide interaction tooltip before disabling systems (with null-safe guards)
            try
            {
                if (ItemDisplayUI.Instance != null)
                {
                    ItemDisplayUI.Instance.HideInteractPrompt();
                    ItemDisplayUI.Instance.HideWorldHover();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] ItemDisplayUI hide failed: {e.Message}");
            }
            
            try
            {
                if (hoverLabelController != null)
                {
                    hoverLabelController.HideIfShowing();
                    hoverLabelController.ClearAll();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] HoverLabelController clear failed: {e.Message}");
            }
            
            try
            {
                fadeCoroutine = StartCoroutine(FadeUI(true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] FadeUI start failed: {e.Message}");
            }

            try
            {
                SetUIRaycastBlocking(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] SetUIRaycastBlocking failed: {e.Message}");
            }

            // Hide hotbar while in wardrobe (same pattern as trophy cabinet mode)
            try
            {
                if (InventoryManagerUI.Instance != null && InventoryManagerUI.Instance.playerHotbarContainer != null)
                    InventoryManagerUI.Instance.playerHotbarContainer.gameObject.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] Hotbar hide failed: {e.Message}");
            }

            // PreviewController binding removed: the in-world MirrorCamera handles all preview rendering.

            // Subscribe to UI close event
            if (wardrobeUI != null)
                wardrobeUI.OnWardrobeClosed += ExitWardrobeMode;

            LogMirrorDiagnostics();

            Debug.Log("[WardrobeManager] Entered Wardrobe Mode");
        }

        public void ExitWardrobeMode()
        {
            Debug.Log("[DEBUG] ExitWardrobeMode CALLED.");

            // 1. TRUE Idempotency check at the VERY TOP — only short-circuit
            //    if BOTH the static and instance flags say we are NOT in wardrobe.
            if (!IsInWardrobeMode && !isInWardrobeMode) return;

            // Close chest lid animation
            if (chestLidAnimator != null)
                chestLidAnimator.SetBool("IsOpen", false);

            // 2. Camera transition (delegate to CameraManager; it now auto-resolves
            //    PlayerControl if its serialized field is null).
            try
            {
                if (CameraManager.Instance != null)
                {
                    CameraManager.Instance.SetMode(CameraManager.CameraMode.Gameplay, null);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Camera reset error: " + e.Message);
            }

            // 3. Brute-force unlock (backup) — uses FindObjectOfType so it works
            //    even when CameraManager's serialized playerControl ref is null.
            try
            {
                var player = UnityEngine.Object.FindObjectOfType<PlayerControl>();
                if (player != null)
                {
                    var t = player.GetType();
                    var field = t.GetField("isInputLocked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(player, false);
                    }
                    else
                    {
                        var prop = t.GetProperty("isInputLocked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (prop != null && prop.CanWrite) prop.SetValue(player, false);
                    }
                    UnityEngine.Debug.Log("[DEBUG] Forcefully unlocked PlayerControl.isInputLocked.");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[DEBUG] Brute-force unlock found no PlayerControl in scene.");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Brute-force unlock error: " + e.Message);
            }

            // 4. Cleanup UI & interaction systems.
            if (wardrobeUI != null && wardrobeUI.gameObject != null) wardrobeUI.gameObject.SetActive(false);
            try { if (playerInteractor != null) playerInteractor.enabled = true; } catch { }
            try { if (hoverLabelController != null) hoverLabelController.enabled = true; } catch { }

            // Disable MirrorInnerCam before camera switch
            try
            {
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.enabled = false;
                    Debug.Log("[WardrobeManager] MirrorInnerCam disabled");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] MirrorInnerCam disable failed: {e.Message}");
            }

            // Re-enable MirrorInnerCam for mirror surface (gameplay)
            try
            {
                if (mirrorCamera != null && mirrorCamera.MirrorTexture != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                    mirrorCamera.MirrorCameraComponent.enabled = true;
                    Debug.Log("[WardrobeManager] MirrorInnerCam re-enabled for mirror surface");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] MirrorInnerCam re-enable failed: {e.Message}");
            }

            // UI fade out
            try
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeUI(false));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] FadeUI failed: {e.Message}");
            }

            try
            {
                if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
                if (wardrobeUIPanel != null) wardrobeUIPanel.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] UI panel deactivate failed: {e.Message}");
            }

            try { SetUIRaycastBlocking(false); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] SetUIRaycastBlocking failed: {e.Message}");
            }

            // Force the Wardrobe UI CanvasGroup to not block raycasts
            try
            {
                var cg = wardrobeUI != null ? wardrobeUI.GetComponent<CanvasGroup>() : null;
                if (cg != null)
                {
                    cg.alpha = 0f;
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                    Debug.Log("[WardrobeManager] Forced WardrobeUI CanvasGroup to not block raycasts");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] CanvasGroup cleanup failed: {e.Message}");
            }

            // Restore hotbar
            try
            {
                bool trophyOwnsHotbar = InventoryManagerUI.Instance != null &&
                    InventoryManagerUI.Instance.currentStorageInventory != null &&
                    TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode;
                if (!trophyOwnsHotbar && InventoryManagerUI.Instance != null &&
                    InventoryManagerUI.Instance.playerHotbarContainer != null)
                    InventoryManagerUI.Instance.playerHotbarContainer.gameObject.SetActive(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] Hotbar restore failed: {e.Message}");
            }

            // Unsubscribe from UI close event
            if (wardrobeUI != null)
                wardrobeUI.OnWardrobeClosed -= ExitWardrobeMode;

            // Revert preview if a preview is in flight
            if (playerOutfit != null && playerOutfit.IsPreviewing)
                playerOutfit.Revert();

            // 5. Update state LAST.
            isInWardrobeMode = false;
            IsInWardrobeMode = false;
            UnityEngine.Debug.Log("[DEBUG] ExitWardrobeMode FULLY EXECUTED.");
        }

        public void TryOnOutfit(OutfitData outfit)
        {
            if (outfit == null || playerOutfit == null) return;
            playerOutfit.TryOn(outfit);
        }

        public void PreviewDefault()
        {
            if (playerOutfit != null)
                playerOutfit.PreviewDefault();
        }

        public void CommitOutfit()
        {
            if (playerOutfit != null)
                playerOutfit.Commit();
        }

        public void RevertOutfit()
        {
            if (playerOutfit != null)
                playerOutfit.Revert();
        }

        private void SetUIRaycastBlocking(bool enabled)
        {
            if (uiCanvasGroup == null) return;
            uiCanvasGroup.interactable = enabled;
            uiCanvasGroup.blocksRaycasts = enabled;
        }

        private void LogMirrorDiagnostics()
        {
            bool mirrorReady = mirrorCamera != null && mirrorCamera.MirrorTexture != null;
            bool innerCamOn = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null && mirrorCamera.MirrorCameraComponent.enabled;
            bool targetOk = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null &&
                            mirrorCamera.MirrorCameraComponent.targetTexture == mirrorCamera.MirrorTexture;
            Debug.Log($"[Wardrobe] diag -> MirrorTexture={(mirrorReady ? "OK" : "NULL")} " +
                      $"| InnerCam.enabled={innerCamOn} " +
                      $"| targetTexture==RT={targetOk}");
        }

        #endregion

        #region Wardrobe Items Initialization

        private void InitializeWardrobeItems()
        {
            if (wardrobeUI == null) return;
            wardrobeUI.RefreshItemGrid();
        }

        #endregion

        #region Camera & UI Fade

        private IEnumerator FadeUI(bool fadeIn)
        {
            if (uiCanvasGroup == null) yield break;

            float startAlpha = fadeIn ? 0f : 1f;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < uiFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / uiFadeDuration);
                uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, EaseInOutCubic(t));
                yield return null;
            }

            uiCanvasGroup.alpha = targetAlpha;

            fadeCoroutine = null;
        }

        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        private void PositionPlayerToMirror()
        {
            if (playerControl == null) return;

            // Fixed pose authored per user spec — no runtime math.
            Vector3 target = new Vector3(17.5f, 0.1f, 17.5f);
            Quaternion facingMirror = Quaternion.Euler(0f, -90f, 0f);

            Rigidbody rb = playerControl.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = target;
                rb.rotation = facingMirror;
                // Sync transform immediately (required in Edit Mode / non-simulated contexts)
                playerControl.transform.position = target;
                playerControl.transform.rotation = facingMirror;
            }
            else
            {
                playerControl.transform.position = target;
                playerControl.transform.rotation = facingMirror;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Self-healing: if the Player prefab was replaced and serialized
            // references were lost, resolve them dynamically.
            if (playerControl == null) playerControl = FindObjectOfType<PlayerControl>();
            if (playerOutfit == null) playerOutfit = FindObjectOfType<PlayerOutfit>();
            if (playerInteractor == null) playerInteractor = FindObjectOfType<PlayerInteractor>();
            if (hoverLabelController == null) hoverLabelController = FindObjectOfType<HoverLabelController>();
            if (wardrobeUI == null) wardrobeUI = FindObjectOfType<WardrobeUI>();
            if (mainCamera == null && Camera.main != null) mainCamera = Camera.main;
            if (mirrorCamera == null) mirrorCamera = FindObjectOfType<MirrorCamera>();

            // Enforce initial state: UI hidden, mirror cam bound to RT only.
            // Scene files can persist stale active-states from a previous session.
            if (wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(false);
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = 0f;
                uiCanvasGroup.interactable = false;
                uiCanvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (!isInWardrobeMode) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ExitWardrobeMode();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion
    }
}