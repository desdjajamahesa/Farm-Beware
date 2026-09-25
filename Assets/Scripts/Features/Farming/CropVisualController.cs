using UnityEngine;
using FeaturesFarming;

namespace FeaturesFarming
{
    /// <summary>
    /// Mengelola representasi visual 3D dari petak tanah dan tanaman:
    /// 1. Tampilan tanah (Untilled, Tilled Dry, Tilled Wet).
    /// 2. Tahap pertumbuhan visual tanaman (Sprout, Growing, Mature Sweet Potato, Mature Taro).
    /// </summary>
    [ExecuteAlways]
    public class CropVisualController : MonoBehaviour
    {
        [Header("Soil Mesh & Materials")]
        [SerializeField] private MeshRenderer soilRenderer;
        [SerializeField] private Material matUntilled;
        [SerializeField] private Material matTilledDry;
        [SerializeField] private Material matTilledWet;

        [Header("Crop Visual Roots")]
        [SerializeField] private Transform cropContainer;
        [SerializeField] private GameObject sproutVisual;
        [SerializeField] private GameObject growingVisual;
        [SerializeField] private GameObject matureSweetPotatoVisual;
        [SerializeField] private GameObject matureTaroVisual;
        [SerializeField] private GameObject matureCornVisual;

        [Header("Juice & Feedback")]
        [SerializeField] private float bounceSpeed = 3f;
        [SerializeField] private float bounceHeight = 0.05f;

        private Vector3 initialCropPos;
        private bool isReadyToHarvest;

        private void Awake()
        {
            if (cropContainer != null)
                initialCropPos = cropContainer.localPosition;
            else
                initialCropPos = new Vector3(0f, 0.1f, 0f);

            EnsureDefaultVisuals();
        }

        private void Update()
        {
            if (isReadyToHarvest && cropContainer != null && Application.isPlaying)
            {
                // Efek subtle bobbing/pantul lembut ketika tanaman sudah siap panen
                float yOffset = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
                cropContainer.localPosition = initialCropPos + new Vector3(0f, yOffset, 0f);
            }
        }

        public void UpdateVisuals(TileState state, SeedItemData seed, float progress)
        {
            EnsureDefaultVisuals();
            isReadyToHarvest = (state == TileState.ReadyToHarvest);

            // 1. Update Material Tanah
            UpdateSoilMaterial(state);

            // 2. Update Visual Tanaman
            if (state == TileState.Untilled || state == TileState.Tilled || seed == null)
            {
                HideAllCrops();
                if (cropContainer != null)
                    cropContainer.localPosition = initialCropPos;
                return;
            }

            // Sembunyikan semua dulu
            HideAllCrops();

            bool isTaro = seed.itemName.ToLower().Contains("taro") || seed.itemId.ToLower().Contains("taro");
            bool isCorn = seed.itemName.ToLower().Contains("corn") || seed.itemId.ToLower().Contains("corn");

            if (state == TileState.ReadyToHarvest || progress >= 1f)
            {
                // Tahap 3: Matang (Mature)
                if (isCorn)
                {
                    if (matureCornVisual != null)
                        matureCornVisual.SetActive(true);
                    else if (matureSweetPotatoVisual != null)
                        matureSweetPotatoVisual.SetActive(true);
                }
                else if (isTaro)
                {
                    if (matureTaroVisual != null) matureTaroVisual.SetActive(true);
                }
                else
                {
                    if (matureSweetPotatoVisual != null) matureSweetPotatoVisual.SetActive(true);
                }
            }
            else if (progress >= 0.4f)
            {
                // Tahap 2: Tumbuh Sedang (Growing)
                if (growingVisual != null)
                {
                    growingVisual.SetActive(true);
                    float scale = Mathf.Lerp(0.7f, 1.1f, (progress - 0.4f) / 0.6f);
                    growingVisual.transform.localScale = Vector3.one * scale;
                }
            }
            else
            {
                // Tahap 1: Tunas Baru (Sprout)
                if (sproutVisual != null)
                {
                    sproutVisual.SetActive(true);
                    float scale = Mathf.Lerp(0.5f, 1f, progress / 0.4f);
                    sproutVisual.transform.localScale = Vector3.one * scale;
                }
            }
        }

