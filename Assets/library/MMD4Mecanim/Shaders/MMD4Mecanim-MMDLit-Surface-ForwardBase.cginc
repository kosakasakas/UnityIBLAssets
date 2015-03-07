// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D
// Upgrade NOTE: replaced tex2D unity_LightmapInd with UNITY_SAMPLE_TEX2D_SAMPLER

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "MMD4Mecanim-MMDLit-AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

#include "MMD4Mecanim-MMDLit-Surface-Lighting.cginc"

#ifdef LIGHTMAP_OFF
struct v2f_surf {
	float4 pos : SV_POSITION;
	float2 pack0 : TEXCOORD0;
	half3 normal : TEXCOORD1;
	half3 vlight : TEXCOORD2;
	half3 viewDir : TEXCOORD3;
	LIGHTING_COORDS(4,5)
	half3 mmd_uvwSphere : TEXCOORD6;
};
#endif

#ifndef LIGHTMAP_OFF
struct v2f_surf {
	float4 pos : SV_POSITION;
	float2 pack0 : TEXCOORD0;
	half3 normal : TEXCOORD1;
	float2 lmap : TEXCOORD2;
#ifndef DIRLIGHTMAP_OFF
	half3 viewDir : TEXCOORD3;
	LIGHTING_COORDS(4,5)
	half3 mmd_uvwSphere : TEXCOORD6;
#else
	LIGHTING_COORDS(3,4)
	half3 mmd_uvwSphere : TEXCOORD5;
#endif
};
#endif

#ifndef LIGHTMAP_OFF
// float4 unity_LightmapST;
#endif
float4 _MainTex_ST;

v2f_surf vert_surf(appdata_full v)
{
	v2f_surf o;
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
	#ifndef LIGHTMAP_OFF
	o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif
	float3 worldN = mul((float3x3)_Object2World, SCALED_NORMAL);
	o.normal = worldN;

	half3 norm = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
	half3 eye = normalize(mul(UNITY_MATRIX_MV, v.vertex).xyz);
	o.mmd_uvwSphere = reflect(eye, norm);
	
	#ifndef DIRLIGHTMAP_OFF
	TANGENT_SPACE_ROTATION;
	o.viewDir = (half3)mul(rotation, ObjSpaceViewDir(v.vertex));
	#else
	#ifdef LIGHTMAP_OFF
	o.viewDir = (half3)WorldSpaceViewDir(v.vertex);
	#endif
	#endif
	
	#ifdef LIGHTMAP_OFF
	o.vlight = ShadeSH9(float4(worldN, 1.0));
	#ifdef VERTEXLIGHT_ON
	float3 worldPos = mul(_Object2World, v.vertex).xyz;
	o.vlight += Shade4PointLights(
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, worldPos, worldN );
	#endif // VERTEXLIGHT_ON
	o.vlight *= (half3)1.0 - _TempAmbientL;
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

inline half4 frag_core(in v2f_surf IN, half3 albedo, half alpha)
{
	half atten = LIGHT_ATTENUATION(IN);
	half shadowAtten = SHADOW_ATTENUATION2(IN);
	half3 c = 0;

	#ifdef LIGHTMAP_OFF
	half NdotL = dot(IN.normal, _WorldSpaceLightPos0.xyz);
	c = MMDLit_Lighting(
		albedo,
		NdotL,
		IN.normal,
		_WorldSpaceLightPos0.xyz,
		normalize(IN.viewDir),
		atten,
		shadowAtten);
	#endif // LIGHTMAP_OFF || DIRLIGHTMAP_OFF
	#ifdef LIGHTMAP_OFF
	c += albedo * IN.vlight;
	#endif // LIGHTMAP_OFF

	#ifndef LIGHTMAP_OFF
	#ifndef DIRLIGHTMAP_OFF
	half3 specColor;
	half4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	half4 lmIndTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd,unity_Lightmap, IN.lmap.xy);
	half3 lm = MMDLit_DirLightmap(
		IN.normal,
		lmtex,
		lmIndTex,
		normalize(IN.viewDir),
		0,
		specColor);
	#else // !DIRLIGHTMAP_OFF
	half4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	half3 lm = MMDLit_Lightmap(
		lmtex);
	#endif // !DIRLIGHTMAP_OFF
	atten = MMDLit_MulAtten(atten, shadowAtten);
	#ifdef SHADOWS_SCREEN
	#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	c = albedo * min(lm, atten*2);
	#else
	c = albedo * max(min(lm,(atten*2)*(half3)lmtex), lm*atten);
	#endif
	#else // SHADOWS_SCREEN
	c = albedo * lm;
	#endif // SHADOWS_SCREEN
	#ifndef DIRLIGHTMAP_OFF
	c += specColor;
	#else // !DIRLIGHTMAP_OFF
	#endif // !DIRLIGHTMAP_OFF
	#endif // LIGHTMAP_OFF

	return half4(c, alpha);
}

half4 frag_surf(v2f_surf IN) : COLOR
{
	half alpha;
	half3 albedo = MMDLit_GetAlbedo(IN.pack0.xy, IN.mmd_uvwSphere, alpha);
	#if (defined(SHADER_API_GLES) && !defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	// Fix: GPU Adreno 205(OpenGL ES 2.0) discard crash
	#else
	clip(alpha - (1.1 / 255.0)); // Simulate MMD
	#endif
	
	return frag_core(IN, albedo, alpha);
}

half4 frag_fast(v2f_surf IN) : COLOR
{
	return frag_core(IN, MMDLit_GetAlbedo(IN.pack0.xy, IN.mmd_uvwSphere), 1.0);
}
