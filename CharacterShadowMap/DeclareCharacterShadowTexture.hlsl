#ifndef CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED
#define CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

float _CharShadowBias;
float4x4 _CharShadowViewM;
float4x4 _CharShadowProjM;
TEXTURE2D(_CharShadowAtlas);
SAMPLER(sampler_CharShadowAtlas);
float4 _CharShadowOffset0;
float4 _CharShadowOffset1;
float4 _CharShadowmapSize;
float _CharShadowStepOffset;
TEXTURE2D(_TransparentShadowAtlas);
// SAMPLER(sampler_TransparentShadowAtlas);

half LinearStep_(float m, float M, float x)
{
    return saturate((x - m) / (M - m));
}

float3 ApplyShadowBias(float3 positionWS, float3 lightDirection)
{
    positionWS = lightDirection * _CharShadowBias + positionWS;
    return positionWS;
}

float4 CharShadowWorldToView(float3 positionWS)
{
    return mul(_CharShadowViewM, float4(positionWS, 1.0));
}
float4 CharShadowViewToHClip(float4 positionVS)
{
    return mul(_CharShadowProjM, positionVS);
}
float4 CharShadowWorldToHClip(float3 positionWS)
{
    return CharShadowViewToHClip(CharShadowWorldToView(positionWS));
}
float4 CharShadowObjectToHClip(float3 positionOS)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0));
    positionWS = ApplyShadowBias(positionWS, _MainLightPosition.xyz);
    return CharShadowWorldToHClip(positionWS);
}
float4 CharShadowObjectToHClipWithoutBias(float3 positionOS)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0));
    return CharShadowWorldToHClip(positionWS);
}


half SampleCharacterShadowmap(float2 uv, float z)
{
    float var = SAMPLE_TEXTURE2D(_CharShadowAtlas, sampler_CharShadowAtlas, uv).r;
    return var > z;
}

half SampleCharacterShadowmapFiltered(float2 uv, float z)
{
    z += 0.00001;
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    // real fetchesWeights[16];
    // real2 fetchesUV[16];
    // SampleShadow_ComputeSamples_Tent_7x7(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleCharacterShadowmap(fetchesUV[0].xy, z)
                + fetchesWeights[1] * SampleCharacterShadowmap(fetchesUV[1].xy, z)
                + fetchesWeights[2] * SampleCharacterShadowmap(fetchesUV[2].xy, z)
                + fetchesWeights[3] * SampleCharacterShadowmap(fetchesUV[3].xy, z)
                + fetchesWeights[4] * SampleCharacterShadowmap(fetchesUV[4].xy, z)
                + fetchesWeights[5] * SampleCharacterShadowmap(fetchesUV[5].xy, z)
                + fetchesWeights[6] * SampleCharacterShadowmap(fetchesUV[6].xy, z)
                + fetchesWeights[7] * SampleCharacterShadowmap(fetchesUV[7].xy, z)
                + fetchesWeights[8] * SampleCharacterShadowmap(fetchesUV[8].xy, z);
                // + fetchesWeights[9] * SampleCharacterShadowmap(fetchesUV[9].xy, z);
                // + fetchesWeights[10] * SampleCharacterShadowmap(fetchesUV[10].xy, z);
                // + fetchesWeights[11] * SampleCharacterShadowmap(fetchesUV[11].xy, z);
                // + fetchesWeights[12] * SampleCharacterShadowmap(fetchesUV[12].xy, z);
                // + fetchesWeights[13] * SampleCharacterShadowmap(fetchesUV[13].xy, z);
                // + fetchesWeights[14] * SampleCharacterShadowmap(fetchesUV[14].xy, z);
                // + fetchesWeights[15] * SampleCharacterShadowmap(fetchesUV[15].xy, z);

    // return attenuation;
    // return 1.0 - step(attenuation, _CharShadowStepOffset);
    return LinearStep_(_CharShadowStepOffset - 0.1, _CharShadowStepOffset, attenuation);
}

half SampleTransparentShadowmap(float2 uv, float z, SamplerState s)
{
    return SAMPLE_TEXTURE2D(_TransparentShadowAtlas, s, uv).r > z;
}

half SampleTransparentShadowmapFiltered(float2 uv, float z, SamplerState s)
{
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleTransparentShadowmap(fetchesUV[0].xy, z, s)
                + fetchesWeights[1] * SampleTransparentShadowmap(fetchesUV[1].xy, z, s)
                + fetchesWeights[2] * SampleTransparentShadowmap(fetchesUV[2].xy, z, s)
                + fetchesWeights[3] * SampleTransparentShadowmap(fetchesUV[3].xy, z, s)
                + fetchesWeights[4] * SampleTransparentShadowmap(fetchesUV[4].xy, z, s)
                + fetchesWeights[5] * SampleTransparentShadowmap(fetchesUV[5].xy, z, s)
                + fetchesWeights[6] * SampleTransparentShadowmap(fetchesUV[6].xy, z, s)
                + fetchesWeights[7] * SampleTransparentShadowmap(fetchesUV[7].xy, z, s)
                + fetchesWeights[8] * SampleTransparentShadowmap(fetchesUV[8].xy, z, s);

    return 1.0 - step(attenuation, _CharShadowStepOffset);
}

half GetTransparentShadow(float2 uv, float z, float opacity)
{
    half hidden = SampleTransparentShadowmapFiltered(uv, z, sampler_CharShadowAtlas);
    // Saturate since texture could have value more than 1
    half atten = saturate(SAMPLE_TEXTURE2D(_TransparentShadowAtlas, sampler_CharShadowAtlas, uv).a - opacity);  // Total alpha sum - current pixel's alpha
    return min(hidden, atten);
}

half GetCharacterAndTransparentShadowmap(float2 uv, float z, float opacity)
{
    return max(SampleCharacterShadowmapFiltered(uv, z), GetTransparentShadow(uv, z, opacity));
}

#endif