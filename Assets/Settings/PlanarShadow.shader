Shader "MC/PlanarShadow"
{
    Properties
    {
        _ShadowColor("ShadowColor", color) = (1,1,1,1)
        _StencilID("StencilID", float) = 2
        _Fade("Fade",float) = 4
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent"
        }
        Pass
        {
            Stencil
            {
                Ref [_StencilID]
                Comp NotEqual
                Pass replace
            }
            zwrite off
            blend srcalpha oneminussrcalpha

            Name "PlanarShadow"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _ShadowColor;
            float _Fade;
            CBUFFER_END


            struct appdata
            {
                float3 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                half3 worldPos = TransformObjectToWorld(v.vertex);
                half3 origin = TransformObjectToWorld(half3(0, 0, 0));
                Light light = GetMainLight();
                half3 lightDir = normalize(light.direction);
                float NdL = dot(lightDir, half3(0, -1, 0)); //cos
                float t = max(0, worldPos.y) / NdL; //求得世界坐标到投影坐标的距离，因为cos=y/t
                half3 shadowPos = worldPos.xyz + t * lightDir.xyz;
                o.pos = TransformWorldToHClip(shadowPos);
                //算出影子和原点（脚下）的距离，越远越淡
                // _ShadowColor.a = max(0, (_Fade - t) / _Fade);
                // _ShadowColor.a = max(0, (_Fade - distance(origin, shadowPos)) / _Fade);
                o.col = _ShadowColor;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return i.col;
            }
            ENDHLSL
        }
    }
}