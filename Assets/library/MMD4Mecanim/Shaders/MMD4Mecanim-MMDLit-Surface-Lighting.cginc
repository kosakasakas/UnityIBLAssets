#include "MMD4Mecanim-MMDLit-Lighting.cginc"

#define SUPPORT_SELFSHADOW

half4 _Color;
half4 _Specular;
half4 _Ambient;
half _Shininess;
half _ShadowLum;
half _SelfShadow;

sampler2D _MainTex;
sampler2D _ToonTex;

half _AddLightToonCen;
half _AddLightToonMin;

half4 _Emissive;

samplerCUBE _SphereCube;
half _SphereAdd;
half _SphereMul;

half4 _TempDiffuse;
half4 _TempAmbient;
half4 _TempAmbientL;

inline half3 MMDLit_Sphere(half3 col, half3 sph)
{
	col *= sph * _SphereMul + (1.0 - _SphereMul);
	col += sph * _SphereAdd;
	return col;
}

inline half3 MMDLit_GetAlbedo(float2 uv_MainTex, half3 uvw_Sphere)
{
	half3 c = (half3)tex2D(_MainTex, uv_MainTex);
	half3 s = (half3)texCUBE(_SphereCube, uvw_Sphere);
	return MMDLit_Sphere( c, s );
}

inline half3 MMDLit_GetAlbedo(float2 uv_MainTex, half3 uvw_Sphere, out half alpha)
{
	half4 c = tex2D(_MainTex, uv_MainTex);
	half3 s = (half3)texCUBE(_SphereCube, uvw_Sphere);
	alpha = c.a * _Color.a;
	return MMDLit_Sphere( (half3)c, s );
}

inline half MMDLit_GetToolRefl(half NdotL)
{
	return NdotL * 0.5 + 0.5;
}

inline half MMDLit_GetToonShadow(half toonRefl)
{
	half toonShadow = toonRefl * 2.0;
	return (half)saturate(toonShadow * toonShadow - 1.0);
}

inline half MMDLit_GetForwardAddStr(half toonRefl)
{
	half toonShadow = (toonRefl - _AddLightToonCen) * 2.0;
	return (half)clamp(toonShadow * toonShadow - 1.0, _AddLightToonMin, 1.0);
}

// for ForwardBase
inline half3 MMDLit_GetRamp(half NdotL, half shadowAtten)
{
	half refl = (NdotL * 0.5 + 0.5) * shadowAtten;
	half toonRefl = refl;
#ifdef SUPPORT_SELFSHADOW
	half selfShadowInv = 1.0 - _SelfShadow;
	refl = refl * selfShadowInv; // _SelfShadow = 1.0 as 0
#endif
	half3 ramp = (half3)tex2D(_ToonTex, half2(refl, refl));
#ifdef SUPPORT_SELFSHADOW
	half toonShadow = MMDLit_GetToonShadow(toonRefl);
	half3 rampSS = (1.0 - toonShadow) * ramp + toonShadow;
	ramp = rampSS * _SelfShadow + ramp * selfShadowInv;
#endif
	ramp = saturate(1.0 - (1.0 - ramp) * _ShadowLum);
	return ramp;
}

// for ForwardAdd
inline half3 MMDLit_GetRamp_Add(half toonRefl, half toonShadow)
{
	half refl = toonRefl;
#ifdef SUPPORT_SELFSHADOW
	half selfShadowInv = 1.0 - _SelfShadow;
	refl = refl * selfShadowInv; // _SelfShadow = 1.0 as 0
#endif
	half3 ramp = (half3)tex2D(_ToonTex, half2(refl, refl));
#ifdef SUPPORT_SELFSHADOW
	half3 rampSS = (1.0 - toonShadow) * ramp + toonShadow;
	ramp = rampSS * _SelfShadow + ramp * selfShadowInv;
#endif
	ramp = saturate(1.0 - (1.0 - ramp) * _ShadowLum);
	return ramp;
}

