# Render Graph Memory & Buffer Validation Guidelines (Unity 6 URP 17+)

Dokumentasi arsitektur dan pedoman validasi manajemen memori buffer grafis untuk **Farm-Beware** pada Unity 6000.3.20f1 Universal Render Pipeline (URP).

---

## 1. Latar Belakang & Paradigma Render Graph

Pada versi URP sebelum Unity 6, manipulasi render target seringkali menggunakan `RTHandle` persisten (yang dialokasikan via `RTHandles.Alloc()`) atau `RenderTargetHandle` usang. Pola lama ini menimbulkan masalah mendasar:
1. **Risiko Memory Leak**: Developer lupa memanggil `.Release()` pada `RTHandle` saat transisi scene, object destroy, atau perubahan resolusi layar.
2. **Ketiadaan Memory Aliasing**: Buffer yang tidak aktif tetap memakan alokasi VRAM secara eksklusif, menghalangi GPU meminjam memori yang sama untuk pass lain.
3. **Implicit Sync Hazards**: GPU synchronization barrier disisipkan secara manual atau tidak optimal.

Dalam **Unity 6 URP (17.3+)**, eksekusi rendering sepenuhnya beralih ke arsitektur **Render Graph API**. Render Graph bertindak sebagai compiler yang menganalisis seluruh dependency pass, menjadwalkan alokasi, melakukan **Memory Aliasing** otomatis, dan mengeliminasi overhead CPU.

---

## 2. Aturan Mutlak Penggunaan Buffer di Farm-Beware

### ❌ ATURAN 1: Dilarang Menggunakan `RTHandle` Persisten di Dalam Custom Pass
Jangan pernah mengalokasikan `RTHandle` di dalam metode `RecordRenderGraph()` atau menyimpan referensi tekstur transien di tingkat kelas (field class) antar frame.

```csharp
// ❌ PROHIBITED (Pola Usang & Rawan Memory Leak)
private RTHandle m_LeakingTexture;

public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    // Alokasi runtime tanpa manajemen Render Graph
    RenderingUtils.ReAllocateIfNeeded(ref m_LeakingTexture, desc, name: "_LeakingTarget");
}
```

### ✅ ATURAN 2: Wajib Menggunakan `TextureHandle` Transien
Gunakan `TextureHandle` dari namespace `UnityEngine.Rendering.RenderGraphModule`. `TextureHandle` adalah representasi lightweight (handle) yang validitasnya dikelola secara otomatis oleh Render Graph compiler hanya untuk frame aktif.

```csharp
// ✅ COMPLIANT (Unity 6 URP Render Graph)
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
    UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

    // 1. Ambil source handle dari pipeline frameData
    TextureHandle source = resourceData.activeColorTexture;
    if (!source.IsValid()) return;

    // 2. Buat target transien via UniversalRenderer helper
    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
    desc.depthBufferBits = 0;
    
    TextureHandle intermediate = UniversalRenderer.CreateRenderGraphTexture(
        renderGraph,
        desc,
        "_IntermediateColor",
        clear: false,
        filterMode: FilterMode.Bilinear,
        wrapMode: TextureWrapMode.Clamp
    );

    // 3. Daftarkan Raster Render Pass dengan PassData terisolasi
    using (var builder = renderGraph.AddRasterRenderPass<PassData>("CustomPass_Blit", out var passData))
    {
        passData.source = source;
        passData.material = m_Material;

        // Deklarasikan akses ke Render Graph compiler
        builder.UseTexture(source, AccessFlags.Read);
        builder.SetRenderAttachment(intermediate, 0, AccessFlags.Write);

        builder.SetRenderFunc<PassData>((data, context) =>
        {
            // Blit via Blitter API
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
        });
    }
}
```

---

## 3. Checklist Validasi Arsitektur Kode URP 6

| No | Kriteria Validasi | Status & Solusi |
|---|---|---|
| 1 | **Tipe Handle** | Wajib `TextureHandle`. Jika menemukan `RTHandle` atau `RenderTargetIdentifier`, ganti ke `TextureHandle`. |
| 2 | **Akses Frame Data** | Gunakan `ContextContainer` (`frameData.Get<UniversalResourceData>()`, `frameData.Get<UniversalCameraData>()`). Hindari membaca `renderingData.cameraData` di luar `RecordRenderGraph`. |
| 3 | **Alokasi Tekstur** | Gunakan `UniversalRenderer.CreateRenderGraphTexture(renderGraph, ...)` agar memori di-recycle otomatis via render graph memory pool. |
| 4 | **Deklarasi Akses Resource** | Setiap tekstur yang dibaca wajib didaftarkan dengan `builder.UseTexture(handle, AccessFlags.Read)`. Tekstur tujuan render target wajib didaftarkan dengan `builder.SetRenderAttachment(...)`. |
| 5 | **Tidak Ada State Caching Antar Frame** | `TextureHandle` tidak boleh disimpan di variabel statis atau instance field untuk digunakan di frame berikutnya (`TextureHandle` menjadi invalid segera setelah frame selesai dieksekusi). |

---

## 4. Referensi Implementasi Nyata di Proyek

Pola standar di atas telah terimplementasi dan dapat dijadikan contoh acuan pada file:
- [`WorldSpaceVisionFeature.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Rendering/Vision/WorldSpaceVisionFeature.cs) (Line 103 - 165)
  - Menggunakan `RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)`
  - Mengambil `resourceData.activeColorTexture`
  - Mengalokasikan target via `UniversalRenderer.CreateRenderGraphTexture`
  - Mendaftarkan raster render pass `VisionMask_Process` dengan `AccessFlags.Read` dan `AccessFlags.Write`.
