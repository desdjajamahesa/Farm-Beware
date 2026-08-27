using FeaturesCamera;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using FarmBeware.Logic;

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
        
        public PlayerOutfit PlayerOutfitProp => playerOutfit;
        [SerializeField] private Transform playerHead;

        [Header("UI")]
        [SerializeField] private GameObject wardrobeUIPanel;
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private WardrobeUI wardrobeUI;

        [Header("Wardrobe Items Data")]
        [Tooltip("All available wardrobe items organized by category.")]
        [SerializeField] private List<FarmBeware.Logic.WardrobeItemData> allWardrobeItems = new List<FarmBeware.Logic.WardrobeItemData>();

        [Header("Preview System")]
        [Tooltip("Reference to the PreviewController for 3D avatar preview.")]
        [SerializeField] private PreviewController previewController;

        [Header("Debug")]
        [SerializeField] private bool debugCameraAudit = false;

        private bool isInWardrobeMode;
        private Coroutine fadeCoroutine;
        private Vector3 playerOriginalPosition;
        private Quaternion playerOriginalRotation;

        #region Public API

        public bool IsInWardrobeMode => isInWardrobeMode;

        public void EnterWardrobeMode()
        {
            if (isInWardrobeMode) return;

            isInWardrobeMode = true;

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

            playerOriginalPosition = playerControl.transform.position;
            playerOriginalRotation = playerControl.transform.rotation;

            // Position player in front of mirror
            PositionPlayerToMirror();

            // Initialize MirrorCamera BEFORE camera mode switch
            if (mirrorCamera != null)
            {
                mirrorCamera.EnsureInitialized();
                if (playerHead != null)
                    mirrorCamera.SetPlayerTarget(playerHead);
            }

            // Delegate camera switching to CameraManager (preferred)
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

            // Enable MirrorInnerCam (renders to RawImage texture)
            if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
            {
                mirrorCamera.MirrorCameraComponent.enabled = true;
                Debug.Log("[WardrobeManager] MirrorInnerCam enabled");
            }

            // UI fade in
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeUI(true));

            if (wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(true);

            SetUIRaycastBlocking(true);

            // Hide hotbar while in wardrobe (same pattern as trophy cabinet mode)
            if (InventoryManagerUI.Instance != null && InventoryManagerUI.Instance.playerHotbarContainer != null)
                InventoryManagerUI.Instance.playerHotbarContainer.gameObject.SetActive(false);

            if (wardrobeUI != null && previewController != null)
            {
                // Refresh preview camera texture binding
                var previewRawImage = wardrobeUI.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
                if (previewRawImage != null)
                    previewController.BindToRawImage(previewRawImage);
            }
            LogMirrorDiagnostics();

            Debug.Log("[WardrobeManager] Entered Wardrobe Mode");
        }

        public void ExitWardrobeMode()
        {
            if (!isInWardrobeMode) return;

            if (playerOutfit != null && playerOutfit.IsPreviewing)
                playerOutfit.Revert();

            isInWardrobeMode = false;

            // Disable MirrorInnerCam before camera switch
            if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
            {
                mirrorCamera.MirrorCameraComponent.enabled = false;
                Debug.Log("[WardrobeManager] MirrorInnerCam disabled");
            }

            // Delegate camera switching to CameraManager (preferred)
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetMode(CameraManager.CameraMode.Gameplay);
            }
            else
            {
                // FALLBACK: Manual camera control
                if (wardrobeCamera != null)
                    wardrobeCamera.enabled = false;

                if (mainCamera != null)
                    mainCamera.enabled = true;

                // Unlock input, keep cursor free
                if (playerControl != null)
                    playerControl.isInputLocked = false;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Debug.Log("[WardrobeManager] Fallback: Restored main camera");
            }

            // Re-enable MirrorInnerCam for mirror surface (gameplay)
            if (mirrorCamera != null && mirrorCamera.MirrorTexture != null && mirrorCamera.MirrorCameraComponent != null)
            {
                mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                mirrorCamera.MirrorCameraComponent.enabled = true;
                Debug.Log("[WardrobeManager] MirrorInnerCam re-enabled for mirror surface");
            }

            // UI fade out
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeUI(false));

            if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
            if (wardrobeUIPanel != null) wardrobeUIPanel.SetActive(false);

            SetUIRaycastBlocking(false);

            // Restore hotbar (unless trophy cabinet mode owns it — it hides hotbar itself)
            bool trophyOwnsHotbar = InventoryManagerUI.Instance != null &&
                InventoryManagerUI.Instance.currentStorageInventory != null &&
                TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode;
            if (!trophyOwnsHotbar && InventoryManagerUI.Instance != null &&
                InventoryManagerUI.Instance.playerHotbarContainer != null)
                InventoryManagerUI.Instance.playerHotbarContainer.gameObject.SetActive(true);

            Debug.Log("[WardrobeManager] Exited Wardrobe Mode");
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
            Texture rt = wardrobeUI != null && wardrobeUI.previewController != null
                ? wardrobeUI.previewController.PreviewRenderTexture
                : null;
            bool mirrorReady = mirrorCamera != null && mirrorCamera.MirrorTexture != null;
            bool innerCamOn = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null && mirrorCamera.MirrorCameraComponent.enabled;
            bool targetOk = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null &&
                            mirrorCamera.MirrorCameraComponent.targetTexture == mirrorCamera.MirrorTexture;
            Debug.Log($"[Wardrobe] diag -> RawImage.texture={(rt != null ? rt.name : "NULL")} " +
                      $"| MirrorTexture={(mirrorReady ? "OK" : "NULL")} " +
                      $"| InnerCam.enabled={innerCamOn} " +
                      $"| targetTexture==RT={targetOk}");
        }

        #endregion

        #region Wardrobe Items Initialization

        private void InitializeWardrobeItems()
        {
            if (wardrobeUI == null) return;

            // If we have items assigned in the inspector, organize them by category
            if (allWardrobeItems != null && allWardrobeItems.Count > 0)
            {
                var itemsByCategory = new Dictionary<FarmBeware.Logic.OutfitPartResolver.Category, List<FarmBeware.Logic.WardrobeItemData>>();

                foreach (var item in allWardrobeItems)
                {
                    if (item == null) continue;

                    var cat = item.category;
                    if (!itemsByCategory.ContainsKey(cat))
                        itemsByCategory[cat] = new List<FarmBeware.Logic.WardrobeItemData>();

                    itemsByCategory[cat].Add(item);
                }

                // Register with UI
                foreach (var kvp in itemsByCategory)
                {
                    wardrobeUI.RegisterCategoryItems(kvp.Key, kvp.Value);
                }
            }
            else
            {
                // Fallback: auto-populate from PlayerOutfit's unlockedOutfits
                if (playerOutfit != null && playerOutfit.unlockedOutfits != null && playerOutfit.unlockedOutfits.Count > 0)
                {
                    PopulateItemsFromUnlockedOutfits();
                }
            }

            // Bind preview controller if available
            if (previewController != null && wardrobeUI != null)
            {
                var previewRawImage = wardrobeUI.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
                if (previewRawImage != null)
                    previewController.BindToRawImage(previewRawImage);
            }
        }

        private void PopulateItemsFromUnlockedOutfits()
        {
            if (playerOutfit == null || playerOutfit.unlockedOutfits == null) return;

            var itemsByCategory = new Dictionary<FarmBeware.Logic.OutfitPartResolver.Category, List<FarmBeware.Logic.WardrobeItemData>>();

            foreach (var outfit in playerOutfit.unlockedOutfits)
            {
                if (outfit == null) continue;

                // Create WardrobeItemData from each OutfitData
                // This is a simplified mapping - in production you'd have proper item definitions
                CreateItemFromOutfit(outfit, itemsByCategory);
            }

            foreach (var kvp in itemsByCategory)
            {
                wardrobeUI.RegisterCategoryItems(kvp.Key, kvp.Value);
            }
        }

        private void CreateItemFromOutfit(OutfitData outfit, Dictionary<FarmBeware.Logic.OutfitPartResolver.Category, List<FarmBeware.Logic.WardrobeItemData>> itemsByCategory)
        {
            // Map OutfitData variants to individual WardrobeItemData
            var categories = System.Enum.GetValues(typeof(FarmBeware.Logic.OutfitPartResolver.Category));
            foreach (FarmBeware.Logic.OutfitPartResolver.Category cat in categories)
            {
                if (!itemsByCategory.ContainsKey(cat))
                    itemsByCategory[cat] = new List<FarmBeware.Logic.WardrobeItemData>();

                int variantCount = FarmBeware.Logic.OutfitPartResolver.GetVariantCount(cat);
                for (int i = 0; i < variantCount; i++)
                {
                    var itemData = ScriptableObject.CreateInstance<FarmBeware.Logic.WardrobeItemData>();
                    itemData.itemId = $"{outfit.outfitName}_{cat}_{i}";
                    itemData.displayName = $"{cat} {FarmBeware.Logic.OutfitPartResolver.GetVariantLabel(cat, i)}";
                    itemData.icon = outfit.icon;
                    itemData.category = cat;
                    itemData.description = $"From outfit: {outfit.outfitName}";

                    itemsByCategory[cat].Add(itemData);
                }
            }
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

            // Enforce initial state: UI hidden, mirror cam bound to RT only.
            // Scene files can persist stale active-states from a previous session.
            if (wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(false);
            if (uiCanvasGroup != null)
                uiCanvasGroup.alpha = 0f;
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