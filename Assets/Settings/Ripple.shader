Shader "MC/Ripple"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DistanceFactor("WaveThick",float) = 80			//波纹密度
		_TotalFactor("TotalFactor",Range(0,0.1)) = 0.02 //振幅
		_WaveWidth("WaveWith",Range(0,1)) = 0.3			//扩散宽度，到_WaveWidth后开始收缩
		_TimeFactor("TimeFactor",float) = 30			//扩散速度,影响波纹扩散影响时间变量
		_ReduceSpeed("reduceSpeed",float) = 1.3			//减弱速度
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Tags
            {
                "LightMode" = "UniversalForward"
            }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Assets/SRP/Universal/ShaderLibrary/Core.hlsl"
			
			sampler2D _MainTex;
			float _DistanceFactor;
			float _TotalFactor;
			float _TimeFactor;
			float _WaveWidth;
			float _ReduceSpeed;
			uniform float _CurWaveDis; 

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			///思想：1.求像素点距离中心点的方向矢量,用该方向A*sin(ωx+φ)系数为像素偏移 A为振幅  ω压缩程度 φ偏移量
			///		 2.波纹是正弦波，因此sinFactor通过正弦函数计算，输入像素点距离中心点的距离
			///		 3.扩散系数 spreedFactor 将波纹限制在 _WaveWidth中，小于_WaveWidth返回0，大于返回1
			///	     4.减弱系数 shrinkFactor 刚开始最强，随着时间推移逐渐减弱，减小偏移 _ReduceSpeed 控制减弱程度
			///		 5.偏移量	所有系数相乘的结果去偏移uv
			half4 frag (v2f i) : SV_Target
			{
				float2 rippleDir = float2(0.5,0.5) - i.uv;
				rippleDir = rippleDir * float2(_ScreenParams.x/_ScreenParams.y,1);

				float dis = sqrt(rippleDir.x*rippleDir.x + rippleDir.y*rippleDir.y); 
				float sinFactor =  _TotalFactor * sin(dis * _DistanceFactor - _Time.y *_TimeFactor);

				float spreedFactor = clamp(_WaveWidth-abs(_CurWaveDis - dis),0,1)/_WaveWidth;
				float shrinkFactor = clamp(1 - _CurWaveDis * _ReduceSpeed,0,1);

				rippleDir = normalize(rippleDir);
				float2 offset = rippleDir * sinFactor * spreedFactor * shrinkFactor ;
				float2 uv = offset + i.uv;

				return tex2D(_MainTex,uv);
			}
			ENDHLSL
		}
	}
}
