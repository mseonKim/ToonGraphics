#ifndef CHARACTER_SHADOW_TRANSFORMS_INCLUDED
#define CHARACTER_SHADOW_TRANSFORMS_INCLUDED

#include "./CharacterShadowInput.hlsl"

#define _CharShadowCullingDist -(_CharShadowCascadeParams.x - 2) // should be less than renderer feature's max cascade split value 

float3 ApplyCharShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection, uint shadowmapIdx)
{
    bool isLocal = shadowmapIdx > 0;
    float depthBias = lerp(_CharShadowBias.x, _CharShadowBias.z, isLocal);
    float normalBias = lerp(_CharShadowBias.y, _CharShadowBias.w, isLocal);

    // Depth Bias
    positionWS = lightDirection * depthBias + positionWS;

    // Normal Bias
    float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
    float scale = invNdotL * -normalBias;
    // float scale = normalBias;
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

// Skip if too far (since we don't use mipmap for charshadowmap, manually cull this calculation based on view depth.)
bool IfCharShadowCulled(float viewPosZ)
{
    return viewPosZ < _CharShadowCullingDist;
}

#endif