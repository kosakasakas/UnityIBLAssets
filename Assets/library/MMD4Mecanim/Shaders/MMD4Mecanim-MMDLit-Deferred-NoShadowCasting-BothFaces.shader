Shader "MMD4Mecanim/Deferred/MMDLit-NoShadowCasting-BothFaces"
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
			Name "PREPASS"
			Tags { "LightMode" = "PrePassBase" }
			Fog {Mode Off}
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_fast
			#pragma fragment frag_fast
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "MMD4Mecanim-MMDLit-Deferred-Surface-PrePassBase.cginc"
			ENDCG
		}

		Pass {
			Name "PREPASS"
			Tags { "LightMode" = "PrePassFinal" }
			//ZWrite Off // ZWrite On for Deferred Transparent.
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_fast
			#pragma fragment frag_fast
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_prepassfinal
			#include "MMD4Mecanim-MMDLit-Deferred-Surface-PrePassFinal.cginc"
			ENDCG
		}
	}

	Fallback "MMD4Mecanim/MMDLit-NoShadowCasting-BothFaces"
}
