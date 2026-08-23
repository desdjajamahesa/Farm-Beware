using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesWardrobe
{
    public class WardrobeUI : MonoBehaviour
    {
        [Header("Mirror Display")]
        [SerializeField] private RawImage mirrorRawImage; // Full screen background
        [SerializeField] private MirrorCamera mirrorCamera; // Sumber RenderTexture cermin
        [SerializeField] private RenderTexture mirrorRenderTexture; // Referensi langsung ke RT asset

        [Header("Outfit Grid")]
        [SerializeField] private Transform outfitGrid; // GridLayoutGroup
        [SerializeField] private GameObject outfitButtonPrefab;

        [Header("Current Preview")]
        [SerializeField] private Image currentPreviewImage; // Outfit saat ini

        [Header("Actions")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;

        [Header("Visual")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color normalColor = Color.white;

        private List<Button> outfitButtons = new List<Button>();
        private List<OutfitData> outfitSlotData = new List<OutfitData>(); // null = slot Default
        private OutfitData currentlySelectedOutfit;

        private void Awake()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnEnable()
        {
            // FIX BUG layar putih: assign RenderTexture cermin tiap panel diaktifkan.
            RefreshMirrorTexture();
        }

        private void Start()
        {
            // FIX BUG layar putih: assign RenderTexture juga di Start (first activation).
            RefreshMirrorTexture();

            // Subscribe to PlayerOutfit events
            if (WardrobeManager.Instance != null && WardrobeManager.Instance.PlayerOutfitProp != null)
            {
                var outfit = WardrobeManager.Instance.PlayerOutfitProp;
                outfit.OnOutfitChanged += OnOutfitChanged;
                outfit.OnPreviewChanged += OnPreviewChanged;
            }

            BuildOutfitGrid();
            UpdateCurrentPreview();
        }

        /// <summary>Set texture RawImage dari sumber cermin (null-safe). Tanpa ini RawImage render putih.</summary>
        /// <summary>Set texture RawImage dari sumber cermin (null-safe). Tanpa ini RawImage render putih.</summary>
        private void RefreshMirrorTexture()
        {
            if (mirrorRawImage == null)
            {
                Debug.LogWarning("[WardrobeUI] mirrorRawImage null, tidak bisa assign texture.");
                return;
            }

            if (mirrorCamera == null)
                mirrorCamera = FindFirstObjectByType<MirrorCamera>(FindObjectsInactive.Include);

            if (mirrorCamera == null)
            {
                Debug.LogWarning("[WardrobeUI] MirrorCamera tidak ditemukan -> RawImage tetap putih. Wire field 'mirrorCamera' di scene.");
                return;
            }

            if (mirrorCamera.MirrorTexture == null)
            {
                Debug.LogWarning("[WardrobeUI] MirrorCamera.MirrorTexture masih null -> RawImage tetap putih.");
                return;
            }

            // DIRECT ASSIGN: Bypass property getter, assign directly to RawImage
            mirrorRawImage.texture = mirrorCamera.MirrorTexture;
            Debug.Log("[WardrobeUI] MirrorRawImage texture assigned: " + mirrorCamera.MirrorTexture.name);
        }

        /// <summary>Panggil ulang assign texture kapan pun (mis. dari WardrobeManager setelah blend selesai).</summary>
        /// <summary>Panggil ulang assign texture kapan pun (mis. dari WardrobeManager setelah blend selesai).</summary>
        public void ForceRefreshMirror()
        {
            RefreshMirrorTexture();
        }

        /// <summary>Sumber texture: preferensi referensi RT langsung, fallback ke MirrorCamera.MirrorTexture.</summary>
        public Texture MirrorTextureSource =>
            mirrorRenderTexture != null ? mirrorRenderTexture : (mirrorCamera != null ? mirrorCamera.MirrorTexture : null);

        private void Update()
        {
            // Self-heal: selama panel aktif, pastikan texture mirror selalu terpasang.
            // Menangani kasus MirrorCamera meng-recreate/me-release RT setelah OnEnable.
            if (mirrorRawImage != null && mirrorRawImage.texture != MirrorTextureSource)
            {
                Texture current = MirrorTextureSource;
                if (current != null)
                {
                    mirrorRawImage.texture = current;
                    Debug.Log("[WardrobeUI] Update: Re-assigned mirror texture: " + current.name);
                }
            }
        }

        private void OnDestroy()
        {
            if (WardrobeManager.Instance != null && WardrobeManager.Instance.PlayerOutfitProp != null)
            {
                var outfit = WardrobeManager.Instance.PlayerOutfitProp;
                outfit.OnOutfitChanged -= OnOutfitChanged;
                outfit.OnPreviewChanged -= OnPreviewChanged;
            }
        }

        private void BuildOutfitGrid()
        {
            if (outfitGrid == null || outfitButtonPrefab == null) return;

            // Clear existing
            foreach (var btn in outfitButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            outfitButtons.Clear();
            outfitSlotData.Clear();

            var outfit = WardrobeManager.Instance?.PlayerOutfitProp;
            if (outfit == null) return;

            // Slot 0 (kiri-atas) = Default: tanpa outfit -> model player asli.
            outfitButtons.Add(CreateOutfitButton(null, "Default", outfit));
            outfitSlotData.Add(null);

            for (int i = 0; i < outfit.unlockedOutfits.Count; i++)
            {
                var data = outfit.unlockedOutfits[i];
                if (data == null) continue;
                outfitButtons.Add(CreateOutfitButton(data, data.outfitName, outfit));
                outfitSlotData.Add(data);
            }

            SelectOutfitButton(outfit.currentOutfit);
        }

        private Button CreateOutfitButton(OutfitData data, string label, PlayerOutfit outfit)
        {
            var btnGO = Instantiate(outfitButtonPrefab, outfitGrid);
            // FIX BUG layar putih: prefab tombol dibuat SetActive(false) di setup,
            // jadi wajib aktifkan kembali agar grid tampil.
            btnGO.SetActive(true);
            var btn = btnGO.GetComponent<Button>();
            var img = btnGO.GetComponentInChildren<Image>();
            var txt = btnGO.GetComponentInChildren<Text>();

            if (img != null && data != null && data.icon != null)
                img.sprite = data.icon;

            if (txt != null)
                txt.text = label;

            OutfitData captured = data; // null untuk Default
            btn.onClick.AddListener(() =>
            {
                if (captured == null)
                    WardrobeManager.Instance.PreviewDefault();
                else
                    WardrobeManager.Instance.TryOnOutfit(captured);
                SelectOutfitButton(captured);
            });

            return btn;
        }

        private void SelectOutfitButton(OutfitData outfit)
        {
            currentlySelectedOutfit = outfit;

            for (int i = 0; i < outfitButtons.Count; i++)
            {
                var btn = outfitButtons[i];
                var img = btn.GetComponentInChildren<Image>();
                if (img == null) continue;

                // Identitas objek: null == Default, sisanya kecocokan referensi OutfitData.
                bool isSelected = i < outfitSlotData.Count && outfitSlotData[i] == outfit;
                img.color = isSelected ? selectedColor : normalColor;
            }
        }

        private void OnOutfitChanged(OutfitData outfit)
        {
            UpdateCurrentPreview();
            // Update button highlight
            SelectOutfitButton(outfit);
        }

        private void OnPreviewChanged(OutfitData outfit)
        {
            // Update mirror raw image if needed (handled by MirrorCamera)
            // But we can update a small preview thumbnail here
            if (currentPreviewImage != null && outfit != null && outfit.icon != null)
                currentPreviewImage.sprite = outfit.icon;

            // Sinkronkan highlight tombol (termasuk pilihan Default/null).
            SelectOutfitButton(outfit);
        }

        private void UpdateCurrentPreview()
        {
            var outfit = WardrobeManager.Instance?.PlayerOutfitProp?.currentOutfit;
            if (currentPreviewImage != null && outfit != null && outfit.icon != null)
                currentPreviewImage.sprite = outfit.icon;
        }

        private void OnSaveClicked()
        {
            WardrobeManager.Instance.CommitOutfit();
            WardrobeManager.Instance.ExitWardrobeMode();
        }

        private void OnCancelClicked()
        {
            WardrobeManager.Instance.RevertOutfit();
            WardrobeManager.Instance.ExitWardrobeMode();
        }
    }
}