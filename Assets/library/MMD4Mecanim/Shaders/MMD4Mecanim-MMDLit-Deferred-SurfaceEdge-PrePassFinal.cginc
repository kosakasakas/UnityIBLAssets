// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D
// Upgrade NOTE: replaced tex2D unity_LightmapInd with UNITY_SAMPLE_TEX2D_SAMPLER

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_PREPASSFINAL
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

#include "MMD4Mecanim-MMDLit-Deferred-SurfaceEdge-Lighting.cginc"

struct v2f_surf
{
	float4 pos : SV_POSITION;
	float4 screen : TEXCOORD0;
	#ifdef LIGHTMAP_OFF
	half3 vlight : TEXCOORD1;
	#else
	float2 lmap : TEXCOORD1;
	#ifdef DIRLIGHTMAP_OFF
	float4 lmapFadePos : TEXCOORD2;
	#else
	half3 viewDir : TEXCOORD2;
	#endif
	#endif
};

#ifndef LIGHTMAP_OFF
// float4 unity_LightmapST;
#endif

v2f_surf vert_surf(appdata_full v)
{
	v2f_surf o;
	v.vertex = MMDLit_GetEdgeVertex(v.vertex, v.normal);
	o.pos = MMDLit_TransformEdgeVertex(v.vertex);
	float3 worldN = mul((float3x3)_Object2World, SCALED_NORMAL);
	o.screen = ComputeScreenPos(o.pos);
	#ifndef LIGHTMAP_OFF
	o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#ifdef DIRLIGHTMAP_OFF
	o.lmapFadePos.xyz = (mul(_Object2World, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;
	o.lmapFadePos.w = (-mul(UNITY_MATRIX_MV, v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);
	#endif
	#else
	o.vlight = ShadeSH9(float4(worldN, 1.0));
	#endif
	#ifndef DIRLIGHTMAP_OFF
	TANGENT_SPACE_ROTATION;
	o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex));
	#endif
	return o;
}

sampler2D _LightBuffer;
#if defined (SHADER_API_XBOX360) && defined (HDR_LIGHT_PREPASS_ON)
sampler2D _LightSpecBuffer;
#endif
#ifndef LIGHTMAP_OFF
// sampler2D unity_Lightmap;
// sampler2D unity_LightmapInd;
float4 unity_LightmapFade;
#endif

fixed4 frag_surf(v2f_surf IN) : COLOR
{
	half alpha;
	half3 albedo = MMDLit_GetAlbedo(alpha);

	half4 light = tex2Dproj(_LightBuffer, UNITY_PROJ_COORD(IN.screen));
	#if defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)
	light = max(light, half4(0.001));
	#endif
	#ifndef HDR_LIGHT_PREPASS_ON
	light = -log2(light);
	#endif
	#if defined (SHADER_API_XBOX360) && defined (HDR_LIGHT_PREPASS_ON)
	//light.w = tex2Dproj(_LightSpecBuffer, UNITY_PROJ_COORD(IN.screen)).r;
	#endif

	#ifndef LIGHTMAP_OFF
	#ifdef DIRLIGHTMAP_OFF
	half4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	half4 lmtex2 = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd,unity_Lightmap, IN.lmap.xy);
	half lmFade = length(IN.lmapFadePos) * unity_LightmapFade.z + unity_LightmapFade.w;
	half3 lmFull = MMDLit_DecodeLightmap(lmtex);
	half3 lmIndirect = MMDLit_DecodeLightmap(lmtex2);
	half3 lm = lerp (lmIndirect, lmFull, saturate(lmFade));
	light.rgb += lm;
	#else
	half4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	//half4 lmIndTex = tex2D(unity_LightmapInd, IN.lmap.xy);
	//half4 lm = MMDLit_DirLightmap(lmtex, lmIndTex, IN.normal, 0);
	//light += lm;
	half3 lm = MMDLit_DecodeLightmap(lmtex);
	light.rgb += lm;
	#endif
	#else
	light.rgb += IN.vlight;
	#endif
	
	half3 c = albedo * MMDLit_LightingFinal(light.rgb);
	return fixed4(albedo, alpha);
}
