using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesInteraction;
using FeaturesEconomy.PureLogic;

namespace FeaturesEconomy
{
    /// <summary>
    /// HayDay-style Farm Order Board placed outside the perimeter fence.
    /// Players fulfill washed clean crop requests for 1.2x (+20%) market gold.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(WorldLabel))]
    [RequireComponent(typeof(Highlightable))]
    public class OrderBoardInteractable : MonoBehaviour, IInteractable
    {
        public static readonly Vector3 DefaultOutdoorPosition = new Vector3(25.2f, 0f, 49.8f);
        public static readonly Quaternion DefaultOutdoorRotation = Quaternion.Euler(0f, 195f, 0f);

        [Header("Order Board Configuration")]
        [SerializeField] private int orderSlotCount = BoardOrderEvaluator.DefaultOrderCount;
        [SerializeField] private List<BoardOrderSlot> activeOrders = new List<BoardOrderSlot>();

        private int currentGeneratedDay = -1;
        private uint cycleCounter = 1u;
        private bool isTimeSubscribed;

        private static Material sFallbackFrameMat;
        private static Material sFallbackCorkMat;
        private static Material sFallbackSlipMat;
        private static Material sFallbackPinMat;

        public IReadOnlyList<BoardOrderSlot> ActiveOrders => activeOrders;

        public event Action OnOrdersUpdated;

        private void Awake()
        {
            var label = GetComponent<WorldLabel>();
            if (label != null && string.IsNullOrEmpty(label.displayName))
            {
                label.displayName = "Farm Order Board (1.2x Gold)";
            }

            if (activeOrders == null || activeOrders.Count == 0 || ContainsInvalidOrDirtyOrder(activeOrders))
            {
                RefreshOrdersForDay(ResolveCurrentDay());
            }
        }

        private static bool ContainsInvalidOrDirtyOrder(List<BoardOrderSlot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null || !BoardOrderEvaluator.IsCleanCropTemplateId(slots[i].ItemId))
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            TrySubscribeTimeService();
        }

        private void Start()
        {
            TrySubscribeTimeService();
        }

        private void Update()
        {
            if (!isTimeSubscribed)
            {
                TrySubscribeTimeService();
            }
        }

