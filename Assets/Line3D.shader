Shader "Custom/Line3D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TextureScale ("Texture Scale", Float) = 1
        _TextureOffset ("Texture Offset", Float) = 0
        _ViewOffset ("_ViewOffset", Float) = 0
        _DepthOffset ("_DepthOffset", Float) = 0
        _WidthMultiplier ("_WidthMultiplier", Float) = 2
        _Color ("_Color", Color) = (1, 1, 1, 1)
        _FadeAlphaDistanceFrom ("FadeAlphaDistanceFrom", Float) = 100000
        _FadeAlphaDistanceTo ("FadeAlphaDistanceTo", Float) = 100000
        [Enum(UnityEngine.Rendering.CompareFunction)] _zTestCompare ("ZTest", Float) = 4
        _AutoTextureOffset ("AutoTextureOffset", Float) = 0
        _PersentOfScreenHeightMode ("PersentOfScreenHeightMode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
            "ForceNoShadowCasting" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite On
        ZTest [_zTestCompare]

        Pass
        {
            Fog { Mode Off }
            Offset [_DepthOffset], [_DepthOffset]

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TextureScale;
            float _TextureOffset;
            float _ViewOffset;
            float _FadeAlphaDistanceFrom;
            float _FadeAlphaDistanceTo;
            float _AutoTextureOffset;
            float _PersentOfScreenHeightMode;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _WidthMultiplier)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct AppData
            {
                float3 vertex : POSITION;
                // Matches Linefy Lines: normal stores the opposite endpoint, not a surface normal.
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float3 uv : TEXCOORD0;
                float3 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 ClipToPixel(float4 clipPosition)
            {
                float2 pixel = clipPosition.xy / clipPosition.w;
                pixel.x = (pixel.x + 1) * 0.5 * _ScreenParams.x;
                pixel.y = (pixel.y + 1) * 0.5 * _ScreenParams.y;
                return pixel;
            }

            float4 PixelToClip(float2 pixel, float z, float w)
            {
                float x = (pixel.x / _ScreenParams.x * 2 - 1) * w;
                float y = (pixel.y / _ScreenParams.y * 2 - 1) * w;
                return float4(x, y, z, w);
            }

            Varyings Vert(AppData v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o;
                UNITY_INITIALIZE_OUTPUT(Varyings, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                int vertexId = v.uv1.x;
                int disabled = v.uv1.y;
                if (disabled == 1)
                {
                    o.pos = 0;
                    o.uv = 0;
                    o.color = 0;
                    return o;
                }

                float3 endpoint0 = v.vertex;
                float3 endpoint1 = v.normal;
                float3 viewEndpoint0 = UnityObjectToViewPos(endpoint0);
                float3 viewEndpoint1 = UnityObjectToViewPos(endpoint1);
                float3 objectSpaceViewDir0 = ObjSpaceViewDir(float4(endpoint0, 0));
                float distanceToCamera = length(objectSpaceViewDir0);

                if (abs(_ViewOffset) > 0.0001)
                {
                    float3 viewDir = UNITY_MATRIX_IT_MV[2];
                    float3 objectSpaceViewDir1 = ObjSpaceViewDir(float4(endpoint1, 0));
                    float distanceToCamera1 = length(objectSpaceViewDir1);
                    objectSpaceViewDir0 = distanceToCamera == 0 ? objectSpaceViewDir0 : objectSpaceViewDir0 / distanceToCamera;
                    objectSpaceViewDir1 = distanceToCamera1 == 0 ? objectSpaceViewDir1 : objectSpaceViewDir1 / distanceToCamera1;
                    endpoint0 += lerp(objectSpaceViewDir0, viewDir, unity_OrthoParams.w) * _ViewOffset;
                    endpoint1 += lerp(objectSpaceViewDir1, viewDir, unity_OrthoParams.w) * _ViewOffset;
                }

                if (viewEndpoint0.z > 0)
                {
                    endpoint0 = lerp(endpoint0, endpoint1, -viewEndpoint0.z / (-viewEndpoint0.z + viewEndpoint1.z) + 0.001);
                }

                if (viewEndpoint1.z > 0)
                {
                    endpoint1 = lerp(endpoint1, endpoint0, -viewEndpoint1.z / (-viewEndpoint1.z + viewEndpoint0.z) + 0.001);
                }

                float4 clipEndpoint0 = UnityObjectToClipPos(endpoint0);
                float4 clipEndpoint1 = UnityObjectToClipPos(endpoint1);
                float2 pixel0 = ClipToPixel(clipEndpoint0);
                float2 pixel1 = ClipToPixel(clipEndpoint1);
                float2 pixelDir = pixel1 - pixel0;
                float pixelLength = length(pixelDir);
                pixelDir = pixelLength == 0 ? float2(1, 0) : pixelDir / pixelLength;
                float2 pixelOrtho = float2(pixelDir.y, -pixelDir.x);

                fixed4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float widthMultiplier = UNITY_ACCESS_INSTANCED_PROP(Props, _WidthMultiplier);
                o.color = v.color * instanceColor;

                float width = lerp(
                    widthMultiplier * v.uv.y * 0.5,
                    _ScreenParams.y * widthMultiplier * v.uv.y * 0.005,
                    _PersentOfScreenHeightMode);

                if (_ProjectionParams.x > 0)
                {
                    width = -width;
                }

                float uvw = width * clipEndpoint0.w;
                float distanceFade = saturate((distanceToCamera - _FadeAlphaDistanceFrom) / (_FadeAlphaDistanceTo - _FadeAlphaDistanceFrom));
                o.color.a *= 1 - distanceFade;

                float lineUvLength = length(endpoint0 - endpoint1) * _AutoTextureOffset;
                float uvx = _TextureOffset + v.uv.x * _TextureScale;

                if (vertexId == 0)
                {
                    pixel0 -= pixelOrtho * width;
                    o.uv = float3(uvx, 0, uvw);
                    o.pos = PixelToClip(pixel0, clipEndpoint0.z, clipEndpoint0.w);
                    return o;
                }

                if (vertexId == 1)
                {
                    pixel0 += pixelOrtho * width;
                    o.uv = float3(uvx, uvw, uvw);
                    o.pos = PixelToClip(pixel0, clipEndpoint0.z, clipEndpoint0.w);
                    return o;
                }

                uvx = _TextureOffset + (v.uv.x + lineUvLength) * _TextureScale;

                if (vertexId == 2)
                {
                    pixel0 -= pixelOrtho * width;
                    o.uv = float3(uvx, uvw, uvw);
                    o.pos = PixelToClip(pixel0, clipEndpoint0.z, clipEndpoint0.w);
                    return o;
                }

                pixel0 += pixelOrtho * width;
                o.uv = float3(uvx, 0, uvw);
                o.pos = PixelToClip(pixel0, clipEndpoint0.z, clipEndpoint0.w);
                return o;
            }

            fixed4 Frag(Varyings i) : SV_Target
            {
                float2 uv = float2(i.uv.x, i.uv.y / i.uv.z);
                fixed4 col = tex2D(_MainTex, uv) * i.color;
                clip(col.a - 0.5);
                return col;
            }
            ENDCG
        }
    }
}
