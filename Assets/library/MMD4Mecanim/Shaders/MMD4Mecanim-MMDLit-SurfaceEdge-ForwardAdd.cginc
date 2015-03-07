#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_FORWARDADD
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "MMD4Mecanim-MMDLit-AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

#include "MMD4Mecanim-MMDLit-SurfaceEdge-Lighting.cginc"
struct v2f_surf {
	float4 pos : SV_POSITION;
//	fixed3 normal : TEXCOORD0;
//	half3 lightDir : TEXCOORD1;
//	half3 viewDir : TEXCOORD2;
	LIGHTING_COORDS(0,1)
};

v2f_surf vert_surf (appdata_full v)
{
	v2f_surf o;
	v.vertex = MMDLit_GetEdgeVertex(v.vertex, v.normal);
	o.pos = MMDLit_TransformEdgeVertex(v.vertex);
//	o.normal = mul((float3x3)_Object2World, SCALED_NORMAL);
//	o.lightDir = WorldSpaceLightDir(v.vertex);
//	o.viewDir = WorldSpaceViewDir(v.vertex);
	TRANSFER_VERTEX_TO_FRAGMENT(o);
	return o;
}

fixed4 frag_surf (v2f_surf IN) : COLOR
{
	half alpha;
	half3 albedo = MMDLit_GetAlbedo(alpha);
	half3 c = MMDLit_Lighting(albedo, LIGHT_ATTENUATION(IN));
	c = min(c, 1.0);
	c *= alpha;
	return fixed4(c, 0.0);
}
