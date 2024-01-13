Shader "Hidden/Universal Render Pipeline/Blur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BlurSize("Blur Size", Range(0, 5)) = 2.5
        _BlurSizeScale("Blur Size Scale", Float) = 1
    }
    
    HLSLINCLUDE
    #include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
    
    sampler2D _MainTex;
    half4 _MainTex_TexelSize;
    
    CBUFFER_START(UnityPerMaterial)
    half _BlurSize;
    half _BlurSizeScale;
    CBUFFER_END

    //准备高斯模糊权重矩阵参数7x4的矩阵 ||  Gauss Weight
    static const half4 GaussWeight[7] = {
        half4(0.0205, 0.0205, 0.0205, 0),
        half4(0.0855, 0.0855, 0.0855, 0),
        half4(0.232, 0.232, 0.232, 0),
        half4(0.324, 0.324, 0.324, 1),
        half4(0.232, 0.232, 0.232, 0),
        half4(0.0855, 0.0855, 0.0855, 0),
        half4(0.0205, 0.0205, 0.0205, 0)
    };

    struct VertexInput
    {
        float3 positionOS : POSITION;
        half2 texcoord : TEXCOORD0;
        half4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct VertexOutput
    {
        float4 positionCS: SV_POSITION;
        half4 color : COLOR;
        half2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    VertexOutput Vertex(VertexInput input)
    {
        VertexOutput output = (VertexOutput) 0;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        output.color = input.color;
        output.positionCS = TransformObjectToHClip(input.positionOS);
        output.uv = input.texcoord;
        return output;
    }

    half4 FragmentHorizontal(VertexOutput input): SV_TARGET
    {
        half offset = _MainTex_TexelSize.x * _BlurSize;
        half2 uv_withOffset = input.uv - half2(offset * 3.0, 0);
        half3 color = 0;
        for (int j = 0; j < 7; j ++)
        {
            //偏移后的像素纹理值
            half3 texCol = tex2D(_MainTex, uv_withOffset).rgb;
            //待输出颜色值+=偏移后的像素纹理值 x 高斯权重
            color += texCol * GaussWeight[j];
            //移到下一个像素处，准备下一次循环加权
            uv_withOffset.x += offset;
        }
        return half4(color, 1);
    }

    half4 FragmentVertical(VertexOutput input): SV_TARGET
    {
        half offset = _MainTex_TexelSize.y * _BlurSize * _BlurSizeScale;
        half2 uv_withOffset = input.uv - half2(0, offset * 3.0);
        half3 color = 0;
        for (int j = 0; j < 7; j ++)
        {
            //偏移后的像素纹理值
            half3 texCol = tex2D(_MainTex, uv_withOffset).rgb;
            //待输出颜色值+=偏移后的像素纹理值 x 高斯权重
            color += texCol * GaussWeight[j];
            //移到下一个像素处，准备下一次循环加权
            uv_withOffset.y += offset;
        }
        return half4(color, 1);
    }
    ENDHLSL
    
    SubShader
    {
        //因为我们需要记录当前的屏幕所以是需要透明通道
        Tags {"RenderPipelint" = "UniversalPipeline" "Queue" = "Geometry" "RenderType" = "Opaque"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentHorizontal
            ENDHLSL
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentVertical
            ENDHLSL
        }
    }
    Fallback off
}