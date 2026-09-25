using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace FeaturesRendering.Vision
{
    /// <summary>
    /// Custom URP ScriptableRendererFeature for World-Space Vision Mask post-processing.
    /// Compatible with Unity 6 URP RenderGraph and legacy fallback.
    /// </summary>
    [DisallowMultipleRendererFeature("World Space Vision Feature")]
    [Tooltip("Applies a world-space distance mask to desaturate and darken areas outside character vision.")]
    public class WorldSpaceVisionFeature : ScriptableRendererFeature
    {
        [Header("Shader & Material")]
        [SerializeField] private Shader visionShader;

        [Header("Pass Configuration")]
        [Tooltip("Execution point in URP pipeline. Defaults to BeforeRenderingPostProcessing.")]
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private Material m_Material;
        private WorldSpaceVisionPass m_VisionPass;

        public override void Create()
        {
            if (visionShader == null)
            {
                visionShader = Shader.Find("PostProcess/WorldSpaceVisionMask");
            }

            if (visionShader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(visionShader);
            }

            m_VisionPass = new WorldSpaceVisionPass(m_Material, passEvent);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Scoping & Early Exit: Do not schedule pass if VisionManager is absent or inactive (e.g. MainMenu or Daytime)
            if (VisionManager.Instance == null || !VisionManager.Instance.IsVisionActive)
                return;

            // Camera filtering: only execute for Game and SceneView cameras
            CameraType camType = renderingData.cameraData.cameraType;
            if (camType != CameraType.Game && camType != CameraType.SceneView)
                return;

            if (m_Material == null)
            {
                if (visionShader == null)
                    visionShader = Shader.Find("PostProcess/WorldSpaceVisionMask");

                if (visionShader != null)
                    m_Material = CoreUtils.CreateEngineMaterial(visionShader);
                else
                    return;

                m_VisionPass.UpdateMaterial(m_Material);
            }

            m_VisionPass.renderPassEvent = passEvent;
            renderer.EnqueuePass(m_VisionPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_VisionPass?.Dispose();
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }

        #region Render Pass Implementation
        private class WorldSpaceVisionPass : ScriptableRenderPass
        {
            private Material m_PassMaterial;
            private const string ProfilerTag = "WorldSpaceVisionMask";

            public WorldSpaceVisionPass(Material material, RenderPassEvent evt)
            {
                m_PassMaterial = material;
                renderPassEvent = evt;
            }

            public void UpdateMaterial(Material material)
            {
                m_PassMaterial = material;
            }

            private class PassData
            {
                public Material material;
                public TextureHandle source;
            }

            /// <summary>
            /// Modern RenderGraph implementation for Unity 6 / URP 17+.
            /// </summary>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (m_PassMaterial == null) return;

                // Scoping & Early Exit: Do not schedule pass if VisionManager is absent or inactive
                if (VisionManager.Instance == null || !VisionManager.Instance.IsVisionActive)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // Camera filtering: only process Game and SceneView cameras
                if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView)
                    return;

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid()) return;

                // Create intermediate texture for post-process blit
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, 
                    desc, 
                    "_VisionMaskIntermediate", 
                    false, 
                    FilterMode.Bilinear, 
                    TextureWrapMode.Clamp
                );

                // Pass 1: Blit camera color through vision mask material into intermediate texture
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VisionMask_Process", out var passData))
                {
                    passData.material = m_PassMaterial;
                    passData.source = source;

                    builder.UseTexture(source, AccessFlags.Read);

                    // Ensure camera depth texture is available for world position reconstruction
                    if (resourceData.cameraDepthTexture.IsValid())
                    {
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    }

                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, 0);
                    });
                }

                // Pass 2: Blit intermediate texture back to active color target
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VisionMask_BlitBack", out var passData))
                {
                    passData.material = null;
                    passData.source = destination;

                    builder.UseTexture(destination, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), 0, false);
                    });
                }
            }

            private RTHandle m_LegacyTempHandle;

            public void Dispose()
            {
                m_LegacyTempHandle?.Release();
                m_LegacyTempHandle = null;
            }

            /// <summary>
            /// Legacy fallback execution path when RenderGraph is disabled / compatibility mode is used.
            /// </summary>
            [Obsolete("Fallback for non-RenderGraph compatibility mode in URP.", false)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_PassMaterial == null) return;

                // Scoping & Early Exit: Do not schedule pass if VisionManager is absent or inactive
                if (VisionManager.Instance == null || !VisionManager.Instance.IsVisionActive)
                    return;

                CameraType camType = renderingData.cameraData.cameraType;
                if (camType != CameraType.Game && camType != CameraType.SceneView)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                RenderingUtils.ReAllocateHandleIfNeeded(ref m_LegacyTempHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_VisionMaskLegacyTemp");

                Blitter.BlitCameraTexture(cmd, source, m_LegacyTempHandle, m_PassMaterial, 0);
                Blitter.BlitCameraTexture(cmd, m_LegacyTempHandle, source);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
        #endregion
    }
}