// for Lightmap, DirLightmap
inline half3 MMDLit_GetRamp_Lightmap()
{
	half3 ramp = tex2D(_ToonTex, float2(1.0, 1.0));
	ramp = saturate(1.0 - (1.0 - ramp) * _ShadowLum);
#ifdef SUPPORT_SELFSHADOW
	ramp = ramp * (1.0 - _SelfShadow) + _SelfShadow; // _SelfShadow = 1.0 as White
#endif
	// No shadowStr, because included lightColor.
	return (half3)ramp;
}

// DirLightmap
inline half3 MMDLit_GetRamp_DirLightmap(half NdotL, half lambertStr)
{
	half refl = (NdotL * 0.5 + 0.5);
#ifdef SUPPORT_SELFSHADOW
	half selfShadowInv = 1.0 - _SelfShadow;
	refl = refl * selfShadowInv; // _SelfShadow = 1.0 as 0
#endif
	half3 ramp = (half3)tex2D(_ToonTex, half2(refl, refl));
#ifdef SUPPORT_SELFSHADOW
	half3 rampSS = (1.0 - lambertStr) * ramp + lambertStr; // memo: Not use toonShadow.
	ramp = rampSS * _SelfShadow + ramp * selfShadowInv;
#endif
	ramp = saturate(1.0 - (1.0 - ramp) * _ShadowLum);
	// No shadowStr, because included lightColor.
	return ramp;
}

// for FORWARD_BASE
inline half3 MMDLit_Lighting(
	half3 albedo,
	half NdotL,
	half3 normal,
	half3 lightDir,
	half3 viewDir,
	half atten,
	half shadowAtten)
{
	half3 ramp = MMDLit_GetRamp(NdotL, shadowAtten);
	half3 lightColor = (half3)_LightColor0 * atten * 2.0;

	half3 c = _TempDiffuse * lightColor * ramp;
	c *= albedo;

	half refl = MMDLit_SpecularRefl(normal, lightDir, viewDir, _Shininess);
	c += (half3)_Specular * lightColor * refl;

	// AutoLuminous
	c += albedo * (half3)_Emissive;
	return c;
}

// for FORWARD_ADD
inline half3 MMDLit_Lighting_Add(
	half3 albedo,
	half toonRefl,
	half toonShadow,
	half3 normal,
	half3 lightDir,
	half3 viewDir,
	half atten)
{
	half3 ramp = MMDLit_GetRamp_Add(toonRefl, toonShadow);
	half3 lightColor = (half3)_LightColor0 * atten * 2.0;

	half3 c = _TempDiffuse * lightColor * ramp;
	c *= albedo;

	half refl = MMDLit_SpecularRefl(normal, lightDir, viewDir, _Shininess);
	c += (half3)_Specular * lightColor * refl;
	return c;
}

inline half MMDLit_MulAtten(half atten, half shadowAtten)
{
	return atten * shadowAtten;
}

inline half3 MMDLit_Lightmap(half4 lmtex)
{
	half3 lm = MMDLit_DecodeLightmap(lmtex);
	// lm = lightColor = _LightColor0.rgb * atten * 2.0
	half3 ramp = MMDLit_GetRamp_Lightmap();

	return _TempDiffuse * lm * ramp + _TempAmbient;
}

inline half3 MMDLit_DirLightmap(
	half3 normal,
	half4 color,
	half4 scale,
	half3 viewDir,
	bool surfFuncWritesNormal,
	out half3 specColor)
{
	UNITY_DIRBASIS
	half3 scalePerBasisVector;
	half3 lm = MMDLit_DirLightmapDiffuse (unity_DirBasis, color, scale, normal, surfFuncWritesNormal, scalePerBasisVector);
	half3 lightDir = normalize(scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2]);
	// lm = lightColor = _LightColor0.rgb * atten * 2.0

	half NdotL = dot(normal, lightDir);
	half lambertStr = max(NdotL, 0.0);
	half3 ramp = MMDLit_GetRamp_DirLightmap(NdotL, lambertStr);

	half3 c = _TempDiffuse * lm * ramp + _TempAmbient;

	half refl = MMDLit_SpecularRefl(normal, lightDir, viewDir, _Shininess);
	specColor = (half3)_Specular * lm * refl;
	return c;
}
