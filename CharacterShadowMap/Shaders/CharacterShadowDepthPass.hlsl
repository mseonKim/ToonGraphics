#ifndef CHARACTER_SHADOW_DEPTH_PASS_INCLUDED
#define CHARACTER_SHADOW_DEPTH_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DeclareCharacterShadowTexture.hlsl"

struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    float3 normal     : NORMAL;
    // UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    // float3 positionWS   : TEXCOORD1;
    // UNITY_VERTEX_INPUT_INSTANCE_ID
    // UNITY_VERTEX_OUTPUT_STEREO
};

float4 _ClippingMask_ST;
TEXTURE2D(_ClippingMask); SAMPLER(sampler_ClippingMask);

Varyings CharShadowVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    // UNITY_SETUP_INSTANCE_ID(input);
    // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _ClippingMask);
    output.positionCS = CharShadowObjectToHClip(input.position.xyz, input.normal, (uint)_CharShadowmapIndex);

#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return output;
}

float CharShadowFragment(Varyings input) : SV_TARGET
{
    // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float alphaClipVar = SAMPLE_TEXTURE2D(_ClippingMask, sampler_ClippingMask, input.uv).r;
    clip(alphaClipVar- 0.001);
    // uint idx = (uint)_CharShadowmapIndex;
    // float4 output = input.positionCS.z;
    // output *= float4(idx == 0, idx == 1, idx == 2, idx == 3);
    
    return input.positionCS.z;
}
#endif