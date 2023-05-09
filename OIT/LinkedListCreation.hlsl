#ifndef OIT_LINKED_LIST_INCLUDED
#define OIT_LINKED_LIST_INCLUDED

// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "OitUtils.hlsl"

struct FragmentAndLinkBuffer_STRUCT
{
    uint pixelColor;
    uint uDepthSampleIdx;
    uint next;
};

float Linear01Depth_(float depth)
{
    return 1.0 / (_ZBufferParams.x * depth + _ZBufferParams.y);
}

RWStructuredBuffer<FragmentAndLinkBuffer_STRUCT> FLBuffer;
RWByteAddressBuffer StartOffsetBuffer;

void createFragmentEntry(float4 col, float3 pos, uint uSampleIdx) {
    //Retrieve current Pixel count and increase counter
    uint uPixelCount = FLBuffer.IncrementCounter();

    //calculate bufferAddress
    // uint uStartOffsetAddress = 4 * (_ScaledScreenParams.x * (pos.y - 0.5) + (pos.x - 0.5));
    uint uStartOffsetAddress = 4 * (_ScreenParams.x * (pos.y - 0.5) + (pos.x - 0.5));
    uint uOldStartOffset;
    StartOffsetBuffer.InterlockedExchange(uStartOffsetAddress, uPixelCount, uOldStartOffset);

    //add new Fragment Entry in FragmentAndLinkBuffer
    FragmentAndLinkBuffer_STRUCT Element;
    Element.pixelColor = PackRGBA(col);
    Element.uDepthSampleIdx = PackDepthSampleIdx(Linear01Depth_(pos.z), uSampleIdx);
    Element.next = uOldStartOffset;
    FLBuffer[uPixelCount] = Element;
}

#endif // OIT_LINKED_LIST_INCLUDED