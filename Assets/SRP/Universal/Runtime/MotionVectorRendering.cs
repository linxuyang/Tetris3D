using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal.Internal
{
    sealed class MotionVectorRendering
    {
        #region Fields
        static MotionVectorRendering s_Instance;

        Dictionary<Camera, PreviousFrameData> m_CameraFrameData;
        uint m_FrameCount;
        float m_LastTime;
        float m_Time;
        #endregion

        #region Constructors
        private MotionVectorRendering()
        {
            m_CameraFrameData = new Dictionary<Camera, PreviousFrameData>();
        }

        public static MotionVectorRendering instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new MotionVectorRendering();
                return s_Instance;
            }
        }
        #endregion

        #region RenderPass

        public void Clear()
        {
            m_CameraFrameData.Clear();
        }

        public PreviousFrameData GetMotionDataForCamera(Camera camera, CameraData camData)
        {
            // Get MotionData
            PreviousFrameData motionData;
            if (!m_CameraFrameData.TryGetValue(camera, out motionData))
            {
                motionData = new PreviousFrameData();
                m_CameraFrameData.Add(camera, motionData);
            }

            // Calculate motion data
            CalculateTime();
            UpdateMotionData(camera, camData, motionData);
            return motionData;
        }

        #endregion

        void CalculateTime()
        {
            // Get data
            float t = Time.realtimeSinceStartup;

            // SRP.Render() can be called several times per frame.
            // Also, most Time variables do not consistently update in the Scene View.
            // This makes reliable detection of the start of the new frame VERY hard.
            // One of the exceptions is 'Time.realtimeSinceStartup'.
            // Therefore, outside of the Play Mode we update the time at 60 fps,
            // and in the Play Mode we rely on 'Time.frameCount'.
            bool newFrame;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newFrame = (t - m_Time) > 0.0166f;
                m_FrameCount += newFrame ? 1u : 0u;
            }
            else
#endif
            {
                uint frameCount = (uint)Time.frameCount;
                newFrame = m_FrameCount != frameCount;
                m_FrameCount = frameCount;
            }

            if (newFrame)
            {
                // Make sure both are never 0.
                m_LastTime = (m_Time > 0) ? m_Time : t;
                m_Time = t;
            }
        }

        void UpdateMotionData(Camera camera, CameraData cameraData, PreviousFrameData motionData)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)

            // A camera could be rendered multiple times per frame, only updates the previous view proj & pos if needed

                var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true); // Had to change this from 'false'
                var gpuView = camera.worldToCameraMatrix;
                var gpuVP = gpuProj * gpuView;

                // Last frame data
                if (motionData.lastFrameActive != Time.frameCount)
                {
                    motionData.previousViewProjectionMatrix = motionData.isFirstFrame ? gpuVP : motionData.viewProjectionMatrix;
                    motionData.isFirstFrame = false;
                }

                // Current frame data
                motionData.viewProjectionMatrix = gpuVP;

            motionData.lastFrameActive = Time.frameCount;
        }
    }
}
