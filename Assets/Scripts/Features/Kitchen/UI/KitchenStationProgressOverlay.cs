using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Overlay progress dunia (ala Minecraft) untuk Stove & Kitchen Sink.
/// MURNI visual: subscribe ke event KitchenStation, tidak menyimpan state gameplay.
/// Visual: "veil" putih transparan yang NAIK dari bawah ke atas mengikuti progress
/// real-time (OnProcessProgress per frame), billboard menghadap kamera,
/// plus flash + bunyi saat selesai.
/// Backend tetap sumber kebenaran (timer ada di KitchenStation).
/// </summary>
public class KitchenStationProgressOverlay : MonoBehaviour
{
    [Tooltip("Stasiun yang dipantau (Sink / Kompor). Jika kosong, memakai komponen sejenis pada GameObject ini.")]
    [SerializeField] private KitchenStation station;

    [Tooltip("Dasar overlay per slot (urutan = index slot). Stove: Burner_1 & Burner_2. Kosong = fallback ke atas Renderer.")]
    [SerializeField] private Transform[] slotAnchors;

    [Tooltip("Material overlay transparan (URP Unlit, alpha rendah).")]
    [SerializeField] private Material overlayMaterial;

    [Header("Tampilan")]
    [Tooltip("Warna dasar overlay.")]
    [SerializeField] private Color overlayColor = new Color(1f, 1f, 1f, 0.5f);
    [Tooltip("Lebar overlay dalam satuan dunia.")]
    [SerializeField] private float overlayWidth = 0.9f;
    [Tooltip("Tinggi maksimum overlay saat progress penuh (satuan dunia).")]
    [SerializeField] private float maxHeight = 0.7f;
    [Tooltip("Bila true, overlay menghadap kamera (billboard, terkunci sumbu Y = tetap naik ke atas).")]
    [SerializeField] private bool useBillboard = true;
    [Tooltip("Tag kamera utama untuk billboard.")]
    [SerializeField] private string billboardCameraTag = "MainCamera";

    [Header("Feedback Selesai")]
    [Tooltip("Lama panel 'Selesai!' tampil sebelum overlay disembunyikan.")]
    [SerializeField] private float hideDelayAfterComplete = 1.2f;
    [Tooltip("Durasi efek flash alpha saat selesai.")]
    [SerializeField] private float flashDuration = 0.25f;
    [Tooltip("Klip suara saat selesai (kosong = pop prosedural dibuat otomatis).")]
    [SerializeField] private AudioClip completeSound;
    [Tooltip("Volume suara selesai.")]
    [SerializeField] private float soundVolume = 0.8f;
    [Tooltip("Jeda minimum antar bunyi agar beberapa slot yang selesai bersamaan tidak bunyi dobel.")]
    [SerializeField] private float soundCooldown = 0.5f;

    private class SlotOverlay
    {
        public Transform root;          // pivot tetap di dasar slot (kompensasi skala parent)
        public Transform quad;          // quad anak: posisi.y = tinggi/2, skala.y = tinggi
        public MeshRenderer renderer;
        public bool started;            // sedang proses aktif
        public Coroutine hideRoutine;
        public Color defaultColor;
    }

    private readonly Dictionary<int, SlotOverlay> overlays = new Dictionary<int, SlotOverlay>();
    private AudioSource audioSource;
    private AudioClip generatedPop;
    private Camera cachedCamera;
    private float lastSoundTime = -100f;
    private bool warnedMissing;

    private void OnEnable()
    {
        if (station == null)
            station = GetComponent<KitchenStation>();

        if (station == null)
        {
            if (!warnedMissing)
            {
                Debug.LogWarning("[KitchenStationProgressOverlay] station tidak ditemukan di '" + gameObject.name + "'. Overlay dinonaktifkan.", this);
                warnedMissing = true;
            }
            return;
        }

        BuildOverlays();
        HideAll();

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": " + overlays.Count + " overlay dibuat. " + BuildOverlayPositionReport(), this);