        private void UpdateSoilMaterial(TileState state)
        {
            if (soilRenderer == null) return;

            Material targetMat = matUntilled;
            switch (state)
            {
                case TileState.Untilled:
                    targetMat = matUntilled != null ? matUntilled : soilRenderer.sharedMaterial;
                    break;
                case TileState.Tilled:
                case TileState.PlantedDry:
                    targetMat = matTilledDry != null ? matTilledDry : soilRenderer.sharedMaterial;
                    break;
                case TileState.PlantedWatered:
                case TileState.ReadyToHarvest:
                    targetMat = matTilledWet != null ? matTilledWet : (matTilledDry != null ? matTilledDry : soilRenderer.sharedMaterial);
                    break;
            }

            if (targetMat != null)
            {
                soilRenderer.sharedMaterial = targetMat;

                // Sinkronkan cache material pada Highlightable agar tidak me-revert ke untilled saat un-hover
                var highlight = GetComponent<FeaturesInteraction.Highlightable>() ?? GetComponentInParent<FeaturesInteraction.Highlightable>();
                if (highlight != null)
                {
                    highlight.UpdateOriginalMaterial(soilRenderer, targetMat);
                }
            }
        }

        private void HideAllCrops()
        {
            if (sproutVisual != null) sproutVisual.SetActive(false);
            if (growingVisual != null) growingVisual.SetActive(false);
            if (matureSweetPotatoVisual != null) matureSweetPotatoVisual.SetActive(false);
            if (matureTaroVisual != null) matureTaroVisual.SetActive(false);
        }

        /// <summary>
        /// Membuat atau mereferensikan mesh default bila belum ada di hierarki.
        /// </summary>
        public void EnsureDefaultVisuals()
        {
            if (soilRenderer == null)
            {
                var mesh = GetComponentInChildren<MeshRenderer>();
                if (mesh != null) soilRenderer = mesh;
            }

            if (cropContainer == null)
            {
                var found = transform.Find("CropContainer");
                if (found != null)
                    cropContainer = found;
                else
                {
                    var go = new GameObject("CropContainer");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    cropContainer = go.transform;
                }
            }

            // Pastikan child visuals terhubung bila ada di dalam CropContainer
            if (cropContainer != null)
            {
                if (sproutVisual == null)
                {
                    var t = cropContainer.Find("Sprout");
                    if (t != null) sproutVisual = t.gameObject;
                }
                if (growingVisual == null)
                {
                    var t = cropContainer.Find("Growing");
                    if (t != null) growingVisual = t.gameObject;
                }
                if (matureSweetPotatoVisual == null)
                {
                    var t = cropContainer.Find("Mature_SweetPotato");
                    if (t != null) matureSweetPotatoVisual = t.gameObject;
                }
                if (matureTaroVisual == null)
                {
                    var t = cropContainer.Find("Mature_Taro");
                    if (t != null) matureTaroVisual = t.gameObject;
                }
                if (matureCornVisual == null)
                {
                    var t = cropContainer.Find("Mature_Corn");
                    if (t != null) matureCornVisual = t.gameObject;
                }
            }
        }

        public void SetMaterials(Material untilled, Material tilledDry, Material tilledWet)
        {
            matUntilled = untilled;
            matTilledDry = tilledDry;
            matTilledWet = tilledWet;
        }

        public void SetCropVisualObjects(GameObject sprout, GameObject growing, GameObject sweetPotato, GameObject taro)
        {
            sproutVisual = sprout;
            growingVisual = growing;
            matureSweetPotatoVisual = sweetPotato;
            matureTaroVisual = taro;
        }
    }
}
