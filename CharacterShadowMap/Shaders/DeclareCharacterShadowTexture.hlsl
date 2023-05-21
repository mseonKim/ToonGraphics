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
    float4 _CharShadowLightDirections[3];   // Additional Lights (= MainLight not included)
    float _CharShadowmapIndex;
    float _CharShadowLocalLightIndices[3];
    // float _LocalLightToCharShadowIdxTable[3];
    // float _charshadowpad00_;
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

float3 ApplyCharShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection, uint shadowmapIdx)
{
    bool isLocal = shadowmapIdx > 0;
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

float4 CharShadowWorldToView(float3 positionWS, uint shadowmapIdx = 0)
{
    return mul(_CharShadowViewM[shadowmapIdx], float4(positionWS, 1.0));
}
float4 CharShadowViewToHClip(float4 positionVS)
{
    return mul(_CharShadowProjM, positionVS);
}
float4 CharShadowWorldToHClip(float3 positionWS, uint shadowmapIdx = 0)
{
    return CharShadowViewToHClip(CharShadowWorldToView(positionWS, shadowmapIdx));
}
float4 CharShadowObjectToHClip(float3 positionOS, float3 normalWS, uint shadowmapIdx = 0)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0));

    // TODO: SUPPORT ADDITIONAL LIGHT DIRECITON
    if (shadowmapIdx == 0)
        positionWS = ApplyCharShadowBias(positionWS, normalWS, _MainLightPosition.xyz, 0);
    else
        positionWS = ApplyCharShadowBias(positionWS, normalWS, _CharShadowLightDirections[shadowmapIdx - 1], shadowmapIdx);
    return CharShadowWorldToHClip(positionWS, shadowmapIdx);
}
float4 CharShadowObjectToHClipWithoutBias(float3 positionOS, uint shadowmapIdx = 0)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0));
    return CharShadowWorldToHClip(positionWS, shadowmapIdx);
}


half SampleCharacterShadowmap(float2 uv, float z, uint shadowmapIdx = 0)
{
    float var = SAMPLE_TEXTURE2D_ARRAY(_CharShadowAtlas, sampler_CharShadowAtlas, uv, shadowmapIdx).r;
    return var > z;
}

half SampleCharacterShadowmapFiltered(float2 uv, float z, uint shadowmapIdx = 0)
{
    z += 0.00001;
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    // real fetchesWeights[16];
    // real2 fetchesUV[16];
    // SampleShadow_ComputeSamples_Tent_7x7(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleCharacterShadowmap(fetchesUV[0].xy, z, shadowmapIdx)
                + fetchesWeights[1] * SampleCharacterShadowmap(fetchesUV[1].xy, z, shadowmapIdx)
                + fetchesWeights[2] * SampleCharacterShadowmap(fetchesUV[2].xy, z, shadowmapIdx)
                + fetchesWeights[3] * SampleCharacterShadowmap(fetchesUV[3].xy, z, shadowmapIdx)
                + fetchesWeights[4] * SampleCharacterShadowmap(fetchesUV[4].xy, z, shadowmapIdx)
                + fetchesWeights[5] * SampleCharacterShadowmap(fetchesUV[5].xy, z, shadowmapIdx)
                + fetchesWeights[6] * SampleCharacterShadowmap(fetchesUV[6].xy, z, shadowmapIdx)
                + fetchesWeights[7] * SampleCharacterShadowmap(fetchesUV[7].xy, z, shadowmapIdx)
                + fetchesWeights[8] * SampleCharacterShadowmap(fetchesUV[8].xy, z, shadowmapIdx);
                // + fetchesWeights[9] * SampleCharacterShadowmap(fetchesUV[9].xy, z)
                // + fetchesWeights[10] * SampleCharacterShadowmap(fetchesUV[10].xy, z)
                // + fetchesWeights[11] * SampleCharacterShadowmap(fetchesUV[11].xy, z)
                // + fetchesWeights[12] * SampleCharacterShadowmap(fetchesUV[12].xy, z)
                // + fetchesWeights[13] * SampleCharacterShadowmap(fetchesUV[13].xy, z)
                // + fetchesWeights[14] * SampleCharacterShadowmap(fetchesUV[14].xy, z)
                // + fetchesWeights[15] * SampleCharacterShadowmap(fetchesUV[15].xy, z);

    // return attenuation;
    float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, shadowmapIdx > 0);
    return LinearStep_(offset - 0.1, offset, attenuation);
}


half SampleTransparentShadowmap(float2 uv, float z, SamplerState s, uint shadowmapIdx = 0)
{
    return SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowAtlas, s, uv, shadowmapIdx).r > z;
}

half SampleTransparentShadowmapFiltered(float2 uv, float z, SamplerState s, uint shadowmapIdx = 0)
{
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharTransparentShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleTransparentShadowmap(fetchesUV[0].xy, z, s, shadowmapIdx)
                + fetchesWeights[1] * SampleTransparentShadowmap(fetchesUV[1].xy, z, s, shadowmapIdx)
                + fetchesWeights[2] * SampleTransparentShadowmap(fetchesUV[2].xy, z, s, shadowmapIdx)
                + fetchesWeights[3] * SampleTransparentShadowmap(fetchesUV[3].xy, z, s, shadowmapIdx)
                + fetchesWeights[4] * SampleTransparentShadowmap(fetchesUV[4].xy, z, s, shadowmapIdx)
                + fetchesWeights[5] * SampleTransparentShadowmap(fetchesUV[5].xy, z, s, shadowmapIdx)
                + fetchesWeights[6] * SampleTransparentShadowmap(fetchesUV[6].xy, z, s, shadowmapIdx)
                + fetchesWeights[7] * SampleTransparentShadowmap(fetchesUV[7].xy, z, s, shadowmapIdx)
                + fetchesWeights[8] * SampleTransparentShadowmap(fetchesUV[8].xy, z, s, shadowmapIdx);

    float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, shadowmapIdx > 0);
    return LinearStep_(offset - 0.1, offset, attenuation);
}

half TransparentAttenuation(float2 uv, float opacity, uint shadowmapIdx = 0)
{
    // Saturate since texture could have value more than 1
    return saturate(SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowAtlas, sampler_CharShadowAtlas, uv, shadowmapIdx).a - opacity);    // Total alpha sum - current pixel's alpha
}

half GetTransparentShadow(float2 uv, float z, float opacity, uint shadowmapIdx = 0)
{
    half hidden = SampleTransparentShadowmapFiltered(uv, z, sampler_CharShadowAtlas, shadowmapIdx);
    half atten = TransparentAttenuation(uv, opacity, shadowmapIdx);
    return min(hidden, atten);
}

half GetCharacterAndTransparentShadowmap(float2 uv, float z, float opacity, int shadowmapIdx = 0)
{
    return max(SampleCharacterShadowmapFiltered(uv, z, shadowmapIdx), GetTransparentShadow(uv, z, opacity, shadowmapIdx));
}

uint LocalLightIndexToShadowmapIndex(int lightindex)
{
    for (int i = 0; i < 3; i++)
    {
        if (lightindex == (int)_CharShadowLocalLightIndices[i])
        {
            return (uint)(i + 1);
        }
    }
    return MAX_CHAR_SHADOWMAPS;
}

#endif