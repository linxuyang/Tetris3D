using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Bloom")]
    public sealed class Bloom : VolumeComponent, IPostProcessComponent
    {
        [InspectorName("阈值(gamma)")]
        [Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);

        [InspectorName("强度")]
        [Tooltip("Strength of the bloom filter.")]
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);

        [Tooltip("在低于阈值和高于阈值之间的过渡")]
        public ClampedFloatParameter softKnee = new ClampedFloatParameter(0.5f, 0, 1);

        [Tooltip("Changes the extent of veiling effects.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        [InspectorName("限制")]
        [Tooltip("Clamps pixels to control the bloom amount.")]
        public MinFloatParameter clamp = new MinFloatParameter(65472f, 0f);

        [InspectorName("扩散")]
        [Tooltip("改变辉光效果遮罩范围")]
        public ClampedFloatParameter diffusion = new ClampedFloatParameter(7f, 1f, 10f);

        [InspectorName("变形比例")]
        [Tooltip("扭曲变形Bloom效果，负值对应垂直方向变形，正值对应水平方向变形")]
        public ClampedFloatParameter anamorphicRatio = new ClampedFloatParameter(0, -1f, 1f);
        
        [InspectorName("颜色")]
        [Tooltip("Global tint of the bloom filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, true, false, true);

        [InspectorName("快速模式")]
        [Tooltip("通过降低Bloom的质量来获取更好的性能表现（移动端建议开启）")]
        public BoolParameter fastMode = new BoolParameter(false);

        [InspectorName("采样次数")]
        public ClampedIntParameter sampleTimes = new ClampedIntParameter(4, 1, 16);
        
        public BoolParameter antiFlicker = new BoolParameter(false);

        [Tooltip("Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter highQualityFiltering = new BoolParameter(false);

        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);

        [Tooltip("Dirtiness texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);
        
        [Tooltip("Amount of dirtiness.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        public bool IsActive() => intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}
