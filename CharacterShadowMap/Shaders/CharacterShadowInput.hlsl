#ifndef CHARACTER_SHADOW_INPUT_INCLUDED
#define CHARACTER_SHADOW_INPUT_INCLUDED

CBUFFER_START(CharShadow)
    float4 _CharShadowBias;                 // x: main depth , y: main normal , z: local depth , w: local normal
    float4x4 _CharShadowViewM[4];
    float4x4 _CharShadowProjM;
    float4 _CharShadowOffset0;
    float4 _CharShadowOffset1;
    float4 _CharShadowmapSize;
    float4 _CharTransparentShadowmapSize;
    float4 _CharShadowStepOffset;           // x: main , y: local
    float4 _CharShadowLightDirections[3];   // Additional Lights (= MainLight not included)
    float _CharShadowmapIndex;
    float _CharShadowLocalLightIndices[3];
    // float _LocalLightToCharShadowIdxTable[3];
    // float _charshadowpad00_;
CBUFFER_END

TEXTURE2D_ARRAY(_CharShadowAtlas);
SAMPLER(sampler_CharShadowAtlas);
TEXTURE2D_ARRAY(_TransparentShadowAtlas);

#endif