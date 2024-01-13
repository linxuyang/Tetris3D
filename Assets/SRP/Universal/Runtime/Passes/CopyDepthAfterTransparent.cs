using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CopyDepthAfterTransparent : ScriptableRendererFeature
{
    private const int UI_LAYER = 5;
    private CopyDepthPass pass;
    
    public override void Create()
    {
        pass = new CopyDepthPass();
        pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        int layer = renderingData.cameraData.camera.gameObject.layer;
        CameraType cameraType = renderingData.cameraData.camera.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection || layer == UI_LAYER)
        {
            return;
        }
        renderer.EnqueuePass(pass);
    }

    class CopyDepthPass : ScriptableRenderPass
    {
        static readonly int TRANSPARENT_DEPTH = Shader.PropertyToID("_CameraTransparentDepthTexture");
        RenderTargetIdentifier transparentDepthRT = new RenderTargetIdentifier(TRANSPARENT_DEPTH);
        private Material copyDepthMat;

        private Material CopyMaterial
        {
            get
            {
                if (copyDepthMat == null)
                {
                    copyDepthMat = new Material(Shader.Find("Hidden/Universal Render Pipeline/CopyDepth"));
                }

                return copyDepthMat;
            }
        }
        private RenderTextureDescriptor targetDes;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 32;
            descriptor.msaaSamples = 1;
            targetDes = descriptor;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("CopyDepthAfterTransparent");
            
            cmd.GetTemporaryRT(TRANSPARENT_DEPTH, targetDes);
            cmd.SetRenderTarget(transparentDepthRT);
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                int cameraSamples = descriptor.msaaSamples;

                // When auto resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampleAutoResolve || SystemInfo.supportsMultisampledTextures == 0)
                    cameraSamples = 1;

                CameraData cameraData = renderingData.cameraData;

                switch (cameraSamples)
                {
                    case 8:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 4:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 2:
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;
                }
                {
                    float flipSign = (cameraData.IsCameraProjectionMatrixFlipped()) ? -1.0f : 1.0f;
                    Vector4 scaleBiasRt = (flipSign < 0.0f)
                        ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                        : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(Shader.PropertyToID("_ScaleBiasRt"), scaleBiasRt);

                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, CopyMaterial);
                }
            cmd.SetGlobalTexture(TRANSPARENT_DEPTH, transparentDepthRT);
            context.ExecuteCommandBuffer(cmd);
            
            CommandBufferPool.Release(cmd);
        }
    }
}