#ifndef CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED
#define CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "./CharacterShadowInput.hlsl"

#define MAX_CHAR_SHADOWMAPS 4

half LinearStep_(float m, float M, float x)
{
    return saturate((x - m) / (M - m));
}

void ScaleUVForCascadeCharShadow(inout float2 uv)
{
    // Refactor below logic to MAD
    // uv *= _CharShadowCascadeParams.y;
    // uv = (1.0 - _CharShadowCascadeParams.y) * 0.5 + uv;
    uv = uv * _CharShadowCascadeParams.y - (_CharShadowCascadeParams.y * 0.5 + 0.5);
}

half SampleCharacterShadowmap(float2 uv, float z, uint shadowmapIdx = 0)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    float var = SAMPLE_TEXTURE2D_ARRAY(_CharShadowMap, sampler_CharShadowMap, uv, shadowmapIdx).r;
    return var - z > lerp(_CharShadowBias.x, _CharShadowBias.z, shadowmapIdx > 0);
}

half SampleCharacterShadowmapFiltered(float2 uv, float z, uint shadowmapIdx = 0)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    z += 0.00001;
#ifdef _HighCharSoftShadow
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleCharacterShadowmap(fetchesUV[0].xy, z, shadowmapIdx)
                + fetchesWeights[1] * SampleCharacterShadowmap(fetchesUV[1].xy, z, shadowmapIdx)
                + fetchesWeights[2] * SampleCharacterShadowmap(fetchesUV[2].xy, z, shadowmapIdx)
                + fetchesWeights[3] * SampleCharacterShadowmap(fetchesUV[3].xy, z, shadowmapIdx)
                + fetchesWeights[4] * SampleCharacterShadowmap(fetchesUV[4].xy, z, shadowmapIdx)
                + fetchesWeights[5] * SampleCharacterShadowmap(fetchesUV[5].xy, z, shadowmapIdx)
                + fetchesWeights[6] * SampleCharacterShadowmap(fetchesUV[6].xy, z, shadowmapIdx)
                + fetchesWeights[7] * SampleCharacterShadowmap(fetchesUV[7].xy, z, shadowmapIdx)
                + fetchesWeights[8] * SampleCharacterShadowmap(fetchesUV[8].xy, z, shadowmapIdx);
#else
    float ow = _CharShadowmapSize.x;
    float oh = _CharShadowmapSize.y;
    float attenuation = SampleCharacterShadowmap(uv, z, shadowmapIdx)
                + SampleCharacterShadowmap(uv + float2(ow, ow), z, shadowmapIdx)
                + SampleCharacterShadowmap(uv + float2(ow, -ow), z, shadowmapIdx)
                + SampleCharacterShadowmap(uv + float2(-ow, ow), z, shadowmapIdx)
                + SampleCharacterShadowmap(uv + float2(-ow, -ow), z, shadowmapIdx);
    attenuation /= 5.0f;
#endif

    // float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, shadowmapIdx > 0);
    // return LinearStep_(offset - 0.1, offset, attenuation);
    return smoothstep(0, 1, attenuation);
}


half SampleTransparentShadowmap(float2 uv, float z, SamplerState s, uint shadowmapIdx = 0)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    return SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowMap, s, uv, shadowmapIdx).r > z;
}

half SampleTransparentShadowmapFiltered(float2 uv, float z, SamplerState s, uint shadowmapIdx = 0)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    z += 0.00001;
#ifdef _HighCharSoftShadow
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
#else
    float ow = _CharTransparentShadowmapSize.x;
    float oh = _CharTransparentShadowmapSize.y;
    float attenuation = SampleTransparentShadowmap(uv, z, s, shadowmapIdx)
                + SampleTransparentShadowmap(uv + float2(ow, ow), z, s, shadowmapIdx)
                + SampleTransparentShadowmap(uv + float2(ow, -ow), z, s, shadowmapIdx)
                + SampleTransparentShadowmap(uv + float2(-ow, ow), z, s, shadowmapIdx)
                + SampleTransparentShadowmap(uv + float2(-ow, -ow), z, s, shadowmapIdx);
    attenuation /= 5.0f;
#endif
    float offset = lerp(_CharShadowStepOffset.x, _CharShadowStepOffset.y, shadowmapIdx > 0);
    return LinearStep_(offset - 0.2, offset, attenuation);
    // return smoothstep(0, 1, attenuation);
}

half TransparentAttenuation(float2 uv, float opacity, uint shadowmapIdx = 0)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    // Saturate since texture could have value more than 1
    return saturate(SAMPLE_TEXTURE2D_ARRAY(_TransparentAlphaSum, sampler_CharShadowMap, uv, shadowmapIdx).r - opacity);    // Total alpha sum - current pixel's alpha
}

half GetTransparentShadow(float2 uv, float z, float opacity, uint shadowmapIdx = 0)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    half hidden = SampleTransparentShadowmapFiltered(uv, z, sampler_CharShadowMap, shadowmapIdx);
    // half hidden = SampleTransparentShadowmap(uv, z, sampler_CharShadowMap, shadowmapIdx);
    half atten = TransparentAttenuation(uv, opacity, shadowmapIdx);
    return min(hidden, atten);
}

half GetCharacterAndTransparentShadowmap(float2 uv, float z, float opacity, int shadowmapIdx = 0)
{
    // Scale uv first for cascade char shadow map
    ScaleUVForCascadeCharShadow(uv);
    return max(SampleCharacterShadowmapFiltered(uv, z, shadowmapIdx), GetTransparentShadow(uv, z, opacity, shadowmapIdx));
    // return max(SampleCharacterShadowmap(uv, z, shadowmapIdx), GetTransparentShadow(uv, z, opacity, shadowmapIdx));
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