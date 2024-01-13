Shader "Hidden/Universal Render Pipeline/Bloom"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        // #pragma multi_compile_local _ _USE_RGBM
        // #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Assets/SRP/Core/ShaderLibrary/Common.hlsl"
        #include "Assets/SRP/Core/ShaderLibrary/Filtering.hlsl"
        #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
        #include "Assets/SRP/Universal/Shaders/PostProcessing/Common.hlsl"
        #include "Assets/SRP/Universal/ShaderLibrary/SSAO.hlsl"

    #define EPSILON (1e-10)
        TEXTURE2D_X(_SourceTex);
        SAMPLER(sampler_SourceTex);
        float4 _SourceTex_TexelSize;
        TEXTURE2D_X(_SourceTexLowMip);
        float4 _SourceTexLowMip_TexelSize;

        float _Clamp;
        // float4 _Params; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee
        float4 _Threshold; // x: threshold value (linear), y: threshold - knee, z: knee * 2, w: 0.25 / knee
        float  _SampleScale;

        // #define Scatter             _Params.x
        // #define ClampMax            _Params.y
        // #define Threshold           _Params.z
        // #define ThresholdKnee       _Params.w

        // half4 EncodeHDR(half3 color)
        // {
        // #if _USE_RGBM
        //     half4 outColor = EncodeRGBM(color);
        // #else
        //     half4 outColor = half4(color, 1.0);
        // #endif
        //
        // #if UNITY_COLORSPACE_GAMMA
        //     return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
        // #else
        //     return outColor;
        // #endif
        // }

        // half3 DecodeHDR(half4 color)
        // {
        // #if UNITY_COLORSPACE_GAMMA
        //     color.xyz *= color.xyz; // γ to linear
        // #endif
        //
        // #if _USE_RGBM
        //     return DecodeRGBM(color);
        // #else
        //     return color.xyz;
        // #endif
        // }

        half3 DownsampleBox13Tap(float2 uv)
        {
            float texelSize = _SourceTex_TexelSize.x;
            half4 A = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-1.0, -1.0));
            half4 B = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.0, -1.0));
            half4 C = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(1.0, -1.0));
            half4 D = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-0.5, -0.5));
            half4 E = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.5, -0.5));
            half4 F = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-1.0, 0.0));
            half4 G = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv);
            half4 H = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(1.0, 0.0));
            half4 I = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-0.5, 0.5));
            half4 J = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.5, 0.5));
            half4 K = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-1.0, 1.0));
            half4 L = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.0, 1.0));
            half4 M = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(1.0, 1.0));

            half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

            half4 o = (D + E + I + J) * div.x;
            o += (A + B + G + F) * div.y;
            o += (B + C + H + G) * div.y;
            o += (F + G + L + K) * div.y;
            o += (G + H + M + L) * div.y;

            return o.xyz;
        }

        // Standard box filtering
        half3 DownsampleBox4Tap(float2 uv)
        {
            float4 d = _SourceTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
        
            half4 s = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.xy);
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.zy);
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.xw);
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.zw);
        
            return s.rgb * (1.0 / 4.0);
        }

        half3 DownsampleBox4TapAntiFlicker(float2 uv)
        {
            float4 d = _SourceTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
        
            half4 s1 = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.xy);
            half4 s2 = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.zy);
            half4 s3 = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.xw);
            half4 s4 = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.zw);
        
            half s1w = 1 / (Max3(s1.r, s1.g, s1.b) + 1);
            half s2w = 1 / (Max3(s2.r, s2.g, s2.b) + 1);
            half s3w = 1 / (Max3(s3.r, s3.g, s3.b) + 1);
            half s4w = 1 / (Max3(s4.r, s4.g, s4.b) + 1);
            half one_div_wsum = 1 / (s1w + s2w + s3w + s4w);
            return (s1 * s1w + s2 * s2w + s3 * s3w + s4 * s4w).rgb * one_div_wsum;
        }

        // Quadratic color thresholding
        // curve = (threshold - knee, knee * 2, 0.25 / knee)
        half3 QuadraticThreshold(half3 color, half threshold, half3 curve)
        {
            // Pixel brightness
            half br = Max3(color.r, color.g, color.b);
        
            // Under-threshold part: quadratic curve
            half rq = clamp(br - curve.x, 0.0, curve.y);
            rq = curve.z * rq * rq;
        
            // Combine and apply the brightness response curve.
            color *= max(rq, br - threshold) / max(br, EPSILON);
        
            return color;
        }

        half4 Prefilter(half3 color)
        {
            color = min(_Clamp, color); // clamp to max
            color = QuadraticThreshold(color, _Threshold.x, _Threshold.yzw);
            return half4(color, 1);
        }

        // 9-tap bilinear upsampler (tent filter)
        half4 UpsampleTent(float2 uv)
        {
            float4 d = _SourceTex_TexelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * _SampleScale;
        
            half4 s;
            s =  SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv - d.xy);
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv - d.wy) * 2.0;
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv - d.zy);
        
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.zw) * 2.0;
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv) * 4.0;
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.xw) * 2.0;
        
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.zy);
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.wy) * 2.0;
            s += SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv + d.xy);
        
            return s * (1.0 / 16.0);
        }

        half4 Combine(half4 bloom, float2 uv)
        {
            half4 color = SAMPLE_TEXTURE2D(_SourceTexLowMip, sampler_SourceTex, uv);
            return bloom + color;
        }

        // half4 FragPrefilter(Varyings input) : SV_Target
        // {
        //     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //     float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
        //
        // #if _BLOOM_HQ
        //     float texelSize = _SourceTex_TexelSize.x;
        //     half4 A = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-1.0, -1.0));
        //     half4 B = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.0, -1.0));
        //     half4 C = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(1.0, -1.0));
        //     half4 D = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-0.5, -0.5));
        //     half4 E = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.5, -0.5));
        //     half4 F = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-1.0, 0.0));
        //     half4 G = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv);
        //     half4 H = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(1.0, 0.0));
        //     half4 I = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-0.5, 0.5));
        //     half4 J = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.5, 0.5));
        //     half4 K = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(-1.0, 1.0));
        //     half4 L = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(0.0, 1.0));
        //     half4 M = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + texelSize * float2(1.0, 1.0));
        //
        //     half2 div = (1.0 / 4.0) * half2(0.5, 0.125);
        //
        //     half4 o = (D + E + I + J) * div.x;
        //     o += (A + B + G + F) * div.y;
        //     o += (B + C + H + G) * div.y;
        //     o += (F + G + L + K) * div.y;
        //     o += (G + H + M + L) * div.y;
        //
        //     half3 color = o.xyz;
        // #else
        //     half3 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv).xyz;
        // #endif
        //
        //     // User controlled clamp to limit crazy high broken spec
        //     color = min(ClampMax, color);
        //
        //     // Thresholding
        //     half brightness = Max3(color.r, color.g, color.b);
        //     half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
        //     softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
        //     half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
        //     color *= multiplier;
        //
        //     // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
        //     color = max(color, 0);
        //     return EncodeHDR(color);
        // }

        half4 FragPrefilter13(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            half3 color = DownsampleBox13Tap(uv);
            color = min(color, HALF_MAX);
            return Prefilter(color);
        }

        half4 FragPrefilter4(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
        #ifdef ANTI_FLICKER
            half3 color = DownsampleBox4TapAntiFlicker(uv);
        #else
            half3 color = DownsampleBox4Tap(uv);
        #endif
            color = min(color, HALF_MAX);
            return Prefilter(color);
        }

        half4 FragDownsample13(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            half3 color = DownsampleBox13Tap(uv);
            return half4(color, 1);
        }

        half4 FragDownsample4(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
        #ifdef ANTI_FLICKER
            half3 color = DownsampleBox4TapAntiFlicker(uv);
        #else
            half3 color = DownsampleBox4Tap(uv);
        #endif
            return half4(color, 1);
        }

        half4 FragUpsampleTent(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            half4 bloom = UpsampleTent(uv);
            bloom.a = saturate(bloom.r+bloom.g+bloom.b);
            return Combine(bloom, uv);
        }

        half4 FragUpsampleBox(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            half4 bloom = UpsampleBox(TEXTURE2D_ARGS(_SourceTex, sampler_SourceTex), uv, _SourceTex_TexelSize.xy, _SampleScale);
            bloom.a = saturate(bloom.r+bloom.g+bloom.b);
            return Combine(bloom, uv);
        }

        // half4 FragBlurH(Varyings input) : SV_Target
        // {
        //     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //     float texelSize = _SourceTex_TexelSize.x * 2.0;
        //     float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
        //
        //     // 9-tap gaussian blur on the downsampled source
        //     half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv - float2(texelSize * 4.0, 0.0)));
        //     half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv - float2(texelSize * 3.0, 0.0)));
        //     half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv - float2(texelSize * 2.0, 0.0)));
        //     half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv - float2(texelSize * 1.0, 0.0)));
        //     half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv                               ));
        //     half3 c5 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + float2(texelSize * 1.0, 0.0)));
        //     half3 c6 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + float2(texelSize * 2.0, 0.0)));
        //     half3 c7 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + float2(texelSize * 3.0, 0.0)));
        //     half3 c8 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + float2(texelSize * 4.0, 0.0)));
        //
        //     half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
        //                 + c4 * 0.22702703
        //                 + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;
        //
        //     return EncodeHDR(color);
        // }

        // half4 FragBlurV(Varyings input) : SV_Target
        // {
        //     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //     float texelSize = _SourceTex_TexelSize.y;
        //     float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
        //
        //     // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
        //     half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv - float2(0.0, texelSize * 3.23076923)));
        //     half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv - float2(0.0, texelSize * 1.38461538)));
        //     half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv                                      ));
        //     half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + float2(0.0, texelSize * 1.38461538)));
        //     half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv + float2(0.0, texelSize * 3.23076923)));
        //
        //     half3 color = c0 * 0.07027027 + c1 * 0.31621622
        //                 + c2 * 0.22702703
        //                 + c3 * 0.31621622 + c4 * 0.07027027;
        //
        //     return EncodeHDR(color);
        // }

        // half3 Upsample(float2 uv)
        // {
        //     half3 highMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv));
        //
        // #if _BLOOM_HQ && !defined(SHADER_API_GLES)
        //     half3 lowMip = DecodeHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_SourceTex), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
        // #else
        //     half3 lowMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_SourceTex, uv));
        // #endif
        //
        //     return lerp(highMip, lowMip, Scatter);
        // }

        // half4 FragUpsample(Varyings input) : SV_Target
        // {
        //     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //     half3 color = Upsample(UnityStereoTransformScreenSpaceTex(input.uv));
        //     return EncodeHDR(color);
        // }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