        station.OnProcessStarted += OnProcessStarted;
        station.OnProcessProgress += OnProcessProgress;
        station.OnProcessCompleted += OnProcessCompleted;
        station.OnProcessCancelled += OnProcessCancelled;
    }

    private void OnDisable()
    {
        if (station == null)
            return;

        station.OnProcessStarted -= OnProcessStarted;
        station.OnProcessProgress -= OnProcessProgress;
        station.OnProcessCompleted -= OnProcessCompleted;
        station.OnProcessCancelled -= OnProcessCancelled;

        HideAll();
    }

    private void Update()
    {
        // Lapis kedua (mandiri dari event): sinkron dari state backend setiap frame.
        // Menyembuhkan kasus event terlewat / stasiun aktif di tengah proses.
        if (station == null || overlays.Count == 0)
            return;

        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null || o.quad == null)
                continue;

            // Jangan ganggu saat flash "Selesai!" sedang berjalan.
            if (o.hideRoutine != null)
                continue;

            int slot = pair.Key;
            if (station.IsProcessing(slot))
            {
                if (!o.root.gameObject.activeSelf)
                {
                    o.started = true;
                    o.root.gameObject.SetActive(true);
                }
                SetVeilHeight(o, maxHeight * station.GetSlotProgress(slot));
            }
            else if (o.started || o.root.gameObject.activeSelf)
            {
                // Proses berhenti tanpa event (mis. re-enable di tengah) -> sembunyikan.
                o.started = false;
                o.root.gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (!useBillboard || overlays.Count == 0)
            return;

        Camera cam = FindActiveCamera();
        if (cam == null)
            return;

        bool dirty = false;
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null || o.root == null || !o.root.gameObject.activeSelf)
                continue;

            Vector3 flat = cam.transform.position - o.root.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                o.root.rotation = Quaternion.LookRotation(flat);
                dirty = true;
            }
        }
        if (dirty)
            cachedCamera = cam;
    }

    private void BuildOverlays()
    {
        if (overlays.Count > 0)
            return;

        int count = (slotAnchors != null && slotAnchors.Length > 0) ? slotAnchors.Length : 1;
        Vector3 fallbackAnchor = ComputeFallbackAnchor();

        for (int i = 0; i < count; i++)
        {
            Transform anchor = (slotAnchors != null && i < slotAnchors.Length && slotAnchors[i] != null)
                ? slotAnchors[i]
                : null;

            Vector3 worldPos = anchor != null ? anchor.position : fallbackAnchor;

            // Root pivot: kompensasi skala non-uniform parent agar ukuran quad = satuan dunia.
            GameObject rootGO = new GameObject("ProgressOverlay_" + i);
            Transform root = rootGO.transform;
            root.SetParent(transform, false);
            Vector3 s = transform.lossyScale;
            if (s.x != 0f && s.y != 0f && s.z != 0f)
                root.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            root.position = worldPos;

            // Quad veil.
            GameObject quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGO.name = "Veil_" + i;
            Collider col = quadGO.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            Transform quad = quadGO.transform;
            quad.SetParent(root, false);
            quad.localPosition = Vector3.zero;
            quad.localRotation = Quaternion.identity;
            quad.localScale = new Vector3(overlayWidth, 0.0001f, 1f);

            MeshRenderer mesh = quadGO.GetComponent<MeshRenderer>();
            mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mesh.receiveShadows = false;
            mesh.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            if (overlayMaterial != null) { mesh.material = overlayMaterial; mesh.material.renderQueue = 4000; } else if (!warnedMissing)
            {
                Debug.LogWarning("[KitchenStationProgressOverlay] overlayMaterial kosong di '" + gameObject.name + "'. Veil tampil apa adanya.", this);
                warnedMissing = true;
            }
            Color color = (overlayMaterial != null) ? overlayColor : Color.white;
            mesh.material.color = color;

            SlotOverlay slot = new SlotOverlay();
            slot.root = root;
            slot.quad = quad;
            slot.renderer = mesh;
            slot.started = false;
            slot.defaultColor = color;

            rootGO.SetActive(false);
            quadGO.SetActive(true);
            overlays[i] = slot;
        }
    }

    private Vector3 ComputeFallbackAnchor()
    {
        Vector3 pos = transform.position;
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Bounds b = r.bounds;
            pos = new Vector3(b.center.x, b.max.y, b.max.z);
        }
        return pos;
    }

    private void OnProcessStarted(int slotIndex, float duration)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null || o.quad == null)
            return;

        if (o.hideRoutine != null)
        {
            StopCoroutine(o.hideRoutine);
            o.hideRoutine = null;
        }

        o.started = true;
        o.root.gameObject.SetActive(true);
        SetVeilHeight(o, 0f);

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": proses MULAI slot " + slotIndex + " (durasi " + duration + "s)", this);
    }

    private void OnProcessProgress(int slotIndex, float progress01)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null || o.quad == null)
            return;

        // Guard anti-lost: bila proses sudah berjalan sebelum enable / event Started terlewat.
        if (!o.started || !o.root.gameObject.activeSelf)
        {
            o.started = true;
            o.root.gameObject.SetActive(true);
        }

        float height = maxHeight * Mathf.Clamp01(progress01);
        SetVeilHeight(o, height);
    }

    private void OnProcessCompleted(int slotIndex)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null || o.quad == null)
            return;

        o.root.gameObject.SetActive(true);
        SetVeilHeight(o, maxHeight);
        PlayCompleteSound();

        if (o.hideRoutine != null)
            StopCoroutine(o.hideRoutine);
        o.hideRoutine = StartCoroutine(FlashThenHideRoutine(o));

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": proses SELESAI slot " + slotIndex, this);
    }

    private void OnProcessCancelled(int slotIndex)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null)
            return;

        o.started = false;
        if (o.hideRoutine != null)
        {
            StopCoroutine(o.hideRoutine);
            o.hideRoutine = null;
        }
        SetVeilHeight(o, 0f);
        o.root.gameObject.SetActive(false);

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": proses DIBATALKAN slot " + slotIndex, this);
    }

    private IEnumerator FlashThenHideRoutine(SlotOverlay o)
    {
        // Flash cepat: alpha berdenyut 1 -> baseAlpha -> 1 selama flashDuration.
        float t = 0f;
        while (t < flashDuration && o.renderer != null)
        {
            Color c = o.defaultColor;
            c.a = Mathf.Lerp(1f, o.defaultColor.a, Mathf.PingPong(t * (2f / flashDuration), 1f));
            o.renderer.material.color = c;
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(hideDelayAfterComplete);

        if (o.renderer != null)
            o.renderer.material.color = o.defaultColor;
        SetVeilHeight(o, 0f);
        if (o.root != null)
            o.root.gameObject.SetActive(false);
        o.started = false;
        o.hideRoutine = null;
    }

    private void SetVeilHeight(SlotOverlay o, float height)
    {
        if (o.quad == null)
            return;

        float h = Mathf.Max(height, 0.0001f);
        o.quad.localScale = new Vector3(overlayWidth, h, 1f);
        // Quad ber-pivot di tengah -> geser setengah tinggi agar TEPI BAWAH tetap di dasar (naik dari bawah ke atas).
        o.quad.localPosition = new Vector3(0f, h * 0.5f, 0f);
    }

    private string BuildOverlayPositionReport()
    {
        string report = "";
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null || o.root == null)
                continue;
            report += " [slot " + pair.Key + "]" + o.root.position.ToString("F2");
        }
        return report;
    }

    private SlotOverlay GetOverlay(int slotIndex)
    {
        if (overlays.Count == 0)
            return null;

        if (overlays.TryGetValue(slotIndex, out SlotOverlay o))
            return o;

        // Slot di luar jumlah anchor -> gunakan anchor dengan index terbesar (robust).
        int lastIndex = -1;
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            if (pair.Key > lastIndex)
                lastIndex = pair.Key;
        }

        if (lastIndex >= 0 && overlays.TryGetValue(lastIndex, out SlotOverlay fallback))
            return fallback;
        return null;
    }

    private void HideAll()
    {
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null)
                continue;
            if (o.hideRoutine != null && isActiveAndEnabled)
                StopCoroutine(o.hideRoutine);
            o.hideRoutine = null;
            o.started = false;
            if (o.renderer != null)
                o.renderer.material.color = o.defaultColor;
            if (o.root != null)
                o.root.gameObject.SetActive(false);
        }
    }

    private void PlayCompleteSound()
    {
        if (Time.time - lastSoundTime < soundCooldown || soundVolume <= 0f)
            return;

        lastSoundTime = Time.time;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (completeSound != null)
        {
            audioSource.PlayOneShot(completeSound, soundVolume);
            return;
        }

        if (generatedPop == null)
            generatedPop = CreatePopClip();
        if (generatedPop != null)
            audioSource.PlayOneShot(generatedPop, soundVolume);
    }

    private static AudioClip CreatePopClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.14f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(1400f, 400f, t / duration);
            float envelope = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ProgressPop", samples, 1, sampleRate, false);
        if (clip != null)
            clip.SetData(data, 0);
        return clip;
    }

    private Camera FindActiveCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        Camera[] all = Camera.allCameras;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].isActiveAndEnabled)
                return all[i];
        }
        return null;
    }
}