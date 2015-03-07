#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#define UNITY_PASS_SHADOWCOLLECTOR
#define SHADOW_COLLECTOR_PASS
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

struct v2f_surf {
  V2F_SHADOW_COLLECTOR;
};
v2f_surf vert_surf (appdata_full v) {
  v2f_surf o;
  TRANSFER_SHADOW_COLLECTOR(o)
  return o;
}
fixed4 frag_surf (v2f_surf IN) : COLOR {
  SHADOW_COLLECTOR_FRAGMENT(IN)
}
