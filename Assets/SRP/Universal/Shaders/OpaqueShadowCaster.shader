Shader "MC/OpaqueShadowCaster"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)]_Cull("剔除模式", Float) = 2
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            // #pragma exclude_renderers gles gles3 glcore
            #pragma target 3.0

            // #pragma multi_compile_instancing

            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            
            float3 _LightDirection;
            float4 _ShadowBias; // x: depth bias, y: normal bias

            struct ShadowCasterVertexInput
            {
                float4 positionOS : POSITION;
                half2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct ShadowCasterVertexOutput
            {
                float4 positionCS : SV_POSITION;
            };

            float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * _ShadowBias.y;

                // normal bias is negative since we want to apply an inset normal offset
                positionWS = lightDirection * _ShadowBias.xxx + positionWS;
                positionWS = normalWS * scale.xxx + positionWS;
                return positionWS;
            }

            float4 GetShadowPositionHClip(ShadowCasterVertexInput input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            ShadowCasterVertexOutput ShadowVertex(ShadowCasterVertexInput input)
            {
                ShadowCasterVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowFragment(ShadowCasterVertexOutput input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}