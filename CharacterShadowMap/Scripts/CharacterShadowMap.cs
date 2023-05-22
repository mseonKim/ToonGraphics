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
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
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
        public CustomShadowMapSize atlasSize = CustomShadowMapSize._4096;
        public CustomShadowMapSize transparentAtlasSize = CustomShadowMapSize._4096;
        public CustomShadowMapPrecision atlasPrecision = CustomShadowMapPrecision.RHalf;
        public UniversalRendererData urpData;
        public bool enableAdditionalShadow = false;

        /// <inheritdoc/>
        public override void Create()
        {
            m_CharShadowPass = new CharacterShadowPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque);
            m_CharShadowPass.ConfigureInput(requirements);
            m_CharTransparentShadowPass = new TransparentShadowPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.transparent);
            m_CharTransparentShadowPass.ConfigureInput(requirements);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Additional shadow is only available in forward+
            var additionalShadowEnabled = urpData != null ? enableAdditionalShadow && urpData.renderingMode == RenderingMode.ForwardPlus : false;
            m_CharShadowPass.Setup(   "CharacterShadowMapRendererFeature", renderingData,
                            new Vector4(bias, normalBias, additionalBias, additionalNormalBias),
                            new Vector2(stepOffset, additionalStepOffset),
                            (int)atlasSize, (int)atlasPrecision, additionalShadowEnabled);
            renderer.EnqueuePass(m_CharShadowPass);
            m_CharTransparentShadowPass.Setup("TransparentShadowMapRendererFeature", (int)transparentAtlasSize, enableAdditionalShadow);
            renderer.EnqueuePass(m_CharTransparentShadowPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_CharShadowPass.Dispose();
            m_CharTransparentShadowPass.Dispose();
        }

    }
}