        private void OnDisable()
        {
            if (!isTimeSubscribed) return;

            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null)
            {
                timeService.OnDayChanged -= HandleDayChanged;
            }
            isTimeSubscribed = false;
        }

        private void TrySubscribeTimeService()
        {
            if (isTimeSubscribed) return;

            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService == null) return;

            timeService.OnDayChanged += HandleDayChanged;
            isTimeSubscribed = true;

            if (currentGeneratedDay != Mathf.Max(1, timeService.CurrentDay))
            {
                RefreshOrdersForDay(timeService.CurrentDay);
            }
        }

        private void HandleDayChanged(int newDay)
        {
            RefreshOrdersForDay(newDay);
        }

        private int ResolveCurrentDay()
        {
            var timeService = ServiceLocator.Resolve<ITimeService>();
            return timeService != null ? Mathf.Max(1, timeService.CurrentDay) : 1;
        }

        /// <summary>
        /// Regenerates the pinned request slips for the given game day.
        /// </summary>
        public void RefreshOrdersForDay(int day, uint customSeed = 0u)
        {
            int safeDay = Mathf.Max(1, day);
            currentGeneratedDay = safeDay;
            uint seed = customSeed != 0u ? customSeed : (uint)(safeDay * 10007 + 42197);
            int targetSlots = Mathf.Max(1, orderSlotCount);

            activeOrders = BoardOrderEvaluator.GenerateDailyOrders(safeDay, seed, targetSlots);
            OnOrdersUpdated?.Invoke();
        }

        /// <summary>
        /// Resolves the ItemData asset requested by an order slot via the unified item catalog.
        /// </summary>
        public ItemData ResolveOrderItem(BoardOrderSlot slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.ItemId))
                return null;

            return ItemDatabase.ResolveItem(slot.ItemId);
        }

        /// <summary>
        /// Evaluates current normal vs 1.2x Board Order reward for the given order slot.
        /// </summary>
        public BoardOrderPriceBreakdown GetOrderPricing(BoardOrderSlot slot)
        {
            if (slot == null || slot.RequiredQuantity <= 0)
                return default;

            ItemData item = ResolveOrderItem(slot);
            if (item == null)
                return default;

            int baseUnitPrice = DailyEconomyManager.Instance != null
                ? DailyEconomyManager.Instance.GetCropSellPrice(item)
                : (item.sellPrice > 0 ? item.sellPrice : 50);

            return BoardOrderEvaluator.EvaluatePrice(baseUnitPrice, slot.RequiredQuantity);
        }

        /// <summary>
        /// Atomically verifies inventory, deducts requested crops, pays 1.2x Gold,
        /// records trading telemetry, and replaces the fulfilled slot with a new request.
        /// </summary>
        public bool TryFulfillOrder(
            int slotIndex,
            InventoryComponent playerInventory,
            PlayerWallet wallet,
            out int rewardGold,
            out string statusMessage)
        {
            rewardGold = 0;
            statusMessage = string.Empty;

            if (activeOrders == null || slotIndex < 0 || slotIndex >= activeOrders.Count)
            {
                statusMessage = "Order slot no longer exists.";
                return false;
            }

            if (playerInventory == null)
            {
                statusMessage = "Player inventory unavailable.";
                return false;
            }

            if (wallet == null)
            {
                statusMessage = "Player wallet unavailable.";
                return false;
            }

            BoardOrderSlot slot = activeOrders[slotIndex];
            ItemData item = ResolveOrderItem(slot);
            if (slot == null || item == null || slot.RequiredQuantity <= 0)
            {
                statusMessage = "Requested crop data missing.";
                return false;
            }

            if (item.isDirty || !BoardOrderEvaluator.IsCleanCropTemplateId(item.itemId))
            {
                statusMessage = "Board Orders accept washed clean crops only.";
                return false;
            }

            int owned = playerInventory.CountItem(item);
            if (owned < slot.RequiredQuantity)
            {
                statusMessage = $"Need {slot.RequiredQuantity}x {item.itemName} (Have {owned}).";
                return false;
            }

            BoardOrderPriceBreakdown pricing = GetOrderPricing(slot);
            if (pricing.BoardTotalReward <= 0)
            {
                statusMessage = "Invalid order payout.";
                return false;
            }

            if (!playerInventory.RemoveItem(item, slot.RequiredQuantity))
            {
                statusMessage = "Failed to deliver items from backpack.";
                return false;
            }

            rewardGold = pricing.BoardTotalReward;
            wallet.AddGold(rewardGold);
            DailyEconomyManager.Instance?.RecordCropSold(item, slot.RequiredQuantity, rewardGold);

            cycleCounter++;
            uint rollSeed = (uint)(Mathf.Max(1, currentGeneratedDay) * 65537 + (slotIndex + 1) * 131 + cycleCounter * 977);
            activeOrders[slotIndex] = BoardOrderEvaluator.CreateReplacementOrder(
                slotIndex,
                currentGeneratedDay,
                rollSeed,
                slot.ItemId);

            statusMessage = $"+{rewardGold:N0} G (Delivered {slot.RequiredQuantity}x {item.itemName} at 1.2x!)";
            OnOrdersUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Replaces an unwanted order slip on the board with a new agricultural request.
        /// </summary>
        public bool TryReplaceOrder(int slotIndex)
        {
            if (activeOrders == null || slotIndex < 0 || slotIndex >= activeOrders.Count)
                return false;

            string prevId = activeOrders[slotIndex]?.ItemId;
            cycleCounter++;
            uint rollSeed = (uint)(Mathf.Max(1, currentGeneratedDay) * 32749 + (slotIndex + 3) * 811 + cycleCounter * 1223);
            activeOrders[slotIndex] = BoardOrderEvaluator.CreateReplacementOrder(
                slotIndex,
                currentGeneratedDay,
                rollSeed,
                prevId);

            OnOrdersUpdated?.Invoke();
            return true;
        }

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            int currentDay = ResolveDayForInteraction();
            if (currentDay < 1)
            {
                ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                    transform.position + Vector3.up * 1.6f,
                    "Rest first. Board orders arrive tomorrow.",
                    new Color(1f, 0.8f, 0.4f));
                return;
            }

            EnsureOrderBoardUI();
            if (OrderBoardUI.Instance != null)
            {
                OrderBoardUI.Instance.OpenBoard(this);
            }
            else
            {
                Debug.LogWarning("[OrderBoardInteractable] OrderBoardUI could not be initialized.");
            }
        }

        private static int ResolveDayForInteraction()
        {
            var timeService = ServiceLocator.Resolve<ITimeService>();
            return timeService != null ? timeService.CurrentDay : 1;
        }

        private static void EnsureOrderBoardUI()
        {
            if (OrderBoardUI.Instance != null) return;

            var existing = FindFirstObjectByType<OrderBoardUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            Canvas screenCanvas = FindOverlayCanvas();
            if (screenCanvas != null)
            {
                var boardUIGo = new GameObject("OrderBoardUI_Panel", typeof(OrderBoardUI));
                boardUIGo.transform.SetParent(screenCanvas.transform, false);
            }
        }

        private static Canvas FindOverlayCanvas()
        {
            var uiCanvasGO = GameObject.Find("UI_Canvas");
            if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var c) && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }

            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cv in allCanvases)
            {
                if (cv != null && cv.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return cv;
                }
            }
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOutdoorOrderBoardOnSceneLoad()
        {
            if (FindFirstObjectByType<OrderBoardInteractable>(FindObjectsInactive.Include) != null)
                return;

            var boardGO = CreateOutdoorOrderBoard(DefaultOutdoorPosition, DefaultOutdoorRotation);
            var stations = GameObject.Find("InteractiveStations");
            if (stations != null)
            {
                boardGO.transform.SetParent(stations.transform, true);
            }
        }

        /// <summary>
        /// Constructs the 3D wooden Order Board outside the perimeter fence with pinned order slips,
        /// collider, WorldLabel, Highlightable, and OrderBoardInteractable component.
        /// </summary>
        public static GameObject CreateOutdoorOrderBoard(
            Vector3 position,
            Quaternion rotation,
            Material frameMat = null,
            Material corkMat = null,
            Material slipMat = null,
            Material pinMat = null,
            Material highlightMat = null)
        {
            GameObject root = new GameObject("OrderBoard_Outdoor");
            root.transform.position = position;
            root.transform.rotation = rotation;

            ResolveFallbackMaterials(ref frameMat, ref corkMat, ref slipMat, ref pinMat, ref highlightMat);

            CreateBoardPart(root.transform, "Post_Left", PrimitiveType.Cube, new Vector3(-0.92f, 1.05f, 0f), new Vector3(0.14f, 2.10f, 0.14f), Quaternion.identity, frameMat);
            CreateBoardPart(root.transform, "Post_Right", PrimitiveType.Cube, new Vector3(0.92f, 1.05f, 0f), new Vector3(0.14f, 2.10f, 0.14f), Quaternion.identity, frameMat);
            CreateBoardPart(root.transform, "Frame_Top", PrimitiveType.Cube, new Vector3(0f, 2.02f, 0f), new Vector3(2.14f, 0.18f, 0.24f), Quaternion.identity, frameMat);
            CreateBoardPart(root.transform, "Frame_Bottom", PrimitiveType.Cube, new Vector3(0f, 0.88f, 0f), new Vector3(1.94f, 0.12f, 0.16f), Quaternion.identity, frameMat);
            CreateBoardPart(root.transform, "Roof_SlantFront", PrimitiveType.Cube, new Vector3(0f, 2.16f, 0.11f), new Vector3(2.26f, 0.08f, 0.28f), Quaternion.Euler(22f, 0f, 0f), frameMat);
            CreateBoardPart(root.transform, "Roof_SlantBack", PrimitiveType.Cube, new Vector3(0f, 2.16f, -0.11f), new Vector3(2.26f, 0.08f, 0.28f), Quaternion.Euler(-22f, 0f, 0f), frameMat);
            CreateBoardPart(root.transform, "CorkBacking", PrimitiveType.Cube, new Vector3(0f, 1.45f, 0f), new Vector3(1.78f, 1.04f, 0.08f), Quaternion.identity, corkMat);

            // 4 pinned parchment slips across the front face (+Z local)
            Vector3[] slipOffsets =
            {
                new Vector3(-0.46f, 1.68f, 0.048f),
                new Vector3(0.46f, 1.66f, 0.048f),
                new Vector3(-0.44f, 1.21f, 0.048f),
                new Vector3(0.45f, 1.22f, 0.048f)
            };
            float[] slipTilts = { -3f, 2.5f, 2f, -2.5f };

            for (int i = 0; i < slipOffsets.Length; i++)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, slipTilts[i]);
                CreateBoardPart(root.transform, $"OrderSlip_{i}", PrimitiveType.Cube, slipOffsets[i], new Vector3(0.56f, 0.36f, 0.02f), rot, slipMat);
                Vector3 pinPos = slipOffsets[i] + new Vector3(0f, 0.13f, 0.014f);
                CreateBoardPart(root.transform, $"OrderPin_{i}", PrimitiveType.Sphere, pinPos, new Vector3(0.055f, 0.055f, 0.04f), Quaternion.identity, pinMat);
            }

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 2.2f, 0.7f);
            col.center = new Vector3(0f, 1.1f, 0f);

            var label = root.AddComponent<WorldLabel>();
            label.displayName = "Farm Order Board (1.2x Gold)";

            var highlightable = root.AddComponent<Highlightable>();
            if (highlightMat != null)
            {
                highlightable.SetHighlightMaterial(highlightMat);
            }

            var board = root.AddComponent<OrderBoardInteractable>();
            board.RefreshOrdersForDay(ResolveDayForInteraction());

            return root;
        }

        private static void CreateBoardPart(
            Transform parent,
            string partName,
            PrimitiveType primitive,
            Vector3 localPos,
            Vector3 localScale,
            Quaternion localRot,
            Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = partName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null && mat != null)
            {
                rend.sharedMaterial = mat;
            }

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }
        }

        private static void ResolveFallbackMaterials(
            ref Material frameMat,
            ref Material corkMat,
            ref Material slipMat,
            ref Material pinMat,
            ref Material highlightMat)
        {
            Shader litShader = ShaderFallbackResolver.ResolveLitShader();

            if (frameMat == null)
            {
                frameMat = FindSceneWoodMaterial() ?? GetOrCreateFallbackMaterial(ref sFallbackFrameMat, "OrderBoard_FrameWoodMat", litShader, new Color(0.42f, 0.27f, 0.15f, 1f));
            }
            if (corkMat == null)
            {
                corkMat = GetOrCreateFallbackMaterial(ref sFallbackCorkMat, "OrderBoard_CorkMat", litShader, new Color(0.72f, 0.56f, 0.36f, 1f));
            }
            if (slipMat == null)
            {
                slipMat = GetOrCreateFallbackMaterial(ref sFallbackSlipMat, "OrderBoard_SlipPaperMat", litShader, new Color(0.96f, 0.92f, 0.78f, 1f));
            }
            if (pinMat == null)
            {
                pinMat = GetOrCreateFallbackMaterial(ref sFallbackPinMat, "OrderBoard_PinRedMat", litShader, new Color(0.82f, 0.22f, 0.18f, 1f));
            }
            if (highlightMat == null)
            {
                highlightMat = FindSceneHighlightMaterial();
            }
        }

        private static Material GetOrCreateFallbackMaterial(ref Material slot, string matName, Shader shader, Color color)
        {
            if (slot != null) return slot;
            if (shader == null) return null;

            slot = new Material(shader)
            {
                name = matName,
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            return slot;
        }

        private static Material FindSceneWoodMaterial()
        {
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.IndexOf("Wood", StringComparison.OrdinalIgnoreCase) >= 0)
                    return r.sharedMaterial;
            }
            return null;
        }

        private static Material FindSceneHighlightMaterial()
        {
            var mat = Resources.Load<Material>("Mat_Highlight");
            if (mat != null) return mat;

            var existingHighlightables = FindObjectsByType<Highlightable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var field = typeof(Highlightable).GetField("highlightMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return null;

            foreach (var h in existingHighlightables)
            {
                if (h == null) continue;
                var candidate = field.GetValue(h) as Material;
                if (candidate != null) return candidate;
            }
            return null;
        }
    }
}
