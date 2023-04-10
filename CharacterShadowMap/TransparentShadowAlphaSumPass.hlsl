#ifndef TRANSPARENT_SHADOW_ALPHA_SUM_PASS_INCLUDED
#define TRANSPARENT_SHADOW_ALPHA_SUM_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DeclareCharacterShadowTexture.hlsl"
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

float4 _BaseColor;
float4 _MainTex_ST;
float4 _ClippingMask_ST;
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_ClippingMask);

v2f TransparentShadowVert (appdata v)
{
    v2f o;
    o.vertex = CharShadowObjectToHClipWithoutBias(v.vertex.xyz);
    o.uv = v.uv;
    o.positionWS = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0));
    return o;
}

float4 TransparentShadowFragment (v2f i) : SV_Target
{
    float4 clipPos = CharShadowWorldToHClip(i.positionWS);
    clipPos.z = 1.0;
    float3 ndc = clipPos.xyz / clipPos.w;
    float2 ssUV = ndc.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    ssUV.y = 1.0 - ssUV.y;
#endif

    // Use G Channel for alpha sum
    float4 color = 0;
    color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).a * _BaseColor.a;
    float alphaClipVar = SAMPLE_TEXTURE2D(_ClippingMask, sampler_MainTex, TRANSFORM_TEX(i.uv, _ClippingMask)).r;
    color.g *= alphaClipVar;
    clip(color.g - 0.001);
    // Discard behind fragment
    color.g = lerp(color.g, 0, SampleCharacterShadowmap(ssUV, ndc.z));
    return color;
    // return _BaseColor.a;
}
#endif
