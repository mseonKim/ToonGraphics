Shader "ToonEye"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        _Exposure("Exposure", Range(0, 10)) = 1
        _HiLightTex("Texture", 2D) = "white" {}
        _ShadeStep("ShadeStep", Range(0, 1)) = 0.5
        _ShadeStepOffset("ShadeStepOffset", Range(0, 1)) = 0.01
        _Roughness("Roughness", Range(0, 1)) = 1 // Change only if need to calculate reflection.
        _Metallic("Metallic", Range(0, 1)) = 1 // Change only if need to calculate reflection.
        _MaxAdditionalLightIntensity("MaxAdditionalLightIntensity", Range(0, 10)) = 1
        [Toggle(_)] _Refraction("Refraction", Float) = 1
        _RefractionWeight("RefractionWeight", Range(0, 0.1)) = 0.016
    }
    SubShader
    {
        PackageRequirements
        {
             "com.unity.render-pipelines.universal": "10.5.0"
        }    
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ LIGHTMAP_ON DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _HiLightTex_ST;
            float4 _EyeForward;
            float4 _EyeUp;
            float _Exposure;
            float _RefractionWeight;
            float _Refraction;
            float _ShadeStep;
            float _ShadeStepOffset;
            float _Roughness;
            float _Metallic;
            float _MaxAdditionalLightIntensity;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_linear_mirror);
            TEXTURE2D(_HiLightTex);

            struct Attributes
            {
                float4 positionOS           : POSITION;
                float3 normalOS             : NORMAL;       // Only used for bakedGI
                float2 uv                   : TEXCOORD0;
                float2 staticLightmapUV     : TEXCOORD1;
                float2 dynamicLightmapUV    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS           : SV_POSITION;
                float2 uv                   : TEXCOORD0;
                float3 positionWS           : TEXCOORD1;
                float3 normalWS             : TEXCOORD2;    // Only used for bakedGI
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
            #ifdef DYNAMICLIGHTMAP_ON
                float2  dynamicLightmapUV   : TEXCOORD4; // Dynamic lightmap UVs
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                // UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // UNITY_SETUP_INSTANCE_ID(input);
                // UNITY_TRANSFER_INSTANCE_ID(input, output);
                // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
            #ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
            #endif
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Apply Refraction
                float2 uv = input.uv;
                float3 V = normalize(_WorldSpaceCameraPos.xyz - input.positionWS.xyz);
                float3 F = lerp(float3(0, 0, 1), _EyeForward.xyz, _EyeForward.w > 0);
                float3 vWorld = lerp(float3(0, 1, 0), _EyeUp.xyz, _EyeUp.w > 0);
                float3 uWorld = cross(vWorld, F);
                float3 offset = float3(dot(uWorld, V), dot(vWorld, V), dot(F, V));
            #if UNITY_UV_STARTS_AT_TOP
                offset.y = -offset.y;
            #endif
                uv += lerp(0, offset.xy * _RefractionWeight, _Refraction);

                half4 color = _BaseColor;
                half4 _BaseMap_var = SAMPLE_TEXTURE2D(_BaseMap, sampler_linear_mirror, TRANSFORM_TEX(uv, _BaseMap));
                color *= _BaseMap_var;
                // Base Exposure
                color.rgb *= _Exposure;

                half alpha = 0;
                BRDFData brdfData;
                InitializeBRDFData(_BaseColor, _Metallic, 0, 1 - _Roughness, alpha, brdfData);
                uint meshRenderingLayers = GetMeshRenderingLayer();
                float3 normalWS = F;

                // TODO: High light
                half4 _HiLightTex_var = SAMPLE_TEXTURE2D(_HiLightTex, sampler_linear_mirror, TRANSFORM_TEX(uv, _HiLightTex));

                // Main Light
                Light mainLight = GetMainLight();
                color.rgb *= mainLight.color;   // Don't apply lambert to guarantee whole eye shape.

                // GI
                float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            #if defined(DYNAMICLIGHTMAP_ON)
                float3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, normalWS);
            #else
                float3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
            #endif
                MixRealtimeAndBakedGI(mainLight, normalWS, bakedGI);
                float3 giColor = GlobalIllumination(brdfData, brdfData, 0,
                                                    bakedGI, 0, input.positionWS,
                                                    normalWS, V, normalizedScreenSpaceUV);

            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                half3 additionalLightsColor = 0;
                float M = _ShadeStep + _ShadeStepOffset;
                float m = _ShadeStep - _ShadeStepOffset;

                // Directional Lights
            #if USE_FORWARD_PLUS
                for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

                    Light light = GetAdditionalLight(lightIndex, input.positionWS);

                #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                #endif
                    {
                        half3 halfLambert = dot(light.direction, normalWS) * 0.5 + 0.5;
                        float lightIntensity = 0.299 * light.color.r + 0.587 * light.color.g + 0.114 * light.color.b;
                        light.color *= lerp(rcp(lightIntensity) * _MaxAdditionalLightIntensity, 1, lightIntensity < _MaxAdditionalLightIntensity);
                        half3 c = light.color * light.distanceAttenuation;
                        additionalLightsColor += _BaseMap_var * lerp(0, c, saturate((halfLambert - m) / (M - m))) * _Exposure;
                    }
                }
            #endif

                // Local Lights
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = normalizedScreenSpaceUV;
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS);

                #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                #endif
                    {
                        half3 halfLambert = dot(light.direction, normalWS) * 0.5 + 0.5;
                        float lightIntensity = 0.299 * light.color.r + 0.587 * light.color.g + 0.114 * light.color.b;
                        light.color *= lerp(rcp(lightIntensity) * _MaxAdditionalLightIntensity, 1, lightIntensity < _MaxAdditionalLightIntensity);
                        half3 c = light.color * light.distanceAttenuation;
                        additionalLightsColor += _BaseMap_var * lerp(0, c, saturate((halfLambert - m) / (M - m))) * _Exposure;
                    }
                LIGHT_LOOP_END

                color.rgb += additionalLightsColor;
            #endif

                color.rgb += giColor;

                return color;
            }

            ENDHLSL
        }
    }
}
