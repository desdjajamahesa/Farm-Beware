using FarmBeware.Core.Runtime;
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

        [Header("Wardrobe Player Placement")]
        [Tooltip("Exact world position of the player when inside wardrobe mode.")]
        [SerializeField] private Vector3 wardrobePlayerPosition = new Vector3(27.100000381469728f, 0.040000081062316897f, 21.040000915527345f);

        [Tooltip("Exact world rotation (Euler angles) of the player when inside wardrobe mode.")]
        [SerializeField] private Vector3 wardrobePlayerRotation = new Vector3(0f, 180f, 0f);

        [Header("Animation")]
        [Tooltip("Durasi UI fade in/out (detik).")]
        [SerializeField] private float uiFadeDuration = 0.3f;

        [Header("Mirror Fallback")]
        [Tooltip("Fallback anchor untuk posisi player jika MirrorCamera/MirrorSurface tidak ada.")]
        [SerializeField] private Transform mirrorFallbackAnchor;

        [Header("Player & Outfit")]
        private IPlayerContext PlayerContext => ServiceLocator.Resolve<IPlayerContext>();
        
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

#pragma warning disable 0414
        [Header("Debug")]
        [SerializeField] private bool debugCameraAudit = false;
#pragma warning restore 0414

        private bool isInWardrobeMode;
        private static bool _isInWardrobeMode;
        public static bool IsInWardrobeMode
        {
            get => _isInWardrobeMode && Instance != null && Instance.isInWardrobeMode;
            private set => _isInWardrobeMode = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isInWardrobeMode = false;
        }
        private Coroutine fadeCoroutine;
        private Vector3 playerOriginalPosition;
        private Quaternion playerOriginalRotation;
        private int previousHotbarIndex = -1;

        #region Public API

        public bool IsInWardrobeModeInstance => isInWardrobeMode;

        public void EnterWardrobeMode()
        {
            Debug.Log("[WardrobeManager] EnterWardrobeMode CALLED");

            if (isInWardrobeMode) return;

            isInWardrobeMode = true;
            IsInWardrobeMode = true;

            // 0. Unequip any held item / weapon so player stands in clean idle pose
            try
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    var inventory = player.GetComponent<InventoryComponent>();
                    if (inventory != null)
                    {
                        previousHotbarIndex = inventory.selectedHotbarIndex;
                    }

                    var playerEquip = player.GetComponent("PlayerEquipment");
                    if (playerEquip != null)
                    {
                        var destroyMethod = playerEquip.GetType().GetMethod("DestroyCurrentWeapon", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        destroyMethod?.Invoke(playerEquip, null);
                    }

                    var anim = player.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.SetBool("HasWeapon", false);
                        anim.SetBool("Idle", true);
                        anim.SetBool("Sprinting", false);
                        anim.SetFloat("Vel", 0f);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WardrobeManager] Unequip held item failed: {ex.Message}");
            }

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

            var playerCtx = PlayerContext;
            if (playerCtx != null)
            {
                playerOriginalPosition = playerCtx.Transform.position;
                playerOriginalRotation = playerCtx.Transform.rotation;
            }
            else
            {
                Debug.LogError("[WardrobeManager] PlayerContext still null at EnterWardrobeMode — wardrobe entry aborted gracefully.");
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

            // Delegate camera switching to ICameraService (preferred)
            try
            {
                var cameraService = ServiceLocator.Resolve<ICameraService>();
                if (cameraService != null)
                {
                    cameraService.SetMode(CameraMode.WardrobeMode, wardrobeRoot);
                // Camera pose is authored in the scene — no runtime override needed.
                }
                else
                {
                    Debug.LogWarning("[WardrobeManager] ICameraService not found! Using fallback camera control.");

                    // FALLBACK: Manual camera control
                    if (mainCamera != null)
                        mainCamera.enabled = false;

                    // Camera pose is authored in the scene — fallback only toggles enabled state.
                    if (wardrobeCamera != null)
                        wardrobeCamera.enabled = true;

                    // Lock input and cursor manually
                    if (PlayerContext != null)
                        PlayerContext.IsInputLocked = true;

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] Camera switching failed: {e.Message}");
            }

            // Enable MirrorCamera (renders to RawImage texture)
            try
            {
                if (mirrorCamera != null)
                {
                    mirrorCamera.EnableMirrorCamera(true);
                    Debug.Log("[WardrobeManager] MirrorCamera enabled");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] MirrorCamera enable failed: {e.Message}");
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

            // 2. Camera transition (delegate to ICameraService)
            try
            {
                var camService = ServiceLocator.Resolve<ICameraService>();
                if (camService != null)
                {
                    camService.SetMode(CameraMode.Gameplay, null);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Camera reset error: " + e.Message);
            }

            // 3. Unlock player input via IPlayerContext
            try
            {
                if (PlayerContext != null)
                {
                    PlayerContext.IsInputLocked = false;
                    UnityEngine.Debug.Log("[DEBUG] Forcefully unlocked PlayerContext.IsInputLocked.");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Player unlock error: " + e.Message);
            }

            // 4. Cleanup UI & interaction systems.
            if (wardrobeUI != null && wardrobeUI.gameObject != null) wardrobeUI.gameObject.SetActive(false);
            try { if (playerInteractor != null) playerInteractor.enabled = true; } catch { }
            try { if (hoverLabelController != null) hoverLabelController.enabled = true; } catch { }

            // Disable MirrorCamera
            try
            {
                if (mirrorCamera != null)
                {
                    mirrorCamera.EnableMirrorCamera(false);
                    Debug.Log("[WardrobeManager] MirrorCamera disabled");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WardrobeManager] MirrorCamera disable failed: {e.Message}");
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
                var trophyService = ServiceLocator.Resolve<ITrophyService>();
                bool trophyOwnsHotbar = InventoryManagerUI.Instance != null &&
                    InventoryManagerUI.Instance.currentStorageInventory != null &&
                    trophyService != null && trophyService.IsInTrophyMode;
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

            // Restore held item / hotbar selection
            try
            {
                var player = GameObject.Find("Player");
                if (player != null && previousHotbarIndex >= 0)
                {
                    var inventory = player.GetComponent<InventoryComponent>();
                    if (inventory != null)
                    {
                        inventory.SelectHotbarSlot(previousHotbarIndex);
                    }
                    var playerEquip = player.GetComponent("PlayerEquipment");
                    if (playerEquip != null)
                    {
                        var updateMethod = playerEquip.GetType().GetMethod("UpdateEquipmentVisual", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        updateMethod?.Invoke(playerEquip, new object[] { previousHotbarIndex });
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WardrobeManager] Restore held item failed: {ex.Message}");
            }

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
            var playerCtx = PlayerContext;
            if (playerCtx == null) return;

            Vector3 target = wardrobePlayerPosition != Vector3.zero 
                ? wardrobePlayerPosition 
                : new Vector3(27.100000381469728f, 0.040000081062316897f, 21.040000915527345f);
            Quaternion facingMirror = Quaternion.Euler(wardrobePlayerRotation);

            Transform pTransform = playerCtx.Transform;
            Rigidbody rb = pTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = target;
                rb.rotation = facingMirror;
                pTransform.position = target;
                pTransform.rotation = facingMirror;
            }
            else
            {
                pTransform.position = target;
                pTransform.rotation = facingMirror;
            }

            Physics.SyncTransforms();
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
            isInWardrobeMode = false;
            _isInWardrobeMode = false;

            // Self-healing: if references were lost, resolve them dynamically.
            if (playerOutfit == null) playerOutfit = FindFirstObjectByType<PlayerOutfit>();
            if (playerInteractor == null) playerInteractor = FindFirstObjectByType<PlayerInteractor>();
            if (hoverLabelController == null) hoverLabelController = FindFirstObjectByType<HoverLabelController>();
            if (wardrobeUI == null) wardrobeUI = FindFirstObjectByType<WardrobeUI>();
            if (mainCamera == null && Camera.main != null) mainCamera = Camera.main;
            if (mirrorCamera == null) mirrorCamera = FindFirstObjectByType<MirrorCamera>();
            if (playerHead == null && PlayerContext != null)
            {
                foreach (var t in PlayerContext.Transform.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLower().Contains("head") && !t.name.ToLower().Contains("end"))
                    {
                        playerHead = t;
                        break;
                    }
                }
            }

            // Enforce initial state: UI hidden, mirror cam off for performance.
            if (mirrorCamera != null)
                mirrorCamera.EnableMirrorCamera(false);

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
            {
                UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
                ExitWardrobeMode();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                _isInWardrobeMode = false;
            }
        }

        private void OnDisable()
        {
            if (isInWardrobeMode)
            {
                ExitWardrobeMode();
            }
            _isInWardrobeMode = false;
        }

        #endregion
    }
}