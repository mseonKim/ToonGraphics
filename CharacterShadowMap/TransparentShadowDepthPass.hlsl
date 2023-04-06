#ifndef TRANSPARENT_SHADOW_DEPTH_PASS_INCLUDED
#define TRANSPARENT_SHADOW_DEPTH_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DeclareCharacterShadowTexture.hlsl"
struct appdata
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float3 positionWS : TEXCOORD0;
};

float4 _BaseColor;
v2f TransparentShadowVert (appdata v)
{
    v2f o;
    o.vertex = CharShadowObjectToHClipWithoutBias(v.vertex.xyz);
    o.positionWS = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0));
    return o;
}

half TransparentShadowFragment (v2f i) : SV_Target
{
    float4 clipPos = CharShadowWorldToHClip(i.positionWS);
    clipPos.z = 1.0;
    float3 ndc = clipPos.xyz / clipPos.w;
    float2 ssUV = ndc.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    ssUV.y = 1.0 - ssUV.y;
#endif
    // Discard behind fragment
    return lerp(_BaseColor.a, 0, SampleCharacterShadowmap(ssUV, ndc.z));
    // return _BaseColor.a;
}
#endif
