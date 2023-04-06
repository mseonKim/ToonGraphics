using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// TODO: How to use
public class TransparentShadowMap : ScriptableRendererFeature
{
    TransparentShadowPass m_Pass;
    public static Camera lightCamera;
    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingOpaques;
    public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;

    /// <inheritdoc/>
    public override void Create()
    {
        m_Pass = new TransparentShadowPass(injectionPoint, RenderQueueRange.transparent, lightCamera);
        m_Pass.ConfigureInput(requirements);
        if (lightCamera == null)
        {
            lightCamera = GameObject.FindGameObjectWithTag("CharacterShadow")?.GetComponent<Camera>();
        }
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_Pass.Setup("TransparentShadowMapRendererFeature", renderingData);
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_Pass.Dispose();
    }


    private class TransparentShadowPass : ScriptableRenderPass
    {
        /* Static Variables */
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("TransparentDepth");
        private static int  s_TransparentShadowAtlasId = Shader.PropertyToID("_TransparentShadowAtlas");
        private static int  s_ViewMatrixId = Shader.PropertyToID("_CharShadowViewM");
        private static int  s_ProjMatrixId = Shader.PropertyToID("_CharShadowProjM");
        private static int  s_atlasSize = 2048;


        /* Member Variables */
        private RTHandle m_TransparentShadowRT;
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;

        FilteringSettings m_FilteringSettings;

        public TransparentShadowPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, Camera lightCamera)
        {
            m_PassData = new PassData();
            m_FilteringSettings = new FilteringSettings(renderQueueRange);
            renderPassEvent = evt;
            if (lightCamera != null)
            {
                m_PassData.viewM = lightCamera.worldToCameraMatrix;
                m_PassData.projectM = lightCamera.projectionMatrix;
            }
        }

        public void Dispose()
        {
            m_TransparentShadowRT?.Release();
        }

        public void Setup(string featureName, in RenderingData renderingData)
        {
            m_ProfilingSampler = new ProfilingSampler(featureName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = new RenderTextureDescriptor(s_atlasSize, s_atlasSize, RenderTextureFormat.R16);
            RenderingUtils.ReAllocateIfNeeded(ref m_TransparentShadowRT, descriptor, FilterMode.Bilinear, name:"_TransparentShadowAtlas");
            cmd.SetGlobalTexture(s_TransparentShadowAtlasId, m_TransparentShadowRT.nameID);
            ConfigureTarget(m_TransparentShadowRT);
            ConfigureClear(ClearFlag.All, Color.black);
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

            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // cmd.SetGlobalMatrix(s_ViewMatrixId, passData.viewM);
                // cmd.SetGlobalMatrix(s_ProjMatrixId, passData.projectM);

                var drawSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                drawSettings.perObjectData = PerObjectData.None;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private class PassData
        {
            public ShaderTagId shaderTagId;
            public FilteringSettings filteringSettings;
            public ProfilingSampler profilingSampler;
            public Matrix4x4 viewM;
            public Matrix4x4 projectM;
        }
    }
}


