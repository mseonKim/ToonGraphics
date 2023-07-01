#ifndef TRANSPARENT_SHADOW_PASS_INCLUDED
#define TRANSPARENT_SHADOW_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "CharacterShadowInput.hlsl"
#include "CharacterShadowTransforms.hlsl"
#include "DeclareCharacterShadowTexture.hlsl"

// Below material properties must be declared in seperate shader input to make compatible with SRP Batcher.
// CBUFFER_START(UnityPerMaterial)
// float4 _BaseColor;
// float4 _MainTex_ST;
// float4 _ClippingMask_ST;
// CBUFFER_END
// TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
// TEXTURE2D(_ClippingMask);

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD;
    float3 positionWS : TEXCOORD1;
};

v2f TransparentShadowVert (appdata v)
{
    v2f o;
    o.vertex = CharShadowObjectToHClipWithoutBias(v.vertex.xyz, (uint)_CharShadowmapIndex);
    o.vertex.z = 1.0;
    o.uv = v.uv;
    o.positionWS = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0));
    return o;
}

float4 TransparentShadowFragment (v2f i) : SV_Target
{
    float4 clipPos = CharShadowWorldToHClip(i.positionWS, (uint)_CharShadowmapIndex);
    clipPos.z = 1.0;
    float3 ndc = clipPos.xyz / clipPos.w;
    float2 ssUV = ndc.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    ssUV.y = 1.0 - ssUV.y;
#endif

    // Use A Channel for alpha sum
    float4 color = 0;
    color.a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).a * _BaseColor.a;
    float alphaClipVar = SAMPLE_TEXTURE2D(_ClippingMask, sampler_MainTex, TRANSFORM_TEX(i.uv, _ClippingMask)).r;
    color.a *= alphaClipVar;
    clip(color.a - 0.001);
    // Discard behind fragment
    color.a = lerp(color.a, 0, SampleCharacterShadowmap(ssUV, ndc.z, (uint)_CharShadowmapIndex));
    color.r = i.vertex.z;   // Depth
    return color;
    // return _BaseColor.a;
}
#endif
