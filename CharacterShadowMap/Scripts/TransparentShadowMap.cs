/// NOTE)
/// This feature should be used only for character's cloth.
/// Otherwise, the shadow will cast to far object as well.
/// Limitations: Not will behave natural if more than 2 transparent clothes overlapped.

/// How to use
/// 0. Add "CharacterShadowMap RendererFeature first. (Required)
///    This should be used with above RendererFeature. Otherwise, it will not be running.
/// 1. Add pass in your shader to use 'TransparentShadowPass.hlsl'
///    with "TransparentShadow" LightMode. (See below example)
/* [Pass Example - Unity Toon Shader]
 * NOTE) We assume that the shader use "_MainTex" and "_BaseColor", "_ClippingMask" properties.
 *   Pass
 *   {
 *       Name "TransparentShadow"
 *       Tags{"LightMode" = "TransparentShadow"}
 *
 *       ZWrite Off
 *       ZTest Off
 *       Cull Off
 *       Blend One One, One One
 *       BlendOp Max, Add
 *
 *       HLSLPROGRAM
 *       #pragma target 2.0
 *   
 *       #pragma prefer_hlslcc gles
 *       #pragma exclude_renderers d3d11_9x
 *       #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
 *       #pragma shader_feature_local _ALPHATEST_ON
 *
 *       #pragma vertex TransparentShadowVert
 *       #pragma fragment TransparentShadowFragment
 *
 *       #include "Packages/com.unity.toongraphics/CharacterShadowMap/Shaders/TransparentShadowPass.hlsl"
 *       ENDHLSL
 *   }
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class TransparentShadowMap : ScriptableRendererFeature
    {
        TransparentShadowPass m_Pass;
        
        // Note) the RenderPassEvent is set as BeforeRenderingOpaques.
        // It means this RendererFeature should be executed after 'CharacterShadowMap' Feature which is set as BeforeRenderingPrePasses.
        public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingOpaques;
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;
        public CustomShadowMapSize atlasSize = CustomShadowMapSize._4096;
        public bool enableAdditionalShadow;

        /// <inheritdoc/>
        public override void Create()
        {
            m_Pass = new TransparentShadowPass(injectionPoint, RenderQueueRange.transparent);
            m_Pass.ConfigureInput(requirements);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_Pass.Setup("TransparentShadowMapRendererFeature", (int)atlasSize, enableAdditionalShadow);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass.Dispose();
        }


        private class TransparentShadowPass : ScriptableRenderPass
        {
            /* Static Variables */
            private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("TransparentShadow");
            private static int  s_TransparentShadowAtlasId = Shader.PropertyToID("_TransparentShadowAtlas");
            private static int s_ShadowMapSize = Shader.PropertyToID("_CharTransparentShadowmapSize");
            private static int s_ShadowMapIndex = Shader.PropertyToID("_CharShadowmapIndex");


            /* Member Variables */
            private RTHandle m_TransparentShadowRT; // R: Depth, A : Alpha Sum
            private ProfilingSampler m_ProfilingSampler;
            private PassData m_PassData;
            private FilteringSettings m_FilteringSettings;

            public TransparentShadowPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
            {
                m_PassData = new PassData();
                m_FilteringSettings = new FilteringSettings(renderQueueRange);
                renderPassEvent = evt;
            }

            public void Dispose()
            {
                m_TransparentShadowRT?.Release();
            }

            public void Setup(string featureName, int size, bool enableAdditionalShadow)
            {
                m_ProfilingSampler = new ProfilingSampler(featureName);
                m_PassData.atlasSize = size;
                m_PassData.enableAdditionalShadow = enableAdditionalShadow;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // R: Depth, A : Alpha Sum
                var descriptor = new RenderTextureDescriptor(m_PassData.atlasSize, m_PassData.atlasSize, RenderTextureFormat.ARGBHalf, 0);
                descriptor.dimension = TextureDimension.Tex2DArray;
                descriptor.sRGB = false;
                descriptor.volumeDepth = m_PassData.enableAdditionalShadow ? 4 : 1;
                RenderingUtils.ReAllocateIfNeeded(ref m_TransparentShadowRT, descriptor, FilterMode.Bilinear, name:"_TransparentShadowAtlas");
                cmd.SetGlobalTexture(s_TransparentShadowAtlasId, m_TransparentShadowRT.nameID);
                m_PassData.target = m_TransparentShadowRT;
                // ConfigureTarget(m_TransparentShadowRT);
                // ConfigureClear(ClearFlag.All, Color.clear);
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                m_PassData.shaderTagId = k_ShaderTagId;
                m_PassData.filteringSettings = m_FilteringSettings;
                m_PassData.profilingSampler = m_ProfilingSampler;

                ExecuteTransparentShadowPass(context, m_PassData, ref renderingData);
            }

            private static void ExecuteTransparentShadowPass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                var filteringSettings = passData.filteringSettings;

                float invShadowAtlasWidth = 1.0f / passData.atlasSize;
                float invShadowAtlasHeight = 1.0f / passData.atlasSize;
                float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
                float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

                using (new ProfilingScope(cmd, passData.profilingSampler))
                {
                    cmd.SetGlobalVector(s_ShadowMapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight, passData.atlasSize, passData.atlasSize));
                    cmd.SetGlobalFloat(s_ShadowMapIndex, 0);
                    CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, 0);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Depth & Alpha Sum
                    var drawSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

                    if (passData.enableAdditionalShadow)
                    {
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                        for (int i = 0; i < 3; i++)
                        {
                            CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, i + 1);
                            cmd.SetGlobalFloat(s_ShadowMapIndex, i + 1);
                            context.ExecuteCommandBuffer(cmd);
                            cmd.Clear();
                            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private class PassData
            {
                public ShaderTagId shaderTagId;
                public FilteringSettings filteringSettings;
                public ProfilingSampler profilingSampler;
                public RTHandle target;
                public int atlasSize;
                public bool enableAdditionalShadow;
            }
        }
    }
}