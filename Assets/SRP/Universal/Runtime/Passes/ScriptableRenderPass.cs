using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // Input requirements
    [Flags]
    public enum ScriptableRenderPassInput
    {
        None = 0,
        Depth = 1 << 0,
        Normal = 1 << 1,
        Color = 1 << 2,
        Motion = 1 << 3
    }

    // 渲染pass事件，用于控制pass执行顺序
    public enum RenderPassEvent
    {
        BeforeRendering = 0,
        BeforeRenderingShadows = 50,
        AfterRenderingShadows = 100,
        BeforeRenderingPrePasses = 150,
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete, to match the capital from 'Prepass' to 'PrePass' (UnityUpgradable) -> BeforeRenderingPrePasses")]
        BeforeRenderingPrepasses = 151,
        AfterRenderingPrePasses = 200,
        BeforeRenderingGbuffer = 210,
        AfterRenderingGbuffer = 220,
        BeforeRenderingDeferredLights = 230,
        AfterRenderingDeferredLights = 240,
        BeforeRenderingOpaques = 250,
        AfterRenderingOpaques = 300,
        BeforeRenderingSkybox = 350,
        AfterRenderingSkybox = 400,
        BeforeRenderingTransparents = 450,
        AfterRenderingTransparents = 500,
        BeforeRenderingPostProcessing = 550,
        AfterRenderingPostProcessing = 600,
        AfterRendering = 1000,
    }

    /// <summary>
    /// 各种pass的基类
    /// </summary>
    public abstract partial class ScriptableRenderPass
    {
        public RenderPassEvent renderPassEvent { get; set; }

        public RenderTargetIdentifier[] colorAttachments
        {
            get => m_ColorAttachments;
        }

        public RenderTargetIdentifier colorAttachment
        {
            get => m_ColorAttachments[0];
        }

        public RenderTargetIdentifier depthAttachment
        {
            get => m_DepthAttachment;
        }

        public RenderBufferStoreAction[] colorStoreActions
        {
            get => m_ColorStoreActions;
        }

        public RenderBufferStoreAction depthStoreAction
        {
            get => m_DepthStoreAction;
        }

        internal bool[] overriddenColorStoreActions
        {
            get => m_OverriddenColorStoreActions;
        }

        internal bool overriddenDepthStoreAction
        {
            get => m_OverriddenDepthStoreAction;
        }

        // input requirements
        public ScriptableRenderPassInput input
        {
            get => m_Input;
        }

        public ClearFlag clearFlag
        {
            get => m_ClearFlag;
        }

        public Color clearColor
        {
            get => m_ClearColor;
        }

        RenderBufferStoreAction[] m_ColorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store };
        RenderBufferStoreAction m_DepthStoreAction = RenderBufferStoreAction.Store;

        // store actions默认为Store overridden flags are used to keep track of explicitly requested store actions, to
        // help figuring out the correct final store action for merged render passes when using the RenderPass API.
        private bool[] m_OverriddenColorStoreActions = new bool[] { false };
        private bool m_OverriddenDepthStoreAction = false;

        // 剖析器，可以自定义名字
        protected internal ProfilingSampler profilingSampler { get; set; }
        internal bool overrideCameraTarget { get; set; }
        internal bool isBlitRenderPass { get; set; }

        internal bool useNativeRenderPass { get; set; }

        internal int renderTargetWidth { get; set; }
        internal int renderTargetHeight { get; set; }
        internal int renderTargetSampleCount { get; set; }

        internal bool depthOnly { get; set; }
        // this flag is updated each frame to keep track of which pass is the last for the current camera
        internal bool isLastPass { get; set; }
        // index to track the position in the current frame
        internal int renderPassQueueIndex { get; set; }

        internal NativeArray<int> m_ColorAttachmentIndices;
        internal NativeArray<int> m_InputAttachmentIndices;

        internal GraphicsFormat[] renderTargetFormat { get; set; }  //图形格式数组，对应m_ColorAttachments数组
        
        //构造函数中直接创建8个，实际用到1个，各个pass在OnCameraSetup时SetRenderTarget赋值到这里
        RenderTargetIdentifier[] m_ColorAttachments = new RenderTargetIdentifier[] { BuiltinRenderTextureType.CameraTarget };  
        internal RenderTargetIdentifier[] m_InputAttachments = new RenderTargetIdentifier[8];
        internal bool[] m_InputAttachmentIsTransient = new bool[8];
        RenderTargetIdentifier m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;   //单独一个深度图,OnCameraSetup或Configure时传入
        ScriptableRenderPassInput m_Input = ScriptableRenderPassInput.None;
        ClearFlag m_ClearFlag = ClearFlag.None; //OnCameraSetup时传入
        Color m_ClearColor = Color.black;   //OnCameraSetup时传入

        internal DebugHandler GetActiveDebugHandler(RenderingData renderingData)
        {
            var debugHandler = renderingData.cameraData.renderer.DebugHandler;
            if ((debugHandler != null) && debugHandler.IsActiveForCamera(ref renderingData.cameraData))
                return debugHandler;
            return null;
        }

        public ScriptableRenderPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            m_ColorAttachments = new RenderTargetIdentifier[] { BuiltinRenderTextureType.CameraTarget, 0, 0, 0, 0, 0, 0, 0 };
            m_InputAttachments = new RenderTargetIdentifier[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            m_InputAttachmentIsTransient = new bool[] { false, false, false, false, false, false, false, false };
            m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;
            m_ColorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store, 0, 0, 0, 0, 0, 0, 0 };
            m_DepthStoreAction = RenderBufferStoreAction.Store;
            m_OverriddenColorStoreActions = new bool[] { false, false, false, false, false, false, false, false };
            m_OverriddenDepthStoreAction = false;
            m_ClearFlag = ClearFlag.None;
            m_ClearColor = Color.black;
            overrideCameraTarget = false;
            isBlitRenderPass = false;
            profilingSampler = new ProfilingSampler($"Unnamed_{nameof(ScriptableRenderPass)}");
            useNativeRenderPass = true;
            renderTargetWidth = -1;
            renderTargetHeight = -1;
            renderTargetSampleCount = -1;
            renderPassQueueIndex = -1;
            renderTargetFormat = new GraphicsFormat[]
            {
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None,
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None
            };
            depthOnly = false;
        }

        /// 设置 Input Requirements
        public void ConfigureInput(ScriptableRenderPassInput passInput)
        {
            m_Input = passInput;
        }

        // UniversalRenderer.Setup时，EnqueuePass之前被调用
        public void ConfigureColorStoreAction(RenderBufferStoreAction storeAction, uint attachmentIndex = 0)
        {
            m_ColorStoreActions[attachmentIndex] = storeAction;
            m_OverriddenColorStoreActions[attachmentIndex] = true;
        }

        // UniversalRenderer.Setup时，EnqueuePass之前被调用
        public void ConfigureColorStoreActions(RenderBufferStoreAction[] storeActions)
        {
            int count = Math.Min(storeActions.Length, m_ColorStoreActions.Length);
            for (uint i = 0; i < count; ++i)
            {
                m_ColorStoreActions[i] = storeActions[i];
                m_OverriddenColorStoreActions[i] = true;
            }
        }

        // UniversalRenderer.Setup时，EnqueuePass之前被调用
        public void ConfigureDepthStoreAction(RenderBufferStoreAction storeAction)
        {
            m_DepthStoreAction = storeAction;
            m_OverriddenDepthStoreAction = true;
        }

        internal void ConfigureInputAttachments(RenderTargetIdentifier input, bool isTransient = false)
        {
            m_InputAttachments[0] = input;
            m_InputAttachmentIsTransient[0] = isTransient;
        }

        internal void ConfigureInputAttachments(RenderTargetIdentifier[] inputs)
        {
            m_InputAttachments = inputs;
        }

        internal void ConfigureInputAttachments(RenderTargetIdentifier[] inputs, bool[] isTransient)
        {
            ConfigureInputAttachments(inputs);
            m_InputAttachmentIsTransient = isTransient;
        }

        internal void SetInputAttachmentTransient(int idx, bool isTransient)
        {
            m_InputAttachmentIsTransient[idx] = isTransient;
        }

        internal bool IsInputAttachmentTransient(int idx)
        {
            return m_InputAttachmentIsTransient[idx];
        }
        
        // 设置target，这里并未真正执行CommandBuffer.SetRenderTarget，而是先保存target
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
        {
            m_DepthAttachment = depthAttachment;
            ConfigureTarget(colorAttachment);
        }

        internal void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment, GraphicsFormat format)
        {
            m_DepthAttachment = depthAttachment;
            ConfigureTarget(colorAttachment, format);
        }

        // 设置target，这里并未真正执行CommandBuffer.SetRenderTarget，而是先保存target
        public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment)
        {
            overrideCameraTarget = true;

            uint nonNullColorBuffers = RenderingUtils.GetValidColorBufferCount(colorAttachments);
            if (nonNullColorBuffers > SystemInfo.supportedRenderTargetCount)
                Debug.LogError("Trying to set " + nonNullColorBuffers + " renderTargets, which is more than the maximum supported:" + SystemInfo.supportedRenderTargetCount);

            m_ColorAttachments = colorAttachments;
            m_DepthAttachment = depthAttachment;
        }

        internal void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment, GraphicsFormat[] formats)
        {
            ConfigureTarget(colorAttachments, depthAttachment);
            for (int i = 0; i < formats.Length; ++i)
                renderTargetFormat[i] = formats[i];
        }

        // 设置target，这里并未真正执行CommandBuffer.SetRenderTarget，而是先保存target
        public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
        {
            overrideCameraTarget = true;

            m_ColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
                m_ColorAttachments[i] = 0;
        }

        internal void ConfigureTarget(RenderTargetIdentifier colorAttachment, GraphicsFormat format, int width = -1, int height = -1, int sampleCount = -1, bool depth = false)
        {
            ConfigureTarget(colorAttachment);
            for (int i = 1; i < m_ColorAttachments.Length; ++i)
                renderTargetFormat[i] = GraphicsFormat.None;

            if (depth == true && !GraphicsFormatUtility.IsDepthFormat(format))
            {
                throw new ArgumentException("When configuring a depth only target the passed in format must be a depth format.");
            }

            renderTargetWidth = width;
            renderTargetHeight = height;
            renderTargetSampleCount = sampleCount;
            depthOnly = depth;
            renderTargetFormat[0] = format;
        }

        // 设置target，这里并未真正执行CommandBuffer.SetRenderTarget，而是先保存target
        public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments)
        {
            ConfigureTarget(colorAttachments, BuiltinRenderTextureType.CameraTarget);
        }

        public void ConfigureClear(ClearFlag clearFlag, Color clearColor)
        {
            m_ClearFlag = clearFlag;
            m_ClearColor = clearColor;
        }

        // 在pass Execute前被调用(UniversalRenderer.Execute->InternalStartRendering)
        // 一般用于设置render target和clear状态
        // 如果pass没重写这个方法，那pass的render target为相机的render target
        public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        { }

        // 在pass Execute前被调用,比OnCameraSetup晚(UniversalRenderer.Execute->ExecuteBlock->ExecureREnderPass)
        // 一般用于设置render target和clear状态
        // 如果pass没重写这个方法，那pass的render target为相机的render target
        public virtual void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        { }


        // 当一个相机完成渲染时被调用，用于释放资源
        public virtual void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        // 相机堆载渲染完时被调用，用于释放资源
        public virtual void OnFinishCameraStackRendering(CommandBuffer cmd)
        { }

        /// 具体执行函数，子类实现
        public abstract void Execute(ScriptableRenderContext context, ref RenderingData renderingData);

        // Blit函数，此时render target会被改变
        public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material = null, int passIndex = 0)
        {
            ScriptableRenderer.SetRenderTarget(cmd, destination, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
            cmd.Blit(source, destination, material, passIndex);
        }

        // Blit函数，将材质应用到color target
        public void Blit(CommandBuffer cmd, ref RenderingData data, Material material, int passIndex = 0)
        {
            var renderer = data.cameraData.renderer;

            Blit(cmd, renderer.cameraColorTarget, renderer.GetCameraColorFrontBuffer(cmd), material, passIndex);
            renderer.SwapColorBuffer(cmd);
        }

        // 创建DrawingSettings，设置相机物件排序，shaderTagId，主灯Index，是否动态合批
        public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            Camera camera = renderingData.cameraData.camera;
            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortingCriteria };
            DrawingSettings settings = new DrawingSettings(shaderTagId, sortingSettings)
            {
                perObjectData = renderingData.perObjectData,
                mainLightIndex = renderingData.lightData.mainLightIndex,
                enableDynamicBatching = renderingData.supportsDynamicBatching,

                //preview相机取消gpu实例化
                enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
            };
            return settings;
        }

        // 创建DrawingSettings，设置相机物件排序，shaderTagId，主灯Index，是否动态合批
        public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            if (shaderTagIdList == null || shaderTagIdList.Count == 0)
            {
                Debug.LogWarning("ShaderTagId list is invalid. DrawingSettings is created with default pipeline ShaderTagId");
                return CreateDrawingSettings(new ShaderTagId("UniversalPipeline"), ref renderingData, sortingCriteria);
            }

            DrawingSettings settings = CreateDrawingSettings(shaderTagIdList[0], ref renderingData, sortingCriteria);
            for (int i = 1; i < shaderTagIdList.Count; ++i)
                settings.SetShaderPassName(i, shaderTagIdList[i]);
            return settings;
        }

        public static bool operator <(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent < rhs.renderPassEvent;
        }

        public static bool operator >(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
        {
            return lhs.renderPassEvent > rhs.renderPassEvent;
        }
    }
}
