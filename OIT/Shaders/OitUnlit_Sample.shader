Shader "OrderIndependentTransparency/Unlit_Sample"
{
	Properties{
		_BaseColor("Color", Color) = (1,1,1,1)
		_MainTex("MainTex", 2D) = "white" {}
        _ClippingMask ("ClippingMask", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" }

		Pass
		{
			ZTest LEqual
			ZWrite Off
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma require randomwrite
			// #pragma enable_d3d11_debug_symbols

			//#include "UnityCG.cginc"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    		#include "./LinkedListCreation.hlsl"

			sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _BaseColor;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			[earlydepthstencil]
			float4 frag(v2f i, uint uSampleIdx : SV_SampleIndex) : SV_Target
			{
				// no lighting
				float4 col = tex2D(_MainTex, i.uv) * _BaseColor;

				createFragmentEntry(col, i.vertex.xyz, uSampleIdx);

				return col;
			}
			ENDHLSL
		}

		Pass
		{
			Name "TransparentShadow"
			Tags{"LightMode" = "TransparentShadow"}
	
			ZWrite Off
			ZTest Off
			Cull Off
			Blend One One, One One
			BlendOp Max, Add
	
			HLSLPROGRAM
			#pragma target 2.0
		
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature_local _ALPHATEST_ON
	
			#pragma vertex TransparentShadowVert
			#pragma fragment TransparentShadowFragment
	
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			CBUFFER_START(UnityPerMaterial)
			float4 _BaseColor;
			float4 _MainTex_ST;
			float4 _ClippingMask_ST;
			CBUFFER_END
			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			TEXTURE2D(_ClippingMask);
			#include "Packages/com.unity.toongraphics/CharacterShadowMap/Shaders/TransparentShadowPass.hlsl"
			ENDHLSL
		}

		Pass
        {
            Name "OITDepth"
            Tags {
                "LightMode" = "OITDepth"
            }
            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 position     : POSITION;
            };
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            float frag(Varyings input) : SV_TARGET
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
	}

    FallBack "Unlit/Transparent"
}