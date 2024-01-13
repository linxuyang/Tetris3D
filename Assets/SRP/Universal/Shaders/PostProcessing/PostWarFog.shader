Shader "Hidden/Universal Render Pipeline/PostWarFog"
{
    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE
        #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D_X(_SourceTex);
        SAMPLER(sampler_SourceTex);
        TEXTURE2D_X(_PostWarFog_Tex);
        SAMPLER(sampler_PostWarFog_Tex);
        TEXTURE2D_X(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        sampler2D _CloudTexture;

        half4 _SourceTex_TexelSize;

        float4x4 _PostFog_FrustumsRay;

        float4 _Corners;
        float _LandHeightWS;
        half4 _FogColor;
        half _SmoothStart, _SmoothEnd;

        half4 _CloudColor;
        half4 _CloudColor2;
        half _CloudScale, _CloudIntensity, _ShapeSize, _Fractal;
        half2 _CloudMove;
        half _TransformSpeedX, _TransformSpeedY;
        half2 _NoiseTexScale;
        half _NoiseIntensity;
        half2 _NoiseUVSpeed;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        	float3 interpolatedRay : TEXCOORD1;
        };

        half Noise(float2 uv)
		{
           	uv *= _ShapeSize;
			return sin(uv.x) * cos(uv.y);
		}

        half Fbm(float2 p, int n, half strength)
		{
			float2x2 m = float2x2(0.6, 0.8, -0.8, 0.6);
			float f = 0.0;
			float a = 0.5;
			for(int i = 0; i < n; i++)
			{
				f += a * (strength + Noise(p));
				p = mul(p, m) * _Fractal;
				a *= 0.5;
			}
			return f;
		}

        half Cloud(float2 uv, half strength)
		{
			float2 o = float2(Fbm(uv, 4, strength), Fbm(uv + 1.2, 4, strength));
			o += half2(_TransformSpeedX, _TransformSpeedY) * _Time.x;
			o *= 2;
			float2 n = float2(Fbm(o + 9, 4, strength), Fbm(o + 5, 4, strength));
			half f = Fbm(2 * (uv + n), 3, strength);
			f = f * 0.5 + smoothstep(0, 1, pow(f, 3) * pow(n.x, 2)) * 0.5 + smoothstep(0, 1, pow(f, 5) * pow(n.y, 2)) * 0.5;
			return smoothstep(0, 2, f);
		}

        half4 DoubleLayerCloud(half2 uv)
        {
	        half2 cloudUV = uv * 0.25 - 0.005 + _CloudMove * _Time.x;
            half4 tex1 = SAMPLE_TEXTURE2D_X(_NoiseTex, sampler_NoiseTex, cloudUV);
			cloudUV = uv * 0.55 - 0.0065 + _CloudMove * _Time.x;
			half4 tex2 = SAMPLE_TEXTURE2D_X(_NoiseTex, sampler_NoiseTex, cloudUV);
			half noise1 = pow(abs(tex1.g + tex2.g), 0.1);
            half noise2 = pow(abs(tex2.b * tex1.r), 0.25);
            half cloudDensity = pow(noise1 * noise2, _CloudIntensity);
            half cloudAlpha = saturate(cloudDensity);
            half3 cloud1 = lerp(_CloudColor, 0, noise1);
            half3 cloud2 = lerp(_CloudColor, _CloudColor2, noise2) * 2.5;
            half3 cloud = lerp(cloud1, cloud2, noise1 * noise2);
        	return half4(cloud, cloudAlpha);
        }

        v2f Vertex(Attributes v)
        {
            v2f output;
            output.pos = TransformObjectToHClip(v.positionOS.xyz);
            output.uv = v.uv;
        	int index;
            if (v.uv.x < 0.5 && v.uv.y < 0.5)
            {
                index = 0;
            }
            else if (v.uv.x > 0.5 && v.uv.y < 0.5)
            {
                index = 3;
            }
            else if (v.uv.x > 0.5 && v.uv.y > 0.5)
            {
                index = 2;
            }
            else
            {
                index = 1;
            }
        	output.interpolatedRay = _PostFog_FrustumsRay[index];
            return output;
        }

        half4 Fragment(v2f input) : SV_Target
        {
        	float3 ray = normalize(input.interpolatedRay);
        	half needFog = ray.y < 0;
        	float2 posWSxz = _WorldSpaceCameraPos.xz + (_WorldSpaceCameraPos.y - _LandHeightWS) / abs(ray.y) * ray.xz;
        	//_Corners.xy左上,_Corners.zw右下
        	float2 warFogUV = (posWSxz - _Corners.xy) / (_Corners.zw - _Corners.xy);
        	// float2 cloudUV = (posWSxz + _Time.x * _CloudMove) * _CloudScale;
        	// float2 noiseUV = (posWSxz + _Time.x * _NoiseUVSpeed) * _NoiseTexScale;
            half4 sourceColor = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv);
            half warFog = SAMPLE_TEXTURE2D_X(_PostWarFog_Tex, sampler_PostWarFog_Tex, warFogUV);
            warFog = 1 - warFog;
            warFog = smoothstep(_SmoothStart, _SmoothEnd, warFog);

        	
        	// half cloudStrength = SAMPLE_TEXTURE2D_X(_NoiseTex, sampler_NoiseTex, noiseUV).r;
         //   	cloudStrength = (cloudStrength - 0.5) * _NoiseIntensity;
           	// cloudStrength += _CloudIntensity;
        	// half cloud = Cloud(cloudUV * PI / 2, cloudStrength);
        	// half4 warFogColor = lerp(_FogColor, _CloudColor, cloud);
        	// warFogColor.a *= warFog;
			half4 doubleLayerCloud = DoubleLayerCloud(posWSxz * _CloudScale);
        	half4 warFogColor;
        	warFogColor.rgb = lerp(_FogColor.rgb, doubleLayerCloud.rgb, doubleLayerCloud.a);
        	warFogColor.a = saturate((_FogColor.a + _CloudColor.a * doubleLayerCloud.a) * warFog);
            half4 finalColor;
            finalColor.rgb = lerp(sourceColor.rgb, warFogColor.rgb, warFogColor.a * needFog);
            finalColor.a = 1;
            return finalColor;
        }
        ENDHLSL

        Pass
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
            }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDHLSL
        }

    }
}