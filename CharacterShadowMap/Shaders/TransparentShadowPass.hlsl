#ifndef TRANSPARENT_SHADOW_PASS_INCLUDED
#define TRANSPARENT_SHADOW_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DeclareCharacterShadowTexture.hlsl"

// Below material properties must be declared in seperate shader input to make compatible with SRP Batcher.
// CBUFFER_START(UnityPerMaterial)
// float4 _BaseColor;
// float4 _MainTex_ST;
// float4 _ClippingMask_ST;
// CBUFFER_END
// TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
// TEXTURE2D(_ClippingMask);

struct Attributes
{
    float4 position : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct ShadowVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD;
};

struct AlphaSumVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD;
    float3 positionWS : TEXCOORD1;
};

ShadowVaryings TransparentShadowVert(Attributes input)
{
    ShadowVaryings output = (ShadowVaryings)0;
    output.uv = input.texcoord;
    output.positionCS = CharShadowObjectToHClipWithoutBias(input.position.xyz, (uint)_CharShadowmapIndex);
#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    output.positionCS.xy *= _CharShadowCascadeParams.y;
    return output;
}

AlphaSumVaryings TransparentAlphaSumVert(Attributes input)
{
    AlphaSumVaryings output = (AlphaSumVaryings)0;
    output.uv = input.texcoord;
    output.positionCS = CharShadowObjectToHClipWithoutBias(input.position.xyz, (uint)_CharShadowmapIndex);
#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    output.positionCS.xy *= _CharShadowCascadeParams.y;
    output.positionWS = TransformObjectToWorld(input.position.xyz);
    return output;
}

float TransparentShadowFragment(ShadowVaryings input) : SV_Target
{
    // Use A Channel for alpha sum
    float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(input.uv, _MainTex)).a * _BaseColor.a;
    alpha *= SAMPLE_TEXTURE2D(_ClippingMask, sampler_MainTex, TRANSFORM_TEX(input.uv, _ClippingMask)).r;
    clip(alpha - 0.001);

    return input.positionCS.z;   // Depth
}


float TransparentAlphaSumFragment(AlphaSumVaryings input) : SV_Target
{
    // Use A Channel for alpha sum
    float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(input.uv, _MainTex)).a * _BaseColor.a;
    alpha *= SAMPLE_TEXTURE2D(_ClippingMask, sampler_MainTex, TRANSFORM_TEX(input.uv, _ClippingMask)).r;
    clip(alpha - 0.001);

    uint index = (uint)_CharShadowmapIndex;
    float4 clipPos = CharShadowWorldToHClip(input.positionWS, index);
    clipPos.xy *= _CharShadowCascadeParams.y;
    clipPos.z = 1.0;
    float3 ndc = clipPos.xyz / clipPos.w;
    float2 ssUV = ndc.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    ssUV.y = 1.0 - ssUV.y;
#endif

    // Discard behind fragment
    return lerp(alpha, 0, SampleCharacterShadowmap(ssUV, ndc.z, index));
}
#endif
