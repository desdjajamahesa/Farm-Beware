using System.Collections.Generic;
using FeaturesInteraction;
using UnityEngine;

namespace FeaturesEconomy
{
    /// <summary>
    /// Interactable table allowing players to buy seeds and sell harvested crops.
    /// Interacting with this table opens the MerchantShopUI.
    /// Also provides an automatic spawner to guarantee an outdoor merchant table exists in the farm yard.
    /// </summary>
    [RequireComponent(typeof(WorldLabel))]
    public class MerchantTableInteractable : MonoBehaviour, IInteractable
    {
        [Header("Merchant Catalog")]
        [Tooltip("Custom items for sale at this table. If empty, all seeds are loaded automatically.")]
        [SerializeField] private List<ItemData> stockItems = new List<ItemData>();

        private void Awake()
        {
            var label = GetComponent<WorldLabel>();
            if (label != null && string.IsNullOrEmpty(label.displayName))
            {
                label.displayName = "Merchant Table (Trade)";
            }
        }

        public void Interact(GameObject interactor)
        {
            if (MerchantShopUI.Instance == null)
            {
                // Search for existing panel in scene first
                var existing = FindFirstObjectByType<MerchantShopUI>(FindObjectsInactive.Include);
                if (existing != null)
                {
                    existing.gameObject.SetActive(true);
                }
                else
                {
                    // Ensure UI manager exists on UI_Canvas
                    var uiCanvasGO = GameObject.Find("UI_Canvas");
                    Canvas screenCanvas = null;
                    if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var c) && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        screenCanvas = c;
                    }
                    else
                    {
                        var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (var cv in allCanvases)
                        {
                            if (cv != null && cv.renderMode == RenderMode.ScreenSpaceOverlay)
                            {
                                screenCanvas = cv;
                                break;
                            }
                        }
                    }

                    if (screenCanvas != null)
                    {
                        var shopManagerGO = new GameObject("MerchantShopUI_Panel", typeof(MerchantShopUI));
                        shopManagerGO.transform.SetParent(screenCanvas.transform, false);
                    }
                }
            }

            if (MerchantShopUI.Instance != null)
            {
                MerchantShopUI.Instance.OpenShop(stockItems);
            }
            else
            {
                Debug.LogWarning("[MerchantTableInteractable] MerchantShopUI could not be found or initialized!");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOutdoorTableOnSceneLoad()
        {
            var outdoor = GameObject.Find("MerchantTable_Outdoor");
            if (outdoor == null)
            {
                // Spawns right outside the house door leading to the farm (x: 18.5, y: 0.05, z: 27.5)
                CreateOutdoorMerchantTable(new Vector3(18.5f, 0.05f, 27.5f));
            }
        }

        /// <summary>
        /// Creates a clean rustic merchant table outside the house with collider, label, and interactable.
        /// </summary>
        public static GameObject CreateOutdoorMerchantTable(Vector3 position)
        {
            GameObject tableGO = new GameObject("MerchantTable_Outdoor");
            tableGO.transform.position = position;
            tableGO.transform.rotation = Quaternion.Euler(0f, 15f, 0f);

            // Find existing wood material from scene or create a warm brown wood material
            Material woodMat = null;
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.ToLower().Contains("wood"))
                {
                    woodMat = r.sharedMaterial;
                    break;
                }
            }

            if (woodMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                woodMat = new Material(shader)
                {
                    name = "MerchantTable_WoodMat",
                    color = new Color(0.48f, 0.32f, 0.18f, 1f)
                };
            }

            // 1. Table Top Planks
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "TableTop";
            top.transform.SetParent(tableGO.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            top.transform.localScale = new Vector3(2.2f, 0.14f, 1.2f);
            if (woodMat != null) top.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            Destroy(top.GetComponent<Collider>());

            // 2. Table Cloth / Runner (gives it a distinct Merchant Shop look)
            GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloth.name = "ClothRunner";
            cloth.transform.SetParent(tableGO.transform, false);
            cloth.transform.localPosition = new Vector3(0f, 0.84f, 0f);
            cloth.transform.localScale = new Vector3(0.9f, 0.16f, 1.22f);
            var clothShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var clothMat = new Material(clothShader)
            {
                name = "MerchantClothMat",
                color = new Color(0.18f, 0.55f, 0.42f, 1f) // Forest Teal Cloth
            };
            cloth.GetComponent<MeshRenderer>().sharedMaterial = clothMat;
            Destroy(cloth.GetComponent<Collider>());

            // 3. Four Legs
            Vector3[] legPositions = new Vector3[]
            {
                new Vector3(-0.92f, 0.38f, -0.45f),
                new Vector3(0.92f, 0.38f, -0.45f),
                new Vector3(-0.92f, 0.38f, 0.45f),
                new Vector3(0.92f, 0.38f, 0.45f)
            };

            foreach (var legPos in legPositions)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "Leg";
                leg.transform.SetParent(tableGO.transform, false);
                leg.transform.localPosition = legPos;
                leg.transform.localScale = new Vector3(0.15f, 0.76f, 0.15f);
                if (woodMat != null) leg.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                Destroy(leg.GetComponent<Collider>());
            }

            // 4. Box Collider for player walk collision and raycast interaction
            var col = tableGO.AddComponent<BoxCollider>();
            col.size = new Vector3(2.4f, 1.2f, 1.4f);
            col.center = new Vector3(0f, 0.55f, 0f);

            // 5. WorldLabel ("Merchant Table (Trade)")
            var label = tableGO.AddComponent<WorldLabel>();
            label.displayName = "Merchant Table (Trade)";

            // 6. Highlightable (with highlight material auto-resolved)
            var highlightable = tableGO.AddComponent<Highlightable>();

            // Coba cari material highlight dari Highlightable lain di scene
            Material highlightMat = null;
            var existingHighlightables = FindObjectsByType<Highlightable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var h in existingHighlightables)
            {
                if (h != highlightable)
                {
                    // Akses field private via refleksi untuk menyalin material
                    var field = typeof(Highlightable).GetField("highlightMaterial",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var mat = field.GetValue(h) as Material;
                        if (mat != null)
                        {
                            highlightMat = mat;
                            break;
                        }
                    }
                }
            }

            if (highlightMat != null)
                highlightable.SetHighlightMaterial(highlightMat);

            // 7. MerchantTableInteractable
            tableGO.AddComponent<MerchantTableInteractable>();

            return tableGO;
        }
    }
}
