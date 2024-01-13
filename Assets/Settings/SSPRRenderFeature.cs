using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRRenderFeature : ScriptableRendererFeature
{
    private const int UI_LAYER = 5;
    public PassSettings settings = new PassSettings();
    SSPRRenderPass renderPass;
    
    public override void Create()
    {
        renderPass = new SSPRRenderPass(settings);

        renderPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        int layer = renderingData.cameraData.camera.gameObject.layer;
        CameraType cameraType = renderingData.cameraData.camera.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection || layer == UI_LAYER)
        {
            return;
        }
        renderer.EnqueuePass(renderPass);
    }
    
    [Serializable]
    public class PassSettings
    {
        public ComputeShader computeShader;
        /// <summary>
        /// 水平反射平面的高度(世界空间)
        /// </summary>
        public float reflectionPlaneHeightWS = 0;

        /// <summary>
        /// 倒影在屏幕空间垂直方向的淡出速率(以屏幕中央为原点)
        /// </summary>
        [Range(0.01f, 5)] public float fadeOutScreenBorderWidthVerticle = 0.01f;

        /// <summary>
        /// 倒影在屏幕空间水平方向的淡出速率(以屏幕中央为原点)
        /// </summary>
        [Range(0.01f, 5)] public float fadeOutScreenBorderWidthHorizontal = 0.01f;

        /// <summary>
        /// 倒影水平拉伸区域限制
        /// </summary>
        [Range(-1f, 1f)] public float screenLRStretchThreshold = 0.7f;

        /// <summary>
        /// 倒影水平拉伸强度
        /// </summary>
        [Range(0, 8f)] public float screenLRStretchIntensity = 0f;

        /// <summary>
        /// 叠加颜色
        /// </summary>
        [ColorUsage(false, false)] public Color tintColor = Color.white;

        /// <summary>
        /// 倒影分辨率缩放倍数
        /// </summary>
        [Range(1, 4)] public int renderScale = 4;

        /// <summary>
        /// 是否修复倒影空洞(有性能开销)
        /// </summary>
        public bool applyFillHoleFix = true;

        /// <summary>
        /// 修复抖动(有性能开销)
        /// </summary>
        public bool shouldRemoveFlickerFinalControl = true;

        /// <summary>
        /// 自动平台适配，仅在Debug时关闭该选项，其它情况一律开启
        /// </summary>
        public bool enablePerPlatformAutoSafeGuard = true;
    }

    public class SSPRRenderPass : ScriptableRenderPass
    {
        static readonly int SSPR_RT = Shader.PropertyToID("_SSPR_RT");
        static readonly int _SSPR_PackedDataRT_pid = Shader.PropertyToID("_MobileSSPR_PackedDataRT");
        static readonly int _SSPR_PosWSyRT_pid = Shader.PropertyToID("_MobileSSPR_PosWSyRT");
        RenderTargetIdentifier _SSPR_ColorRT_rti = new RenderTargetIdentifier(SSPR_RT);
        RenderTargetIdentifier _SSPR_PackedDataRT_rti = new RenderTargetIdentifier(_SSPR_PackedDataRT_pid);
        RenderTargetIdentifier _SSPR_PosWSyRT_rti = new RenderTargetIdentifier(_SSPR_PosWSyRT_pid);

        // ShaderTagId lightMode_SSPR_sti = new ShaderTagId("MobileSSPR");

        const int SHADER_NUMTHREAD_X = 8;
        const int SHADER_NUMTHREAD_Y = 8;

        PassSettings settings;

        public SSPRRenderPass(PassSettings settings)
        {
            this.settings = settings;
        }

        void GetRTSize(out int rtWidth, out int rtHeight)
        {
            rtWidth = Screen.width / settings.renderScale;
            rtHeight = Screen.height / settings.renderScale;
            rtWidth = Mathf.CeilToInt(rtWidth / (float) SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
            rtHeight = Mathf.CeilToInt(rtHeight / (float) SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
        }

        bool ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve()
        {
            if (settings.enablePerPlatformAutoSafeGuard)
            {
                //if RInt RT is not supported, use mobile path
                if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RInt))
                    return true;

                //tested Metal(even on a Mac) can't use InterlockedMin().
                //so if metal, use mobile path
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                    return true;
#if UNITY_EDITOR
                //PC(DirectX) can use RenderTextureFormat.RInt + InterlockedMin() without any problem, use Non-Mobile path.
                //Non-Mobile path will NOT produce any flickering
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
                    return false;
#elif UNITY_ANDROID
                //- samsung galaxy A70(Adreno612) will fail if use RenderTextureFormat.RInt + InterlockedMin() in compute shader
                //- but Lenovo S5(Adreno506) is correct, WTF???
                //because behavior is different between android devices, we assume all android are not safe to use RenderTextureFormat.RInt + InterlockedMin() in compute shader
                //so android always go mobile path
                return true;
#endif
            }

            //let user decide if we still don't know the correct answer
            return !settings.shouldRemoveFlickerFinalControl;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            GetRTSize(out int rtWidth, out int rtHeight);
            RenderTextureDescriptor descriptor =
                new RenderTextureDescriptor(rtWidth, rtHeight, RenderTextureFormat.Default, 0, 0);

            descriptor.sRGB = false;
            descriptor.enableRandomWrite = true;
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            cmd.GetTemporaryRT(SSPR_RT, descriptor);

            if (ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve())
            {
                descriptor.colorFormat = RenderTextureFormat.RFloat;
                cmd.GetTemporaryRT(_SSPR_PosWSyRT_pid, descriptor);
            }
            else
            {
                descriptor.colorFormat = RenderTextureFormat.RInt;
                cmd.GetTemporaryRT(_SSPR_PackedDataRT_pid, descriptor);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SSPR");
            GetRTSize(out int rtWidth, out int rtHeight);
            int dispatchThreadGroupXCount = rtWidth / SHADER_NUMTHREAD_X;
            int dispatchThreadGroupYCount = rtHeight / SHADER_NUMTHREAD_Y;
            int dispatchThreadGroupZCount = 1;
            
            Camera camera = renderingData.cameraData.camera;

            cmd.SetComputeVectorParam(settings.computeShader, Shader.PropertyToID("_RTSize"), new Vector2(rtWidth, rtHeight));
            cmd.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_HorizontalPlaneHeightWS"),
                settings.reflectionPlaneHeightWS);

            cmd.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_FadeOutScreenBorderWidthVerticle"),
                settings.fadeOutScreenBorderWidthVerticle);
            cmd.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_FadeOutScreenBorderWidthHorizontal"),
                settings.fadeOutScreenBorderWidthHorizontal);
            cmd.SetComputeVectorParam(settings.computeShader, Shader.PropertyToID("_CameraDirection"),
                camera.transform.forward);
            cmd.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_ScreenLRStretchIntensity"),
                settings.screenLRStretchIntensity);
            cmd.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_ScreenLRStretchThreshold"),
                settings.screenLRStretchThreshold);
            cmd.SetComputeVectorParam(settings.computeShader, Shader.PropertyToID("_FinalTintColor"), settings.tintColor);

            //we found that on metal, UNITY_MATRIX_VP is not correct, so we will pass our own VP matrix to compute shader
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
                
            Matrix4x4 VP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * viewMatrix;
            cmd.SetComputeMatrixParam(settings.computeShader, "_VPMatrix", VP);
                
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
            Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
            // 原版的反转相机观察矩阵UNITY_MATRIX_I_VP在更新版本的URP中不一样了, 需要在外部计算好该矩阵传进computeShader
            cmd.SetComputeMatrixParam(settings.computeShader, "_CustomInvCameraViewProj", invViewProjMatrix);

            if (ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve())
            {
                ////////////////////////////////////////////////
                //Mobile Path (Android GLES / Metal)
                ////////////////////////////////////////////////

                //kernel MobilePathsinglePassColorRTDirectResolve
                int kernel_MobilePathSinglePassColorRTDirectResolve =
                    settings.computeShader.FindKernel("MobilePathSinglePassColorRTDirectResolve");
                cmd.SetComputeTextureParam(settings.computeShader, kernel_MobilePathSinglePassColorRTDirectResolve, "ColorRT",
                    _SSPR_ColorRT_rti);
                cmd.SetComputeTextureParam(settings.computeShader, kernel_MobilePathSinglePassColorRTDirectResolve, "PosWSyRT",
                    _SSPR_PosWSyRT_rti);
                cmd.SetComputeTextureParam(settings.computeShader, kernel_MobilePathSinglePassColorRTDirectResolve,
                    "_CameraOpaqueTexture",
                    new RenderTargetIdentifier("_CameraOpaqueTexture"));
                cmd.SetComputeTextureParam(settings.computeShader, kernel_MobilePathSinglePassColorRTDirectResolve,
                    "_CameraDepthTexture",
                    new RenderTargetIdentifier("_CameraDepthTexture"));
                cmd.DispatchCompute(settings.computeShader, kernel_MobilePathSinglePassColorRTDirectResolve,
                    dispatchThreadGroupXCount,
                    dispatchThreadGroupYCount, dispatchThreadGroupZCount);
            }
            else
            {
                ////////////////////////////////////////////////
                //Non-Mobile Path (PC/console)
                ////////////////////////////////////////////////

                //kernel NonMobilePathClear
                int kernel_NonMobilePathClear = settings.computeShader.FindKernel("NonMobilePathClear");
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathClear, "HashRT", _SSPR_PackedDataRT_rti);
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathClear, "ColorRT", _SSPR_ColorRT_rti);
                cmd.DispatchCompute(settings.computeShader, kernel_NonMobilePathClear, dispatchThreadGroupXCount,
                    dispatchThreadGroupYCount,
                    dispatchThreadGroupZCount);

                //kernel NonMobilePathRenderHashRT
                int kernel_NonMobilePathRenderHashRT = settings.computeShader.FindKernel("NonMobilePathRenderHashRT");
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathRenderHashRT, "HashRT",
                    _SSPR_PackedDataRT_rti);
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathRenderHashRT, "_CameraDepthTexture",
                    new RenderTargetIdentifier("_CameraDepthTexture"));

                cmd.DispatchCompute(settings.computeShader, kernel_NonMobilePathRenderHashRT, dispatchThreadGroupXCount,
                    dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                //resolve to ColorRT
                int kernel_NonMobilePathResolveColorRT = settings.computeShader.FindKernel("NonMobilePathResolveColorRT");
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathResolveColorRT, "_CameraOpaqueTexture",
                    new RenderTargetIdentifier("_CameraOpaqueTexture"));
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathResolveColorRT, "ColorRT",
                    _SSPR_ColorRT_rti);
                cmd.SetComputeTextureParam(settings.computeShader, kernel_NonMobilePathResolveColorRT, "HashRT",
                    _SSPR_PackedDataRT_rti);
                cmd.DispatchCompute(settings.computeShader, kernel_NonMobilePathResolveColorRT, dispatchThreadGroupXCount,
                    dispatchThreadGroupYCount, dispatchThreadGroupZCount);
            }

            //optional shared pass to improve result only: fill RT hole
            if (settings.applyFillHoleFix)
            {
                int kernel_FillHoles = settings.computeShader.FindKernel("FillHoles");
                cmd.SetComputeTextureParam(settings.computeShader, kernel_FillHoles, "ColorRT", _SSPR_ColorRT_rti);
                cmd.SetComputeTextureParam(settings.computeShader, kernel_FillHoles, "PackedDataRT", _SSPR_PackedDataRT_rti);
                cmd.DispatchCompute(settings.computeShader, kernel_FillHoles, Mathf.CeilToInt(dispatchThreadGroupXCount / 2f),
                    Mathf.CeilToInt(dispatchThreadGroupYCount / 2f), dispatchThreadGroupZCount);
            }

            //send out to global, for user's shader to sample reflection result RT (_MobileSSPR_ColorRT)
            //where _MobileSSPR_ColorRT's rgb is reflection color, a is reflection usage 0~1 for user's shader to lerp with fallback reflection probe's rgb
            cmd.SetGlobalTexture(SSPR_RT, _SSPR_ColorRT_rti);


            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // DrawingSettings drawingSettings =
            //     CreateDrawingSettings(lightMode_SSPR_sti, ref renderingData, SortingCriteria.CommonOpaque);
            // FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            // context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
        
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(SSPR_RT);

            if(ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve())
                cmd.ReleaseTemporaryRT(_SSPR_PosWSyRT_pid);
            else
                cmd.ReleaseTemporaryRT(_SSPR_PackedDataRT_pid);
        }
    }

}