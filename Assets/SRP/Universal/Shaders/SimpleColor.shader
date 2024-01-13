Shader "Universal Render Pipeline/SimpleColor"
{
    Properties
    {
        _Color("Color",Color) = (1,1,1,1)
        _SpecularColor("SpecularColor",Color) = (1,1,1,1)
        _Gloss("Gloss",float) = 0.1
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
//        LOD 100
        Pass
        {
            Name "SimpleColor"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/Lighting.hlsl"
            

            half4 _Color;
            half4 _SpecularColor;
            half _Gloss;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 normal : TEXCOORD1;
                half3 lightDir : TEXCOORD2;
                half3 lightColor : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD5;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                Light light = GetMainLight();
                o.lightDir = normalize(light.direction);
                o.lightColor = light.color;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    o.shadowCoord = TransformWorldToShadowCoord(o.worldPos);
                #endif
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                Light light = GetMainLight();
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                light.shadowAttenuation = MainLightShadow(i.shadowCoord, i.worldPos, half4(1,1,1,1), _MainLightOcclusionProbes);
#endif
                half3 diffuse = _Color.rgb * (dot(i.normal, i.lightDir) * 0.5 + 0.5);
                half3 worldNormal = normalize(i.normal);
                half3 worldLight = normalize(i.lightDir);
                half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half3 halfDir = normalize(worldLight + viewDir);
                half3 specular = i.lightColor *_SpecularColor.rgb* pow(saturate(dot(worldNormal, halfDir)), _Gloss);
                return half4((diffuse), 1);
                return half4((diffuse + specular), 1);
                // half atten = light.shadowAttenuation*light.distanceAttenuation * light.color;
                // return half4(1,0,0, 1);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            // #pragma only_renderers gles gles3 glcore d3d11
            // #pragma target 2.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            // #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Assets/SRP/Universal/Shaders/LitInput.hlsl"
            #include "Assets/SRP/Universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}