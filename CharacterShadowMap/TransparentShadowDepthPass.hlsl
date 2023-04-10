#ifndef TRANSPARENT_SHADOW_DEPTH_PASS_INCLUDED
#define TRANSPARENT_SHADOW_DEPTH_PASS_INCLUDED

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
    float2 uv : TEXCOORD0;
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
    o.vertex.z = 1.0;
    o.uv = v.uv;
    return o;
}

float4 TransparentShadowFragment (v2f i) : SV_Target
{
    float4 color = 0;
    color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).a * _BaseColor.a;
    color.g *= SAMPLE_TEXTURE2D(_ClippingMask, sampler_MainTex, TRANSFORM_TEX(i.uv, _ClippingMask)).r;
    clip(color.g - 0.001);
    color.g = 0;    // Reset g
    color.r = i.vertex.z;
    return color;
}
#endif
