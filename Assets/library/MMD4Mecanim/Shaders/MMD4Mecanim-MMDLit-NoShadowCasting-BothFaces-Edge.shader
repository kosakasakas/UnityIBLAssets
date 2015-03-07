Shader "MMD4Mecanim/MMDLit-NoShadowCasting-BothFaces-Edge"
{
	Properties
	{
		_Color("Diffuse", Color) = (1,1,1,1)
		_Specular("Specular", Color) = (1,1,1) // Memo: Postfix from material.(Revision>=0)
		_Ambient("Ambient", Color) = (1,1,1)
		_Shininess("Shininess", Float) = 0
		_ShadowLum("ShadowLum", Range(0,10)) = 1.5
		_SelfShadow("SelfShadow", Range(0,1)) = 0 // Memo: Postfix from material.(Revisin>=0, Reset to 0/1)
		_EdgeColor("EdgeColor", Color) = (0,0,0,1)
		_EdgeScale("EdgeScale", Range(0,2)) = 0 // Memo: Postfix from material.(Revision>=0)
		_EdgeSize("EdgeSize", float) = 0 // Memo: Postfix from material.(Revision>=0)
		_MainTex("MainTex", 2D) = "white" {}
		_ToonTex("ToonTex", 2D) = "white" {}

		_SphereCube("SphereCube", Cube) = "white" {} // Memo: Postfix from material.(Revision>=0)
		_SphereMode("SphereMode", Float) = -1.0 // Memo: Sphere material setting trigger.(Reset to 0/1/2/3)
		_SphereMul("SphereMul", Float) = 0.0
		_SphereAdd("SphereAdd", Float) = 0.0

		_Emissive("Emissive", Color) = (0,0,0,0)
		_ALPower("ALPower", Float) = 0

		_AddLightToonCen("AddLightToonCen", Float) = -0.1
		_AddLightToonMin("AddLightToonMin", Float) = 0.5

		_DefLightDir0("DefLightDir0",Vector) = (0,0,1,1)
		_DefLightColor0("DefLightColor0", Color) = (1,1,1,1) // Premultiply lightColor x atten x 2.0

		_TempDiffuse("TempDiffuse", Color) = (0.8,0.8,0.8,1) // Memo: Prefix color.(Revision>=0)
		_TempAmbient("TempAmbient", Color) = (0.2,0.2,0.2) // Memo: Prefix color.(Revision>=0)
		_TempAmbientL("TempAmbientL", Color) = (0.0,0.0,0.0) // Memo: Prefix color.(Revision>=0)

		_Revision("Revision",Float) = -1.0 // Memo: Shader setting trigger.(Reset to 0<=)
	}

	SubShader
	{
		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "ForceNoShadowCasting" = "True" }
		LOD 200

		Cull Off
		ZWrite On
		Blend Off

		Pass {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma target 2.0
			#pragma exclude_renderers flash
			#pragma vertex vert_surf
			#pragma fragment frag_fast
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase
			#include "MMD4Mecanim-MMDLit-Surface-ForwardBase.cginc"
			ENDCG
		}

		Pass {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardAdd" }

			ZWrite Off Blend One One Fog { Color (0,0,0,0) }
			CGPROGRAM
			#pragma target 2.0
			#pragma exclude_renderers flash
			#pragma vertex vert_surf
			#pragma fragment frag_fast
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdadd
			#include "MMD4Mecanim-MMDLit-Surface-ForwardAdd.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			Fog {Mode Off}
			ZWrite On ZTest LEqual Cull Off
			Offset 1, 1
			CGPROGRAM
			#pragma target 2.0
			#pragma exclude_renderers flash
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcaster
			#include "MMD4Mecanim-MMDLit-Surface-ShadowCaster.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCollector"
			Tags { "LightMode" = "ShadowCollector" }
			Fog {Mode Off}
			ZWrite On ZTest LEqual
			CGPROGRAM
			#pragma target 2.0
			#pragma exclude_renderers flash
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcollector
			#include "MMD4Mecanim-MMDLit-Surface-ShadowCollector.cginc"
			ENDCG
		}

		Cull Front
		ZWrite On
		ZTest Less
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB
		Offset 2.5,0

		Pass {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma target 2.0
			#pragma exclude_renderers flash
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase
			#include "MMD4Mecanim-MMDLit-SurfaceEdge-ForwardBase.cginc"
			ENDCG
		}

		Pass {
			Name "FORWARD"
			Tags { "LightMode" = "ForwardAdd" }

			ZWrite Off Blend One One Fog { Color (0,0,0,0) }
			CGPROGRAM
			#pragma target 2.0
			#pragma exclude_renderers flash
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdadd
			#include "MMD4Mecanim-MMDLit-SurfaceEdge-ForwardAdd.cginc"
			ENDCG
		}
	}

	Fallback Off
}
