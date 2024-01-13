using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    // 从给的颜色缓冲复制到目标颜色缓冲
    public class CopyColorPass : ScriptableRenderPass
    {
        int m_SampleOffsetShaderHandle;
        Material m_SamplingMaterial;
        Downsampling m_DownsamplingMethod;
        Material m_CopyColorMaterial;

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }

        public CopyColorPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial = null)
        {
            base.profilingSampler = new ProfilingSampler(nameof(CopyColorPass));

            m_SamplingMaterial = samplingMaterial;
            m_CopyColorMaterial = copyColorMaterial;
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
            renderPassEvent = evt;
            m_DownsamplingMethod = Downsampling.None;
            base.useNativeRenderPass = false;
        }

        // 在UniversalRenderer.Setup时设置来源和目标
        // source来自renderer.m_ActiveCameraColorAttachment
        // destination来自source来自renderer.m_OpaqueColor
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
        {
            this.source = source;
            this.destination = destination;
            m_DownsamplingMethod = downsampling;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            if (m_DownsamplingMethod == Downsampling._2xBilinear)
            {
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
            else if (m_DownsamplingMethod == Downsampling._4xBox || m_DownsamplingMethod == Downsampling._4xBilinear)
            {
                descriptor.width /= 4;
                descriptor.height /= 4;
            }

            cmd.GetTemporaryRT(destination.id, descriptor, m_DownsamplingMethod == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_SamplingMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_SamplingMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //如果source是frontbuffer，直接取cameraColorTarget
            if (source == renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                source = renderingData.cameraData.renderer.cameraColorTarget;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyColor)))
            {
                RenderTargetIdentifier opaqueColorRT = destination.Identifier();

                ScriptableRenderer.SetRenderTarget(cmd, opaqueColorRT, BuiltinRenderTextureType.CameraTarget, clearFlag,
                    clearColor);

                bool useDrawProceduleBlit = renderingData.cameraData.xr.enabled;
                switch (m_DownsamplingMethod)
                {
                    case Downsampling.None:
                        RenderingUtils.Blit(cmd, source, opaqueColorRT, m_CopyColorMaterial, 0, useDrawProceduleBlit);
                        break;
                    case Downsampling._2xBilinear:
                        RenderingUtils.Blit(cmd, source, opaqueColorRT, m_CopyColorMaterial, 0, useDrawProceduleBlit);
                        break;
                    case Downsampling._4xBox:
                        m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                        RenderingUtils.Blit(cmd, source, opaqueColorRT, m_SamplingMaterial, 0, useDrawProceduleBlit);
                        break;
                    case Downsampling._4xBilinear:
                        RenderingUtils.Blit(cmd, source, opaqueColorRT, m_CopyColorMaterial, 0, useDrawProceduleBlit);
                        break;
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
