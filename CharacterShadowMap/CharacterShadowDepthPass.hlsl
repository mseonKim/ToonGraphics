#ifndef CHARACTER_SHADOW_DEPTH_PASS_INCLUDED
#define CHARACTER_SHADOW_DEPTH_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DeclareCharacterShadowTexture.hlsl"

struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    // UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD1;
    // UNITY_VERTEX_INPUT_INSTANCE_ID
    // UNITY_VERTEX_OUTPUT_STEREO
};

Varyings CharShadowVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    // UNITY_SETUP_INSTANCE_ID(input);
    // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    // output.positionCS = TransformObjectToHClip(input.position.xyz);
    output.positionCS = CharShadowObjectToHClip(input.position.xyz);
    output.positionCS.z = 1.0;
    // output.positionWS = mul(UNITY_MATRIX_M, float4(input.position.xyz, 1.0));
    return output;
}

float CharShadowFragment(Varyings input) : SV_DEPTH
{
    // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

    // float4 clipPos = CharShadowWorldToHClip(input.positionWS);
    // clipPos.xyz = clipPos.xyz / clipPos.w;
    // return clipPos.z;
    return input.positionCS.z;
}
#endif
