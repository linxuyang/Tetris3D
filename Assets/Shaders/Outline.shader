Shader "MC/Outline"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _OutlineWidth("OutlineWidth", Range(0, 20)) = 1
        _OutlineColor("OutlineColor", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags {"LightMode" = "SRPDefaultUnlit"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Tags {"LightMode" = "UniversalForward"}
            Name "OUTLINE"
            Cull Front
            
            HLSLPROGRAM

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            
            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"

            float3 OctahedronToUnitVector(float2 oct)
            {
                float3 unitVec = float3(oct, 1 - dot(float2(1, 1), abs(oct)));

                if (unitVec.z < 0)
                {
                    unitVec.xy = (1 - abs(unitVec.yx)) * float2(unitVec.x >= 0 ? 1 : -1, unitVec.y >= 0 ? 1 : -1);
                }
                
                return normalize(unitVec);
            }
    
            struct OutlineVertexInput
            {
                float3 positionOS : POSITION;
                float3 smoothNormalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };
                
            struct OutlineVertexOutput
            {
                float4 positionCS : SV_POSITION;
            };

            half _OutlineWidth;
            half3 _OutlineColor;
            
            OutlineVertexOutput OutlineVertex(OutlineVertexInput input)
            {
                OutlineVertexOutput output = (OutlineVertexOutput)0;

                output.positionCS = TransformObjectToHClip(input.positionOS);
                
                float3 normalTS = input.smoothNormalOS;
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.smoothNormalOS, input.tangentOS);
                float3x3 tangentToWorld = float3x3(normalInputs.tangentWS, normalInputs.bitangentWS, normalInputs.normalWS);
                // float3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
                float3 normalWS = normalTS;
                float3 normalCS = TransformWorldToHClipDir(normalWS);
                
                float2 offset = normalize(normalCS.xy) / _ScreenParams.xy * _OutlineWidth * output.positionCS.w * 2;
                output.positionCS.xy += offset;
                return output;
            }
    
            half4 OutlineFragment(OutlineVertexOutput i) : SV_Target
            {
                return half4(_OutlineColor, 1);
            }
            ENDHLSL
        }
    }
}
