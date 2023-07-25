Shader "ToonEye"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        _HiLightTex("Texture", 2D) = "white" {}
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
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
            float _RefractionWeight;
            float _Refraction;
            float __pad00__;
            float __pad01__;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HiLightTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                // UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 positionCS : SV_POSITION;
                // UNITY_VERTEX_INPUT_INSTANCE_ID
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

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                // UNITY_SETUP_INSTANCE_ID(input);
                // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Refraction
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
                half4 _BaseMap_var = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uv, _BaseMap));
                color *= _BaseMap_var;

                half4 _HiLightTex_var = SAMPLE_TEXTURE2D(_HiLightTex, sampler_BaseMap, TRANSFORM_TEX(uv, _HiLightTex));
                Light mainLight = GetMainLight();

                // Determines Darkness based on main light color
                bool darkness = !any(mainLight.color.rgb);

                return lerp(color, 0, darkness);
            }

            ENDHLSL
        }
    }
}
