Shader "Unlit/TShadowTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ClippingMask ("ClippingMask", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Trasparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                col.a = _BaseColor.a;
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "TransparentShadow"
            Tags {"LightMode" = "TransparentShadow"}

            ZWrite Off
            ZTest Off
            Cull Off
            Blend One One, One One
            BlendOp Max, Add

            HLSLPROGRAM
            #pragma target 2.0

	    
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma vertex TransparentShadowVert
            #pragma fragment TransparentShadowFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _BaseColor;
            float4 _MainTex_ST;
            float4 _ClippingMask_ST;
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_ClippingMask);
            #include "TransparentShadowPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "TransparentAlphaSum"
            Tags {"LightMode" = "TransparentAlphaSum"}

            ZWrite Off
            ZTest Off
            Cull Off
            Blend One One, One One
            BlendOp Max, Add

            HLSLPROGRAM
            #pragma target 2.0

	    
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma vertex TransparentShadowVert
            #pragma fragment TransparentAlphaSumFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _BaseColor;
            float4 _MainTex_ST;
            float4 _ClippingMask_ST;
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_ClippingMask);
            #include "TransparentShadowPass.hlsl"
            ENDHLSL
        }
    }
}
