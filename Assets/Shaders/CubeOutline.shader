Shader "MC/CubeOutline"
{
    Properties
    {
        _Color("Color",Color) = (1,1,1,1)
      _LineColor("LineColor",Color) = (0,0,0,1)
        _OutlineWidth("OutlineWidth", Range(0, 20)) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent"
        }
      Blend SrcAlpha OneMinusSrcAlpha    //透明底
        LOD 100
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

            #pragma enable_d3d11_debug_symbols


            #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SRP/Universal/ShaderLibrary/Lighting.hlsl"
            half4 _Color;
            half4 _LineColor;
            half _OutlineWidth;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 normal : TEXCOORD1;
                half3 lightDir : TEXCOORD2;
                float2 uv : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
            UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                Light light = GetMainLight();
                o.lightDir = normalize(light.direction);
                float2 normal = v.uv - half2(0.5,0.5);//四个角减去中点，得出4个点的外扩方向
                float3 normalWS = half3(normal,0);
                float3 normalCS = TransformWorldToHClipDir(normalWS);
                float2 offset = normalize(normalWS.xy) / _ScreenParams.xy * _OutlineWidth *o.pos.w * 2;
                o.uv = v.uv + offset;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
            UNITY_SETUP_INSTANCE_ID(i);
                //half3 diffuse = _Color.rgb * (dot(i.normal, i.lightDir) * 0.5 + 0.5);
                if(i.uv.x<0||i.uv.x>1||i.uv.y<0||i.uv.y>1)
                {
                    return _LineColor;
                }
                return _Color;
            }
            ENDHLSL
        }
    }
}