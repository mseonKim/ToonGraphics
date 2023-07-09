#ifndef CHARACTER_SHADOW_INPUT_INCLUDED
#define CHARACTER_SHADOW_INPUT_INCLUDED

CBUFFER_START(CharShadow)
    float4 _CharShadowBias;                 // x: main depth , y: main normal , z: local depth , w: local normal
    float4x4 _CharShadowViewM[4];
    float4x4 _CharShadowProjM;
    float4 _CharShadowOffset0;
    float4 _CharShadowOffset1;
    float4 _CharShadowmapSize;              // rcp(width), rcp(height), width, height
    float4 _CharTransparentShadowmapSize;   // rcp(width), rcp(height), width, height
    float4 _CharShadowStepOffset;           // x: main , y: local
    float4 _CharShadowLightDirections[3];   // Additional Lights (= MainLight not included)
    float _CharShadowmapIndex;
    float _CharShadowLocalLightIndices[3];
    float4 _CharShadowCascadeParams;        // x: cascadeMaxDistance, y: cascadeResolutionScale
    // float _LocalLightToCharShadowIdxTable[3];
CBUFFER_END

TEXTURE2D_ARRAY(_CharShadowMap);
SAMPLER(sampler_CharShadowMap);
TEXTURE2D_ARRAY(_TransparentShadowMap);
TEXTURE2D_ARRAY(_TransparentAlphaSum);

#endif