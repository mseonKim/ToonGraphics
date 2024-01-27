#ifndef CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED
#define CHARACTER_SHADOW_DEPTH_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "./CharacterShadowTransforms.hlsl"

#define MAX_CHAR_SHADOWMAPS 1

half LinearStep_(float m, float M, float x)
{
    return saturate((x - m) / (M - m));
}

uint LocalLightIndexToShadowmapIndex(int lightindex)
{
    if (_UseBrightestLight > 0 && lightindex == _CharShadowLocalLightIndex)
        return 0;

    return MAX_CHAR_SHADOWMAPS;
}

#define ADDITIONAL_CHARSHADOW_CHECK(i, lightIndex) { \
        i = LocalLightIndexToShadowmapIndex(lightIndex); \
        if (i >= MAX_CHAR_SHADOWMAPS) \
            return 0; }

float3 TransformWorldToCharShadowCoord(float3 worldPos)
{
    float4 clipPos = CharShadowWorldToHClip(worldPos);
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

half SampleCharacterShadowmap(float2 uv, float z)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    float var = SAMPLE_TEXTURE2D(_CharShadowMap, sampler_CharShadowMap, uv).r;
    return (var - z) > _CharShadowBias.x;
}

half SampleCharacterShadowmapFiltered(float2 uv, float z)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    z += 0.00001;
#ifdef _HIGH_CHAR_SOFTSHADOW
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleCharacterShadowmap(fetchesUV[0].xy, z)
                + fetchesWeights[1] * SampleCharacterShadowmap(fetchesUV[1].xy, z)
                + fetchesWeights[2] * SampleCharacterShadowmap(fetchesUV[2].xy, z)
                + fetchesWeights[3] * SampleCharacterShadowmap(fetchesUV[3].xy, z)
                + fetchesWeights[4] * SampleCharacterShadowmap(fetchesUV[4].xy, z)
                + fetchesWeights[5] * SampleCharacterShadowmap(fetchesUV[5].xy, z)
                + fetchesWeights[6] * SampleCharacterShadowmap(fetchesUV[6].xy, z)
                + fetchesWeights[7] * SampleCharacterShadowmap(fetchesUV[7].xy, z)
                + fetchesWeights[8] * SampleCharacterShadowmap(fetchesUV[8].xy, z);
#else
    float ow = _CharShadowmapSize.x * _CharShadowCascadeParams.y;
    float oh = _CharShadowmapSize.y * _CharShadowCascadeParams.y;
    float attenuation = SampleCharacterShadowmap(uv, z)
                + SampleCharacterShadowmap(uv + float2(ow, ow), z)
                + SampleCharacterShadowmap(uv + float2(ow, -ow), z)
                + SampleCharacterShadowmap(uv + float2(-ow, ow), z)
                + SampleCharacterShadowmap(uv + float2(-ow, -ow), z);
    attenuation /= 5.0f;
#endif

    // float offset = _CharShadowStepOffset;
    // return LinearStep_(offset - 0.1, offset, attenuation);
    return attenuation;
}


half SampleTransparentShadowmap(float2 uv, float z, SamplerState s)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    float var = SAMPLE_TEXTURE2D(_TransparentShadowMap, s, uv).r;
    return (var - z) > _CharShadowBias.x;
}

half SampleTransparentShadowmapFiltered(float2 uv, float z, SamplerState s)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    z += 0.00001;
#if _HIGH_CHAR_SOFTSHADOW
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_CharTransparentShadowmapSize, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleTransparentShadowmap(fetchesUV[0].xy, z, s)
                + fetchesWeights[1] * SampleTransparentShadowmap(fetchesUV[1].xy, z, s)
                + fetchesWeights[2] * SampleTransparentShadowmap(fetchesUV[2].xy, z, s)
                + fetchesWeights[3] * SampleTransparentShadowmap(fetchesUV[3].xy, z, s)
                + fetchesWeights[4] * SampleTransparentShadowmap(fetchesUV[4].xy, z, s)
                + fetchesWeights[5] * SampleTransparentShadowmap(fetchesUV[5].xy, z, s)
                + fetchesWeights[6] * SampleTransparentShadowmap(fetchesUV[6].xy, z, s)
                + fetchesWeights[7] * SampleTransparentShadowmap(fetchesUV[7].xy, z, s)
                + fetchesWeights[8] * SampleTransparentShadowmap(fetchesUV[8].xy, z, s);
#else
    float ow = _CharTransparentShadowmapSize.x * _CharShadowCascadeParams.y;
    float oh = _CharTransparentShadowmapSize.y * _CharShadowCascadeParams.y;
    float attenuation = SampleTransparentShadowmap(uv, z, s)
                + SampleTransparentShadowmap(uv + float2(ow, ow), z, s)
                + SampleTransparentShadowmap(uv + float2(ow, -ow), z, s)
                + SampleTransparentShadowmap(uv + float2(-ow, ow), z, s)
                + SampleTransparentShadowmap(uv + float2(-ow, -ow), z, s);
    attenuation /= 5.0f;
#endif
    // float offset = _CharShadowStepOffset;
    // return LinearStep_(offset - 0.2, offset, attenuation);
    return attenuation;
}

half TransparentAttenuation(float2 uv, float opacity)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    // Saturate since texture could have value more than 1
    return saturate(SAMPLE_TEXTURE2D(_TransparentAlphaSum, sampler_CharShadowMap, uv).r - opacity);    // Total alpha sum - current pixel's alpha
}

half GetTransparentShadow(float2 uv, float z, float opacity)
{
    // UV must be the scaled value with ScaleUVForCascadeCharShadow()
    half hidden = SampleTransparentShadowmapFiltered(uv, z, sampler_CharShadowMap);
    // half hidden = SampleTransparentShadowmap(uv, z, sampler_CharShadowMap, shadowmapIdx);
    half atten = TransparentAttenuation(uv, opacity);
    return min(hidden, atten);
}

half CharacterAndTransparentShadowmap(float2 uv, float z, float opacity)
{
    // Scale uv first for cascade char shadow map
    ScaleUVForCascadeCharShadow(uv);
    return max(SampleCharacterShadowmapFiltered(uv, z), GetTransparentShadow(uv, z, opacity));
    // return max(SampleCharacterShadowmap(uv, z, shadowmapIdx), GetTransparentShadow(uv, z, opacity, shadowmapIdx));
}

half SampleCharacterAndTransparentShadow(float3 worldPos, float opacity)
{
    if (dot(_MainLightPosition.xyz, _BrightestLightDirection.xyz) < 0.9999)
        return 0;

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

    float3 coord = TransformWorldToCharShadowCoord(worldPos);
    return CharacterAndTransparentShadowmap(coord.xy, coord.z, opacity);
}

#endif