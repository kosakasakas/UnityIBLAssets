#include "MMD4Mecanim-MMDLit-Lighting.cginc"

#define MMDLIT_GLOBALLIGHTING (0.6)
#define MMDLIT_EDGE_ZOFST (0.0005)

half4 _EdgeColor;
half _EdgeSize;

#ifdef UNITY_PASS_PREPASSFINAL
half4 unity_Ambient;
#endif

inline half3 MMDLit_GetAmbient()
{
#ifdef UNITY_PASS_PREPASSFINAL
	return (half3)unity_Ambient;
#else
	return 0;
#endif
}

inline float MMDLit_GetEdgeSize()
{
	return _EdgeSize;
}

inline float4 MMDLit_GetEdgeVertex(float4 vertex, float3 normal)
{
#if 1
	float edge_size = MMDLit_GetEdgeSize();
#else
	// Adjust edge_size by distance & fovY
	float4 world_pos = mul(UNITY_MATRIX_MV, vertex);
	float r_proj_y = UNITY_MATRIX_P[1][1];
	float edge_size = abs(MMDLit_GetEdgeSize() / r_proj_y * world_pos.z);
#endif
	return vertex + float4(normal.xyz * edge_size,0.0);
}

inline float4 MMDLit_TransformEdgeVertex(float4 vertex)
{
#if 0
	vertex = mul(UNITY_MATRIX_MVP, vertex);
	vertex.z += MMDLIT_EDGE_ZOFST * vertex.w;
	return vertex;
#else
	return mul(UNITY_MATRIX_MVP, vertex);
#endif
}

inline half MMDLit_GetAlpha()
{
	return _EdgeColor.a;
}

inline half3 MMDLit_GetAlbedo(out half alpha)
{
	alpha = _EdgeColor.a;
	return _EdgeColor.rgb;
}

inline half3 MMDLit_LightingFinal(half3 light)
{
	return max(light - MMDLit_GetAmbient(), 0.0) * MMDLIT_GLOBALLIGHTING + MMDLit_GetAmbient();
}

inline half4 MMDLit_DirLightmap(
	half4 color,
	half4 scale,
	half3 normal,
	bool surfFuncWritesNormal)
{
	UNITY_DIRBASIS
	half3 scalePerBasisVector;
	half3 lm = MMDLit_DirLightmapDiffuse(unity_DirBasis, color, scale, normal, surfFuncWritesNormal, scalePerBasisVector);
	return half4(lm, 0);
}
