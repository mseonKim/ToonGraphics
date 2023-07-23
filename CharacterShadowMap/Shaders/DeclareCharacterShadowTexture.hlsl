#ifndef CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED
#define CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "./CharacterShadowTransforms.hlsl"

#define MAX_CHAR_SHADOWMAPS 4

half LinearStep_(float m, float M, float x)
{
    return saturate((x - m) / (M - m));
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

#define ADDITIONAL_CHARSHADOW_CHECK(i, lightIndex) { \
        i = LocalLightIndexToShadowmapIndex(lightIndex); \
        if (i >= MAX_CHAR_SHADOWMAPS) \
            return 0; }

float3 TransformWorldToCharShadowCoord(float3 worldPos, int shadowmapIdx = 0)
{
    float4 clipPos = CharShadowWorldToHClip(worldPos, shadowmapIdx);
#if UNITY_REVERSED_Z
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
    float3 ndc = clipPos.xyz / clipPos.w;
    float2 ssUV = ndc.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    ssUV.y = 1.0 - ssUV.y;
#endif
    return float3(ssUV, ndc.z);
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
    return (var - z) > lerp(_CharShadowBias.x, _CharShadowBias.z, shadowmapIdx > 0);
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
    float ow = _CharShadowmapSize.x * _CharShadowCascadeParams.y;
    float oh = _CharShadowmapSize.y * _CharShadowCascadeParams.y;
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
    float var = SAMPLE_TEXTURE2D_ARRAY(_TransparentShadowMap, s, uv, shadowmapIdx).r;
    return (var - z) > lerp(_CharShadowBias.x, _CharShadowBias.z, shadowmapIdx > 0);
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
    float ow = _CharTransparentShadowmapSize.x * _CharShadowCascadeParams.y;
    float oh = _CharTransparentShadowmapSize.y * _CharShadowCascadeParams.y;
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

half CharacterAndTransparentShadowmap(float2 uv, float z, float opacity, int shadowmapIdx = 0)
{
    // Scale uv first for cascade char shadow map
    ScaleUVForCascadeCharShadow(uv);
    return max(SampleCharacterShadowmapFiltered(uv, z, shadowmapIdx), GetTransparentShadow(uv, z, opacity, shadowmapIdx));
    // return max(SampleCharacterShadowmap(uv, z, shadowmapIdx), GetTransparentShadow(uv, z, opacity, shadowmapIdx));
}

half SampleCharacterAndTransparentShadow(float3 worldPos, float opacity)
{
    if (IfCharShadowCulled(TransformWorldToView(worldPos).z))
        return 0;

    float3 coord = TransformWorldToCharShadowCoord(worldPos);
    return CharacterAndTransparentShadowmap(coord.xy, coord.z, opacity);
}

half SampleAdditionalCharacterAndTransparentShadow(float3 worldPos, float opacity, int lightIndex = 0)
{
#ifndef USE_FORWARD_PLUS
    return 0;
#endif
    uint i;
    ADDITIONAL_CHARSHADOW_CHECK(i, lightIndex)

    if (IfCharShadowCulled(TransformWorldToView(worldPos).z))
        return 0;

    float3 coord = TransformWorldToCharShadowCoord(worldPos, i);
    return CharacterAndTransparentShadowmap(coord.xy, coord.z, opacity, i);
}

half GetLocalCharacterShadowmapForVoxelLighting(float3 worldPos, int lightIndex = 0)
{
#ifndef USE_FORWARD_PLUS
    return 0;
#endif
    uint i;
    ADDITIONAL_CHARSHADOW_CHECK(i, lightIndex)
        
    if (IfCharShadowCulled(TransformWorldToView(worldPos).z))
        return 0;

    float3 coord = TransformWorldToCharShadowCoord(worldPos, i);
    float2 uv = coord.xy;
    ScaleUVForCascadeCharShadow(uv);
    return SampleCharacterShadowmap(uv, coord.z, i);
}

#endif