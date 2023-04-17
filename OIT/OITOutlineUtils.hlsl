#ifndef OIT_OUTLINE_UTILS_INCLUDED
#define OIT_OUTLINE_UTILS_INCLUDED

TEXTURE2D(_OITDepthTexture);

half SampleOITDepth(float2 uv, float z, SamplerState s)
{
    return SAMPLE_TEXTURE2D(_OITDepthTexture, s, uv).r > z;
}

half SampleOITDepthFiltered(float2 uv, float z, SamplerState s)
{
    real fetchesWeights[9];
    real2 fetchesUV[9];
    SampleShadow_ComputeSamples_Tent_5x5(_ScreenParams, uv, fetchesWeights, fetchesUV);

    float attenuation = fetchesWeights[0] * SampleOITDepth(fetchesUV[0].xy, z, s)
                + fetchesWeights[1] * SampleOITDepth(fetchesUV[1].xy, z, s)
                + fetchesWeights[2] * SampleOITDepth(fetchesUV[2].xy, z, s)
                + fetchesWeights[3] * SampleOITDepth(fetchesUV[3].xy, z, s)
                + fetchesWeights[4] * SampleOITDepth(fetchesUV[4].xy, z, s)
                + fetchesWeights[5] * SampleOITDepth(fetchesUV[5].xy, z, s)
                + fetchesWeights[6] * SampleOITDepth(fetchesUV[6].xy, z, s)
                + fetchesWeights[7] * SampleOITDepth(fetchesUV[7].xy, z, s)
                + fetchesWeights[8] * SampleOITDepth(fetchesUV[8].xy, z, s);

    return step(attenuation, 0.999);
}

#endif // OIT_OUTLINE_UTILS_INCLUDED