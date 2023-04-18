///
/// This Renderer Feature basically renders transparent object's depth to R channel.
/// How to use
/// 1. Add pass in your shader to use "OITDepth" LightMode. (See below example)
/* [Pass Example - Unity Toon Shader]
 * Pass
 *   {
 *       Name "OITDepth"
 *       Tags {
 *           "LightMode" = "OITDepth"
 *       }
 *       ZWrite On
 *       ZTest LEqual
 *       Cull Off
 *       ColorMask R
 *       BlendOp Max
 *
 *       HLSLPROGRAM
 *       #pragma target 2.0
 *       #pragma vertex vert
 *       #pragma fragment frag
 *
 *       #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 *       struct Attributes
 *       {
 *           float4 position     : POSITION;
 *       };
 *       struct Varyings
 *       {
 *           float4 positionCS   : SV_POSITION;
 *       };
 *
 *       Varyings vert(Attributes input)
 *       {
 *           Varyings output = (Varyings)0;
 *           output.positionCS = TransformObjectToHClip(input.position.xyz);
 *           return output;
 *       }
 *
 *       float frag(Varyings input) : SV_TARGET
 *       {
 *           return input.positionCS.z;
 *       }
 *       ENDHLSL
 *   }
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class OITOutlineDepth : ScriptableRendererFeature
    {
        TransparentDepthPass m_Pass;
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;

        /// <inheritdoc/>
        public override void Create()
        {
            m_Pass = new TransparentDepthPass(injectionPoint, RenderQueueRange.transparent);
            m_Pass.ConfigureInput(requirements);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_Pass.Setup("OITDepthRendererFeature", renderingData);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass.Dispose();
        }


        private class TransparentDepthPass : ScriptableRenderPass
        {
            /* Static Variables */
            private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("OITDepth");
            private static int  s_OITDepthTextureId = Shader.PropertyToID("_OITDepthTexture");


            /* Member Variables */
            private RTHandle m_OITDepthRT;
            private ProfilingSampler m_ProfilingSampler;
            private PassData m_PassData;

            FilteringSettings m_FilteringSettings;

            public TransparentDepthPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
            {
                m_PassData = new PassData();
                m_FilteringSettings = new FilteringSettings(renderQueueRange);
                renderPassEvent = evt;
            }

            public void Dispose()
            {
                m_OITDepthRT?.Release();
            }

            public void Setup(string featureName, in RenderingData renderingData)
            {
                m_ProfilingSampler = new ProfilingSampler(featureName);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.RFloat);
                descriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref m_OITDepthRT, descriptor, FilterMode.Bilinear, name:"_OITDepthTexture");
                cmd.SetGlobalTexture(s_OITDepthTextureId, m_OITDepthRT.nameID);

                ConfigureTarget(m_OITDepthRT);
                ConfigureClear(ClearFlag.All, Color.black);
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                m_PassData.filteringSettings = m_FilteringSettings;
                m_PassData.profilingSampler = m_ProfilingSampler;

                ExecutePass(context, m_PassData, ref renderingData);
            }

            private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                var filteringSettings = passData.filteringSettings;

                using (new ProfilingScope(cmd, passData.profilingSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var drawSettings = RenderingUtils.CreateDrawingSettings(k_ShaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private class PassData
            {
                public FilteringSettings filteringSettings;
                public ProfilingSampler profilingSampler;
            }
        }
    }
}