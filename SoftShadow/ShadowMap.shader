Shader "Custom/ShadowMap" {
 	Properties {
        _AOTex ("AO Texture", 2D) = "white" {} // for using AO baked texture. but not using now..
        _MtlColor ("Material Color", Color) = (1.0,1.0,1.0,1.0)
        _MainTex ("Texture 1", 2D) = "white" {}
    }
    
	CGINCLUDE
	 
	#include "UnityCG2.cginc"
	#include "AutoLight2.cginc" // use custom autolight for soft shadow.
	#include "Lighting.cginc"
	
	// uniform = const 
	uniform sampler2D _AOTex;
	uniform float4 _MtlColor;
	 
	ENDCG
	 
	SubShader
	{
	    Tags { "RenderType"="Opaque" }
	    LOD 200
	 
	    Pass
	    {
	       Lighting On
	 
	       Tags {"LightMode" = "ForwardBase"}
	 
	       CGPROGRAM
	 
	       #pragma vertex vert
	       #pragma fragment frag
	       #pragma multi_compile_fwdbase
	 
	       struct VSOut
	       {
	         float4 pos   : SV_POSITION;
	         float2 uv0   : TEXCOORD0;
	         float2 uv1   : TEXCOORD1;
	         LIGHTING_COORDS(3,4)
	       };
	       
	       struct appdata_lightmap {
	       	float4 vertex : POSITION;
	       	float2 texcoord : TEXCOORD0;
	       	float2 texcoord1 : TEXCOORD1;
	       };
	       
	       sampler2D unity_Lightmap;
	       float4 unity_LightmapST;
	       sampler2D _MainTex;
	       float4 _MainTex_ST;
	 
	       VSOut vert(appdata_lightmap v)
	       {
	         VSOut o;
	         o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	         
	         o.uv0 = TRANSFORM_TEX(v.texcoord, _MainTex);
	         o.uv1 = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	         
	         TRANSFER_VERTEX_TO_FRAGMENT(o);
	         return o;
	       }
	 
	       float4 frag(VSOut i) : COLOR 
	       {
	       	 half4 main_color = tex2D(_MainTex, i.uv0);
	         main_color.rgb *= DecodeLightmap(tex2D(unity_Lightmap, i.uv1));
	         float  atten = LIGHT_ATTENUATION(i);
             return atten * main_color;
	       }
	 
	       ENDCG
	    }
	} 
	FallBack "Diffuse"
}
