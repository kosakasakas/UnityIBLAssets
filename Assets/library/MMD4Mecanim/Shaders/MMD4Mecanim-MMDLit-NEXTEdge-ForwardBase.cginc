#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"

#include "MMD4Mecanim-MMDLit-NEXTEdge-Lighting.cginc"

struct v2f_surf {
  float4 pos : SV_POSITION;
  half3 vlight : TEXCOORD1;
  half3 viewDir : TEXCOORD2;
  half3 normal : TEXCOORD3;
};

v2f_surf vert_surf (appdata_full v)
{
	v2f_surf o;
	v.vertex = MMDLit_GetEdgeVertex(v.vertex, v.normal);
	o.pos = MMDLit_TransformEdgeVertex(v.vertex);
	o.normal = mul((float3x3)_Object2World, SCALED_NORMAL);
	o.viewDir = (half3)WorldSpaceViewDir(v.vertex);
	return o;
}

half4 frag_surf (v2f_surf IN) : COLOR
{
	half4 c = _EdgeColor;
	half3 viewDir = normalize(IN.viewDir);
	half r = saturate(dot(-viewDir, IN.normal));
	c.a *= r * ALPHA_SCALE;
	return c;
}
