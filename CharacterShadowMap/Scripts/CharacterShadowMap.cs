/// How to use
/// 1. Add 'CharacterShadowCamera' prefab to your scene.
/// 2. Add pass in your shader to use 'CharacterShadowDepthPass.hlsl' with "CharacterDepth" LightMode. (See below example)
/* [Pass Example - Unity Toon Shader]
 * NOTE) We assume that the shader use "_ClippingMask" property.
 * Pass
 *   {
 *       Name "CharacterDepth"
 *       Tags{"LightMode" = "CharacterDepth"}
 *
 *       ZWrite On
 *       ZTest LEqual
 *       Cull Off
 *
 *       HLSLPROGRAM
 *       #pragma target 2.0
 *   
 *       // Required to compile gles 2.0 with standard srp library
 *       #pragma prefer_hlslcc gles
 *       #pragma exclude_renderers d3d11_9x
 *       #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
 *
 *       #pragma vertex CharShadowVertex
 *       #pragma fragment CharShadowFragment
 *
 *       #include "Packages/com.unity.toongraphics/CharacterShadowMap/Shaders/CharacterShadowDepthPass.hlsl"
 *       ENDHLSL
 *   }
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;
using System.Collections.Generic;

namespace ToonGraphics
{
    public enum CustomShadowMapSize
    {
        X1 = 1,
        X2 = 2,
        X4 = 4
    }

    public enum CustomShadowMapPrecision
    {
        RFloat = 14,
        RHalf = 15,
    }

    public class CharacterShadowMap : ScriptableRendererFeature
    {
        private CharacterShadowPass m_CharShadowPass;
        private TransparentShadowPass m_CharTransparentShadowPass;
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;
        public float bias;
        public float normalBias;
        public float additionalBias;
        public float additionalNormalBias;
        public float stepOffset = 0.999f;
        public float additionalStepOffset = 0.999f;
        public CustomShadowMapSize textureScale = CustomShadowMapSize.X2;
        public CustomShadowMapSize transparentTextureScale = CustomShadowMapSize.X2;
        public CustomShadowMapPrecision precision = CustomShadowMapPrecision.RHalf;
        public UniversalRendererData urpData;
        public bool enableTransparentShadow = false;
        public bool enableAdditionalShadow = false;

        public override void Create()
        {
            m_CharShadowPass = new CharacterShadowPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque);
            m_CharShadowPass.ConfigureInput(requirements);
            m_CharTransparentShadowPass = new TransparentShadowPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.transparent);
            m_CharTransparentShadowPass.ConfigureInput(requirements);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Additional shadow is only available in forward+
            var additionalShadowEnabled = urpData != null ? enableAdditionalShadow && urpData.renderingMode == RenderingMode.ForwardPlus : false;
            m_CharShadowPass.Setup("CharacterShadowMapRendererFeature", renderingData,
                                    new Vector4(bias, normalBias, additionalBias, additionalNormalBias),
                                    new Vector2(stepOffset, additionalStepOffset),
                                    (int)textureScale, (int)precision, additionalShadowEnabled);
            renderer.EnqueuePass(m_CharShadowPass);
            if (enableTransparentShadow)
            {
                m_CharTransparentShadowPass.Setup("TransparentShadowMapRendererFeature", renderingData,
                                                  (int)transparentTextureScale, (int)precision, enableAdditionalShadow);
                renderer.EnqueuePass(m_CharTransparentShadowPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_CharShadowPass.Dispose();
            m_CharTransparentShadowPass.Dispose();
        }

    }
}