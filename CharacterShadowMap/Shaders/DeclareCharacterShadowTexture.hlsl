#ifndef CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED
#define CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#define MAX_CHAR_SHADOWMAPS 4

half LinearStep_(float m, float M, float x)
{
    return saturate((x - m) / (M - m));
}


half SampleCharacterShadowmap(float2 uv, float z, uint shadowmapIdx = 0)
{
    float var = SAMPLE_TEXTURE2D_ARRAY(_CharShadowMap, sampler_CharShadowMap, uv, shadowmapIdx).r;
    return var - z > lerp(_CharShadowBias.x, _CharShadowBias.z, shadowmapIdx > 0);
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

    // float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, shadowmapIdx > 0);
    // return LinearStep_(offset - 0.1, offset, attenuation);
    return smoothstep(0, 1, attenuation);
}


half SampleTransparentShadowmap(float2 uv, float z, SamplerState s, uint shadowmapIdx = 0)
{
    return SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowMap, s, uv, shadowmapIdx).r > z;
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
    // return smoothstep(0, 1, attenuation);
}

half TransparentAttenuation(float2 uv, float opacity, uint shadowmapIdx = 0)
{
    // Saturate since texture could have value more than 1
    return saturate(SAMPLE_TEXTURE2D_ARRAY(_TransparentAlphaSum, sampler_CharShadowMap, uv, shadowmapIdx).r - opacity);    // Total alpha sum - current pixel's alpha
}

half GetTransparentShadow(float2 uv, float z, float opacity, uint shadowmapIdx = 0)
{
    half hidden = SampleTransparentShadowmapFiltered(uv, z, sampler_CharShadowMap, shadowmapIdx);
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