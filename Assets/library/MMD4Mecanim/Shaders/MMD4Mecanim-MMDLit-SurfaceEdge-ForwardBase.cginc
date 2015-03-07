// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "MMD4Mecanim-MMDLit-AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

#include "MMD4Mecanim-MMDLit-SurfaceEdge-Lighting.cginc"
#include "MMD4Mecanim-MMDLit-Lighting.cginc"

#ifdef LIGHTMAP_OFF
struct v2f_surf {
  float4 pos : SV_POSITION;
  half3 vlight : TEXCOORD1;
  half3 viewDir : TEXCOORD2;
  LIGHTING_COORDS(3,4)
};
#endif
#ifndef LIGHTMAP_OFF
struct v2f_surf {
  float4 pos : SV_POSITION;
  float2 lmap : TEXCOORD0;
  LIGHTING_COORDS(1,2)
};
#endif
#ifndef LIGHTMAP_OFF
// float4 unity_LightmapST;
#endif

v2f_surf vert_surf (appdata_full v)
{
	v2f_surf o;
	v.vertex = MMDLit_GetEdgeVertex(v.vertex, v.normal);
	o.pos = MMDLit_TransformEdgeVertex(v.vertex);
	#ifndef LIGHTMAP_OFF
	o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif
	#ifdef LIGHTMAP_OFF
	o.viewDir = (half3)WorldSpaceViewDir(v.vertex);
	#endif
	#ifdef LIGHTMAP_OFF
	float3 worldN = mul((float3x3)_Object2World, SCALED_NORMAL);
	o.vlight = ShadeSH9(float4(worldN, 1.0));
	#ifdef VERTEXLIGHT_ON
	// Skip Vertex Lighting for Edge
	//float3 worldPos = mul(_Object2World, v.vertex).xyz;
	//o.vlight += Shade4PointLights(
	//	unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
	//	unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
	//	unity_4LightAtten0, worldPos, worldN);
	#endif // VERTEXLIGHT_ON
	#endif // LIGHTMAP_OFF
	TRANSFER_VERTEX_TO_FRAGMENT(o);
	return o;
}

#ifndef LIGHTMAP_OFF
// sampler2D unity_Lightmap;
#ifndef DIRLIGHTMAP_OFF
// sampler2D unity_LightmapInd;
#endif
#endif

fixed4 frag_surf (v2f_surf IN) : COLOR
{
	half alpha;
	half3 albedo = MMDLit_GetAlbedo(alpha);
	half atten = LIGHT_ATTENUATION(IN);
	half3 c = 0;

	#ifdef LIGHTMAP_OFF
	c = MMDLit_Lighting(albedo, atten);
	#endif // LIGHTMAP_OFF || DIRLIGHTMAP_OFF
	#ifdef LIGHTMAP_OFF
	c += albedo * IN.vlight;
	#endif // LIGHTMAP_OFF

	#ifndef LIGHTMAP_OFF
	#ifndef DIRLIGHTMAP_OFF
	half4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	//half4 lmIndTex = tex2D(unity_LightmapInd, IN.lmap.xy);
	half3 lm = MMDLit_DecodeLightmap(lmtex);
	#else // !DIRLIGHTMAP_OFF
	half4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	half3 lm = MMDLit_DecodeLightmap(lmtex);
	#endif // !DIRLIGHTMAP_OFF
	#ifdef SHADOWS_SCREEN
	#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	c += albedo * min(lm, atten*2);
	#else
	c += albedo * max(min(lm,(atten*2)*(half3)lmtex), lm*atten);
	#endif
	#else // SHADOWS_SCREEN
	c += albedo * lm;
	#endif // SHADOWS_SCREEN
	#endif // LIGHTMAP_OFF
	
	return fixed4(c, alpha);
}
