#ifndef MMDLIT_LIGHTING_INCLUDED
#define MMDLIT_LIGHTING_INCLUDED

// UnityCG.cginc
inline half3 MMDLit_DecodeLightmap(half4 color)
{
#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	return (2.0 * (half3)color);
#else
	// potentially faster to do the scalar multiplication
	// in parenthesis for scalar GPUs
	return (8.0 * color.a) * (half3)color;
#endif
}

// Lighting.cginc
inline half3 MMDLit_DirLightmapDiffuse(in half3x3 dirBasis, half4 color, half4 scale, half3 normal, bool surfFuncWritesNormal, out half3 scalePerBasisVector)
{
	half3 lm = MMDLit_DecodeLightmap(color);
	
	// will be compiled out (and so will the texture sample providing the value)
	// if it's not used in the lighting function, like in LightingLambert
	scalePerBasisVector = MMDLit_DecodeLightmap(scale);

	// will be compiled out when surface function does not write into o.Normal
	if (surfFuncWritesNormal)
	{
		half3 normalInRnmBasis = saturate(mul(dirBasis, normal));
		lm *= dot (normalInRnmBasis, scalePerBasisVector);
	}

	return lm;
}

// UnityCG.cginc
inline half MMDLit_Luminance( half3 c )
{
	return dot( c, half3(0.22, 0.707, 0.071) );
}

inline half MMDLit_SpecularRefl( half3 normal, half3 lightDir, half3 viewDir, half s )
{
	//return pow(saturate(dot(normal, normalize(lightDir + viewDir))), s); // Buggy intel HD 4000
	return saturate(pow(saturate(dot(normal, normalize(lightDir + viewDir))), s));
}

#endif
