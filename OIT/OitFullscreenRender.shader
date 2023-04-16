Shader "OrderIndependentTransparency/OitFullscreenRender"
{
	Properties
	{
	}
	SubShader
	{
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			// Blend One One
			Blend One OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment OITFrag
			#pragma target 5.0
			#pragma require randomwrite
			// #pragma enable_d3d11_debug_symbols

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			#include "LinkedListRendering.hlsl"

			// TEXTURE2D_X(_CameraOpaqueTexture);
            // SAMPLER(sampler_CameraOpaqueTexture);

			struct appdata {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};
			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			//Pixel function returns a solid color for each point.
			half4 OITFrag(Varyings i, uint uSampleIndex : SV_SampleIndex) : SV_Target
			{
				// Retrieve current color from background texture
				float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord);
				return renderLinkedList(color, i.positionCS.xy, uSampleIndex);
			}
			ENDHLSL
		}
	}
}