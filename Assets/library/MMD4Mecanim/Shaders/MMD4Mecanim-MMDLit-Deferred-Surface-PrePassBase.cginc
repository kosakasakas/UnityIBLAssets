#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_PREPASSBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

#include "MMD4Mecanim-MMDLit-Deferred-Surface-Lighting.cginc"

struct v2f_surf
{
	float4 pos : SV_POSITION;
	fixed3 normal : TEXCOORD0;
};

v2f_surf vert_fast(appdata_full v)
{
	v2f_surf o;
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.normal = mul((float3x3)_Object2World, SCALED_NORMAL);
	return o;
}

v2f_surf vert_surf(appdata_full v)
{
	v2f_surf o;
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.normal = mul((float3x3)_Object2World, SCALED_NORMAL);
	return o;
}

fixed4 frag_fast(v2f_surf IN) : COLOR
{
	return fixed4(IN.normal * 0.5 + 0.5, 0.0); // No supported specular.
}

fixed4 frag_surf(v2f_surf IN) : COLOR
{
	return fixed4(IN.normal * 0.5 + 0.5, 0.0); // No supported specular.
}
