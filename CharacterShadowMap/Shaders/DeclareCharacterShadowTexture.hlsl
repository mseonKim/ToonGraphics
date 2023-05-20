#ifndef CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED
#define CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

CBUFFER_START(CharShadow)
    float4 _CharShadowBias;                 // x: main depth , y: main normal , z: local depth , w: local normal
    float4x4 _CharShadowViewM[4];
    float4x4 _CharShadowProjM;
    float4 _CharShadowOffset0;
    float4 _CharShadowOffset1;
    float4 _CharShadowmapSize;
    float4 _CharTransparentShadowmapSize;
    float4 _CharShadowStepOffset;           // x: main , y: local
    float _CharShadowmapIndex;
    float3 __CharShadowPad__;
    float4 _CharShadowLightDirections[3];   // Additional Lights (= MainLight not included)
CBUFFER_END

TEXTURE2D_ARRAY(_CharShadowAtlas);
SAMPLER(sampler_CharShadowAtlas);
TEXTURE2D_ARRAY(_TransparentShadowAtlas);
// SAMPLER(sampler_TransparentShadowAtlas);

#define MAX_CHAR_SHADOWMAPS 4

half LinearStep_(float m, float M, float x)
{
    return saturate((x - m) / (M - m));
}

float3 ApplyCharShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection, uint i)
{
    bool isLocal = i > 0;
    float depthBias = lerp(_CharShadowBias.x, _CharShadowBias.z, isLocal);
    float normalBias = lerp(_CharShadowBias.y, _CharShadowBias.w, isLocal);

    // Depth Bias
    positionWS = lightDirection * depthBias.xxx + positionWS;

    // Normal Bias
    // float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
    // float scale = invNdotL * -normalBias;
    float scale = normalBias;
    positionWS = normalWS * scale.xxx + positionWS;
    return positionWS;
}

float4 CharShadowWorldToView(float3 positionWS, uint i = 0)
{
    return mul(_CharShadowViewM[i], float4(positionWS, 1.0));
}
float4 CharShadowViewToHClip(float4 positionVS)
{
    return mul(_CharShadowProjM, positionVS);
}
float4 CharShadowWorldToHClip(float3 positionWS, uint i = 0)
{
    return CharShadowViewToHClip(CharShadowWorldToView(positionWS, i));
}
float4 CharShadowObjectToHClip(float3 positionOS, float3 normalWS, uint i = 0)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0));

    // TODO: SUPPORT ADDITIONAL LIGHT DIRECITON
    if (i == 0)
        positionWS = ApplyCharShadowBias(positionWS, normalWS, _MainLightPosition.xyz, i);
    else
        positionWS = ApplyCharShadowBias(positionWS, normalWS, _CharShadowLightDirections[i - 1], i);
    return CharShadowWorldToHClip(positionWS, i);
}
float4 CharShadowObjectToHClipWithoutBias(float3 positionOS, uint i = 0)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0));
    return CharShadowWorldToHClip(positionWS, i);
}


half SampleCharacterShadowmap(float2 uv, float z, uint i = 0)
{
    float var = SAMPLE_TEXTURE2D_ARRAY(_CharShadowAtlas, sampler_CharShadowAtlas, uv, i).r;
    return var > z;
}

half SampleCharacterShadowmapFiltered(float2 uv, float z, uint i = 0)
{
    z += 0.00001;
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    // real fetchesWeights[16];
    // real2 fetchesUV[16];
    // SampleShadow_ComputeSamples_Tent_7x7(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleCharacterShadowmap(fetchesUV[0].xy, z, i)
                + fetchesWeights[1] * SampleCharacterShadowmap(fetchesUV[1].xy, z, i)
                + fetchesWeights[2] * SampleCharacterShadowmap(fetchesUV[2].xy, z, i)
                + fetchesWeights[3] * SampleCharacterShadowmap(fetchesUV[3].xy, z, i)
                + fetchesWeights[4] * SampleCharacterShadowmap(fetchesUV[4].xy, z, i)
                + fetchesWeights[5] * SampleCharacterShadowmap(fetchesUV[5].xy, z, i)
                + fetchesWeights[6] * SampleCharacterShadowmap(fetchesUV[6].xy, z, i)
                + fetchesWeights[7] * SampleCharacterShadowmap(fetchesUV[7].xy, z, i)
                + fetchesWeights[8] * SampleCharacterShadowmap(fetchesUV[8].xy, z, i);
                // + fetchesWeights[9] * SampleCharacterShadowmap(fetchesUV[9].xy, z)
                // + fetchesWeights[10] * SampleCharacterShadowmap(fetchesUV[10].xy, z)
                // + fetchesWeights[11] * SampleCharacterShadowmap(fetchesUV[11].xy, z)
                // + fetchesWeights[12] * SampleCharacterShadowmap(fetchesUV[12].xy, z)
                // + fetchesWeights[13] * SampleCharacterShadowmap(fetchesUV[13].xy, z)
                // + fetchesWeights[14] * SampleCharacterShadowmap(fetchesUV[14].xy, z)
                // + fetchesWeights[15] * SampleCharacterShadowmap(fetchesUV[15].xy, z);

    // return attenuation;
    float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, i > 0);
    return LinearStep_(offset - 0.1, offset, attenuation);
}


half SampleTransparentShadowmap(float2 uv, float z, SamplerState s, uint i = 0)
{
    return SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowAtlas, s, uv, i).r > z;
}

half SampleTransparentShadowmapFiltered(float2 uv, float z, SamplerState s, uint i = 0)
{
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharTransparentShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleTransparentShadowmap(fetchesUV[0].xy, z, s, i)
                + fetchesWeights[1] * SampleTransparentShadowmap(fetchesUV[1].xy, z, s, i)
                + fetchesWeights[2] * SampleTransparentShadowmap(fetchesUV[2].xy, z, s, i)
                + fetchesWeights[3] * SampleTransparentShadowmap(fetchesUV[3].xy, z, s, i)
                + fetchesWeights[4] * SampleTransparentShadowmap(fetchesUV[4].xy, z, s, i)
                + fetchesWeights[5] * SampleTransparentShadowmap(fetchesUV[5].xy, z, s, i)
                + fetchesWeights[6] * SampleTransparentShadowmap(fetchesUV[6].xy, z, s, i)
                + fetchesWeights[7] * SampleTransparentShadowmap(fetchesUV[7].xy, z, s, i)
                + fetchesWeights[8] * SampleTransparentShadowmap(fetchesUV[8].xy, z, s, i);

    float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, i > 0);
    return LinearStep_(offset - 0.1, offset, attenuation);
}

half TransparentAttenuation(float2 uv, float opacity, uint i = 0)
{
    // Saturate since texture could have value more than 1
    return saturate(SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowAtlas, sampler_CharShadowAtlas, uv, i).a - opacity);    // Total alpha sum - current pixel's alpha
}

half GetTransparentShadow(float2 uv, float z, float opacity, uint i = 0)
{
    half hidden = SampleTransparentShadowmapFiltered(uv, z, sampler_CharShadowAtlas, i);
    half atten = TransparentAttenuation(uv, opacity, i);
    return min(hidden, atten);
}

half GetCharacterAndTransparentShadowmap(float2 uv, float z, float opacity, uint i = 0)
{
    if (i >= MAX_CHAR_SHADOWMAPS)
        return 0;
    return max(SampleCharacterShadowmapFiltered(uv, z, i), GetTransparentShadow(uv, z, opacity, i));
}

#endif