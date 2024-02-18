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
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class TransparentDepthPass : ScriptableRenderPass
    {
        /* Static Variables */
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("OITDepth");
        private static int  s_OITDepthTextureId = Shader.PropertyToID("_OITDepthTexture");
        private static int  s_CombinedOITDepthTextureId = Shader.PropertyToID("_CombinedOITDepthTexture");


        /* Member Variables */
        private RTHandle m_OITDepthRT;
        private RTHandle m_CombinedOITDepthRT;
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;

        private FilteringSettings m_FilteringSettings;

        public TransparentDepthPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, ComputeShader combineOITDepthCS)
        {
            m_PassData = new PassData();
            m_FilteringSettings = new FilteringSettings(renderQueueRange);
            renderPassEvent = evt;
            m_PassData.combineOITDepthCS = combineOITDepthCS;
        }

        public void Dispose()
        {
            m_OITDepthRT?.Release();
        }

        public void Setup(string featureName)
        {
            m_ProfilingSampler = new ProfilingSampler(featureName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var descriptor = new RenderTextureDescriptor(cameraDescriptor.width, cameraDescriptor.height, RenderTextureFormat.RFloat, 0);
            descriptor.sRGB = false;
            descriptor.autoGenerateMips = false;
            RenderingUtils.ReAllocateIfNeeded(ref m_OITDepthRT, descriptor, FilterMode.Point, name:"_OITDepthTexture");
            cmd.SetGlobalTexture(s_OITDepthTextureId, m_OITDepthRT.nameID);

            descriptor = new RenderTextureDescriptor(cameraDescriptor.width, cameraDescriptor.height, RenderTextureFormat.RFloat, 0);
            descriptor.sRGB = false;
            descriptor.enableRandomWrite = true;
            descriptor.autoGenerateMips = false;
            RenderingUtils.ReAllocateIfNeeded(ref m_CombinedOITDepthRT, descriptor, FilterMode.Point, name:"_CombinedOITDepthTexture");
            cmd.SetGlobalTexture(s_CombinedOITDepthTextureId, m_CombinedOITDepthRT.nameID);

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

                if (passData.combineOITDepthCS != null)
                {
                    var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                    cmd.DispatchCompute(passData.combineOITDepthCS, 0, (cameraDescriptor.width + 7) / 8, (cameraDescriptor.height + 7) / 8, 1);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private class PassData
        {
            public FilteringSettings filteringSettings;
            public ComputeShader combineOITDepthCS;
            public ProfilingSampler profilingSampler;
        }
    }
}