Shader "Sky/ProceduralNightSky"
{
    Properties
    {
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _SkyViewTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
    	Cull Off ZWrite Off

        Pass
        {
            Name "NightSkyPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_SkyViewTex);
            SAMPLER(samplerLinearClamp);
            float4 _SkyViewTex_ST;
            half   _Exposure;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            float raySphereIntersectNearest(float3 r0, float3 rd, float3 s0, float sR)
            {
                float a = dot(rd, rd);
                float3 s0_r0 = r0 - s0;
                float b = 2.0 * dot(rd, s0_r0);
                float c = dot(s0_r0, s0_r0) - (sR * sR);
                float delta = b * b - 4.0*a*c;
                if (delta < 0.0 || a == 0.0)
                {
                    return -1.0;
                }
                float sol0 = (-b - sqrt(delta)) / (2.0*a);
                float sol1 = (-b + sqrt(delta)) / (2.0*a);
                if (sol0 < 0.0 && sol1 < 0.0)
                {
                    return -1.0;
                }
                if (sol0 < 0.0)
                {
                    return max(0.0, sol1);
                }
                else if (sol1 < 0.0)
                {
                    return max(0.0, sol0);
                }
                return max(0.0, min(sol0, sol1));
            }

            float fromUnitToSubUvs(float u, float resolution)
            {
                return (u + 0.5f / resolution) * (resolution / (resolution + 1.0f));
            }

            void SkyViewLutParamsToUv(bool IntersectGround, in float viewZenithCosAngle, in float lightViewCosAngle, in float viewHeight, out float2 uv)
            {
                float Vhorizon = sqrt(viewHeight * viewHeight - 6360 * 6360);
                float CosBeta = Vhorizon / viewHeight;				// GroundToHorizonCos
                float Beta = acos(CosBeta);
                float ZenithHorizonAngle = PI - Beta;

                if (!IntersectGround)
                {
                    float coord = acos(viewZenithCosAngle) / ZenithHorizonAngle;
                    coord = 1.0 - coord;
            #if NONLINEARSKYVIEWLUT
                    coord = sqrt(coord);
            #endif
                    coord = 1.0 - coord;
                    uv.y = coord * 0.5f;
                }
                else
                {
                    float coord = (acos(viewZenithCosAngle) - ZenithHorizonAngle) / Beta;
                    uv.y = coord * 0.5f + 0.5f;
                }

                {
                    float coord = -lightViewCosAngle * 0.5f + 0.5f;
                    coord = sqrt(coord);
                    uv.x = coord;
                }

                // Constrain uvs to valid sub texel range (avoid zenith derivative issue making LUT usage visible)
                uv = float2(fromUnitToSubUvs(uv.x, 192.0f), fromUnitToSubUvs(uv.y, 108.0f));
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = half4(0, 0, 0, 1);

                float2 pixPos = i.vertex.xy;
				float2 uv = pixPos * _ScreenSize.zw;
				float3 ClipSpace = float3(uv * float2(2.0, -2.0) - float2(1.0, -1.0), 1.0);
				float4 HViewPos = mul(unity_MatrixInvP, float4(ClipSpace, 1.0));
				float3 WorldDir = normalize(mul((float3x3)unity_MatrixInvV, HViewPos.xyz / HViewPos.w));
				float3 CameraPos = float3(0, 1, -1);
				float3 WorldPos = CameraPos + float3(0, 6360, 0);

				float deviceDepth = -1.0;

				float viewHeight = length(WorldPos);
				float3 L = 0;
				deviceDepth = 1.0 - SampleSceneDepth(uv);
                bool IntersectGround = raySphereIntersectNearest(WorldPos, WorldDir, float3(0, 0, 0), 6360) >= 0.0f;

                if (deviceDepth == 1.0f)
				{
					float2 uv;
					float3 UpVector = normalize(WorldPos);
					float viewZenithCosAngle = dot(WorldDir, UpVector);

					float3 sideVector = normalize(cross(UpVector, WorldDir));		// assumes non parallel vectors
					float3 forwardVector = normalize(cross(sideVector, UpVector));	// aligns toward the sun light but perpendicular to up vector
					float2 lightOnPlane = float2(dot(_MainLightPosition.xyz, forwardVector), dot(_MainLightPosition.xyz, sideVector));
					lightOnPlane = normalize(lightOnPlane);
					float lightViewCosAngle = lightOnPlane.x;

					SkyViewLutParamsToUv(IntersectGround, viewZenithCosAngle, lightViewCosAngle, viewHeight, uv);
					
                    float ow = rcp(192.0);
                    float oh = rcp(108.0);
                    float3 skyviewVal = _SkyViewTex.SampleLevel(samplerLinearClamp, uv, 0).rgb
                                        + _SkyViewTex.SampleLevel(samplerLinearClamp, uv + float2(-ow, oh), 0).rgb
                                        + _SkyViewTex.SampleLevel(samplerLinearClamp, uv + float2(ow, oh), 0).rgb
                                        + _SkyViewTex.SampleLevel(samplerLinearClamp, uv + float2(-ow, -oh), 0).rgb
                                        + _SkyViewTex.SampleLevel(samplerLinearClamp, uv + float2(-ow, -oh), 0).rgb;
                    col = float4(skyviewVal/5.0, 1.0);
					
					// Exposure
					col.rgb *= _Exposure;
				}

                return col;
            }
            ENDHLSL
        }
    }
}
