using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;
using System.Collections.Generic;

namespace ToonGraphics
{
    public class CharacterShadowMap : ScriptableRendererFeature
    {
        private CharacterShadowPass m_CharShadowPass;
        private TransparentShadowPass m_CharTransparentShadowPass;
        public CharacterShadowConfig config;
        public UniversalRendererData urpData;


        public override void Create()
        {
            m_CharShadowPass = new CharacterShadowPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque);
            m_CharShadowPass.ConfigureInput(ScriptableRenderPassInput.None);
            m_CharTransparentShadowPass = new TransparentShadowPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.transparent);
            m_CharTransparentShadowPass.ConfigureInput(ScriptableRenderPassInput.None);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (config == null)
                return;

            // Additional shadow is only available in forward+
            var additionalShadowEnabled = urpData != null ? config.enableAdditionalShadow && urpData.renderingMode == RenderingMode.ForwardPlus : false;
            m_CharShadowPass.Setup("CharacterShadowMapRendererFeature", renderingData, config);
            renderer.EnqueuePass(m_CharShadowPass);
            if (config.enableTransparentShadow)
            {
                m_CharTransparentShadowPass.Setup("TransparentShadowMapRendererFeature", renderingData, config);
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