//        // 0
//        Pass
//        {
//            Name "Bloom Prefilter"
//
//            HLSLPROGRAM
//                #pragma vertex FullscreenVert
//                #pragma fragment FragPrefilter
//                #pragma multi_compile_local _ _BLOOM_HQ
//            ENDHLSL
//        }
//
//        // 1
//        Pass
//        {
//            Name "Bloom Blur Horizontal"
//
//            HLSLPROGRAM
//                #pragma vertex FullscreenVert
//                #pragma fragment FragBlurH
//            ENDHLSL
//        }
//
//        // 2
//        Pass
//        {
//            Name "Bloom Blur Vertical"
//
//            HLSLPROGRAM
//                #pragma vertex FullscreenVert
//                #pragma fragment FragBlurV
//            ENDHLSL
//        }
//
//        // 3
//        Pass
//        {
//            Name "Bloom Upsample"
//
//            HLSLPROGRAM
//                #pragma vertex FullscreenVert
//                #pragma fragment FragUpsample
//                #pragma multi_compile_local _ _BLOOM_HQ
//            ENDHLSL
//        }
        
        // 0: Prefilter 13 taps
        Pass
        {
            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragPrefilter13

            ENDHLSL
        }

        // 1: Prefilter 4 taps
        Pass
        {
            HLSLPROGRAM
                #pragma multi_compile _ ANTI_FLICKER
                #pragma vertex FullscreenVert
                #pragma fragment FragPrefilter4

            ENDHLSL
        }
        
        // 2: Downsample 13 taps
        Pass
        {
            HLSLPROGRAM

                #pragma vertex FullscreenVert
                #pragma fragment FragDownsample13

            ENDHLSL
        }

        // 3: Downsample 4 taps
        Pass
        {
            HLSLPROGRAM

                #pragma multi_compile _ ANTI_FLICKER
                #pragma vertex FullscreenVert
                #pragma fragment FragDownsample4

            ENDHLSL
        }
        
        // 4: Upsample tent filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex FullscreenVert
                #pragma fragment FragUpsampleTent

            ENDHLSL
        }

        // 5: Upsample box filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex FullscreenVert
                #pragma fragment FragUpsampleBox

            ENDHLSL
        }
    }
}
