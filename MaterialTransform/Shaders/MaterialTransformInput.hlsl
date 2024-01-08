#ifndef MATERIAL_TRANSFORMER_INPUT_INCLUDED
#define MATERIAL_TRANSFORMER_INPUT_INCLUDED

float _InvTransformerDissolveWidth;
float _TransformerMasks[8];
float4 _TransformerColor;
TEXTURE2D(_TransformerDissolveTex);

// Below material properties must be declared in seperate shader input to make compatible with SRP Batcher.
// CBUFFER_START(MaterialTransformer)
//    float4 _TransformerMaskPivot;
//    float4 _MeshTransformScale; // w unused
//    uint _TransformerMaskChannel;
// CBUFFER_END

#define MATERIAL_TRANSFORMER_CHECK(positionOS)  \
    float transformVal = 0; \
    float mask = -1; \
    if (_UseTransformerMask > 0) \
    { \
        float3 meshCoordOS = positionOS / _MeshTransformScale.xyz + _MeshTransformOffset.xyz; \
        transformVal = abs(dot(meshCoordOS, _TransformerMaskPivot.xyz) - _TransformerMaskPivot.w); \
        mask = _TransformerMasks[_TransformerMaskChannel]; \
    }

inline void MaterialTransformerFragDiscard(float3 positionOS)
{
    MATERIAL_TRANSFORMER_CHECK(positionOS)
    clip(transformVal - mask); // if (mask >= transformVal)
}

inline void MaterialTransformerFragDiscard(float mask, float transformVal)
{
    clip(transformVal - mask); // if (mask >= transformVal)
}

// Unused
inline void MaterialTransformerDissolveClip(float2 uv, float mask, float transformVal, sampler sampler_TransformerDissolveTex)
{
    float dissolveMask = saturate(abs(mask - transformVal) / (1.0 - transformVal)) * 10;
    float dissolveVal = SAMPLE_TEXTURE2D(_TransformerDissolveTex, sampler_TransformerDissolveTex, uv).r;
    clip(dissolveMask - dissolveVal);
}

float4 MaterialTransformDissolve(float mask, float transformVal, inout float lerpVal, float2 uv, sampler sampler_TransformerDissolveTex)
{
    if (_UseTransformerMask > 0)
    {
        lerpVal = 1.0 - saturate(abs(transformVal - mask) * _InvTransformerDissolveWidth);
        float dissolve = 1;
        float dissolveMask = saturate(abs(mask - transformVal) / (1.0 - transformVal)) * 10;
        float dissolveVal = SAMPLE_TEXTURE2D(_TransformerDissolveTex, sampler_TransformerDissolveTex, uv).r;
        if (lerpVal > 0)
        {
            dissolve = (dissolveVal - dissolveMask);
        }
        return float4(_TransformerColor.rgb * lerpVal * dissolve, dissolveMask - dissolveVal);
    }
    return 0;
}

// Unused
inline void MaterialTransformerDiscardByCoord(float3 meshCoord, inout float4 positionCS)
{
    if (_UseTransformerMask > 0)
    {
        float transformVal = abs(dot(meshCoord, _TransformerMaskPivot.xyz) - _TransformerMaskPivot.w);
        float mask = _TransformerMasks[_TransformerMaskChannel];
        if (mask >= transformVal)
        {
            positionCS /= 0;    // Make NaN so it can be ignored in pixel shader
        }
    }
}

#endif