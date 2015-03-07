Shader "MMD4Mecanim/MMDLit-Deferred-ZClear"
{
	SubShader {
		Tags { "Queue" = "Background-1000" "RenderType" = "Opaque" }
		LOD 200
	    Pass {
			Name "PREPASS"
			Tags { "LightMode" = "PrePassFinal" }
			Fog { Mode Off }

			ZTest Always
			ZWrite On
			Cull Off
			Lighting Off
			ColorMask 0

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			#define DEPTH_BIAS (0.0001)

			sampler2D _CameraDepthTexture;

			struct v2f {
			    float4 pos : SV_POSITION;
				float4 screen : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.screen = ComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.screen.z);
				o.pos.z = o.pos.w;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				float4 screen = i.screen;
				float2 screenPos = screen.xy / screen.w;
				float depth = UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(screen)));
			    float depth2 = screen.z;
				depth = LinearEyeDepth(depth);
				clip(depth - depth2 + DEPTH_BIAS);
				return half4(1,1,1,1);
			}
			ENDCG
	    }
	}
	Fallback Off
}