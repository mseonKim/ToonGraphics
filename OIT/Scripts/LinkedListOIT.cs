/* Reference: https://github.com/happy-turtle/oit-unity */
/// NOTE)
/// This feature should be used with CharacterShadowMap & TransparentShadoMap Renderer Features together.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class LinkedListOIT : ScriptableRendererFeature
    {
        public Material fullscreenMaterial;
        public ComputeShader oitComputeUtilsCS;
        public ComputeShader combineOITDepthCS;
        public bool useTransparentOutline;
        private OitPass m_OitPass;
        private TransparentDepthPass m_TDepthPass;

        public override void Create()
        {
            m_OitPass?.Cleanup();
            m_OitPass = new OitPass(fullscreenMaterial, oitComputeUtilsCS, useTransparentOutline);
            m_TDepthPass = new TransparentDepthPass(RenderPassEvent.AfterRenderingOpaques, RenderQueueRange.transparent, combineOITDepthCS);
            m_TDepthPass.ConfigureInput(ScriptableRenderPassInput.None);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Calling ConfigureInput with the ScriptableRenderPassInput.Color argument ensures that the opaque texture is available to the Render Pass
            m_OitPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_OitPass.Setup("Order Independent Transparency");
            renderer.EnqueuePass(m_OitPass);
            m_TDepthPass.Setup("OITDepthRendererFeature");
            renderer.EnqueuePass(m_TDepthPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_OitPass.Cleanup();
            m_TDepthPass.Dispose();
        }
    }
}