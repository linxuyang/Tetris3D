Shader "Hidden/Universal Render Pipeline/LensFlareDataDriven"
{
    SubShader
    {
        // Additive
        Pass
        {
            Name "LensFlareAdditive"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
            LOD 100

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers gles

            #pragma multi_compile_fragment _ FLARE_CIRCLE FLARE_POLYGON
            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF
            #pragma multi_compile _ FLARE_OCCLUSION

            #include "Assets/SRP/Core/ShaderLibrary/Common.hlsl"

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/UnityInput.hlsl"
            #include "Assets/SRP/Universal/Shaders/PostProcessing/Common.hlsl"

            #include "Assets/SRP/Core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Screen
        Pass
        {
            Name "LensFlareScreen"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
            LOD 100

            Blend One OneMinusSrcColor
            BlendOp Max
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers gles

            #pragma multi_compile_fragment _ FLARE_CIRCLE FLARE_POLYGON
            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF
            #pragma multi_compile _ FLARE_OCCLUSION

            #include "Assets/SRP/Core/ShaderLibrary/Common.hlsl"

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/UnityInput.hlsl"
            #include "Assets/SRP/Universal/Shaders/PostProcessing/Common.hlsl"

            #include "Assets/SRP/Core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Premultiply
        Pass
        {
            Name "LensFlarePremultiply"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
            LOD 100

            Blend One OneMinusSrcAlpha
            ColorMask RGB
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers gles

            #pragma multi_compile_fragment _ FLARE_CIRCLE FLARE_POLYGON
            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF
            #pragma multi_compile _ FLARE_OCCLUSION

            #include "Assets/SRP/Core/ShaderLibrary/Common.hlsl"

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/UnityInput.hlsl"
            #include "Assets/SRP/Universal/Shaders/PostProcessing/Common.hlsl"

            #include "Assets/SRP/Core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Lerp
        Pass
        {
            Name "LensFlareLerp"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
            LOD 100

            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers gles

            #pragma multi_compile_fragment _ FLARE_CIRCLE FLARE_POLYGON
            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF
            #pragma multi_compile _ FLARE_OCCLUSION

            #include "Assets/SRP/Core/ShaderLibrary/Common.hlsl"

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/UnityInput.hlsl"
            #include "Assets/SRP/Universal/Shaders/PostProcessing/Common.hlsl"

            #include "Assets/SRP/Core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
    }
}
