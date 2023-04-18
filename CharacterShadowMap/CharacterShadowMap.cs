/// How to use
/// 1. Create "CharacterShadow" Tag and set camera tag as "CharacterShadow"
/// 2. Create camera and set rotation based on main directional light
/// 3. Add Cinemachine component and Set Follow (Usually Hips Transform)
/// 4. Set "Body - Transposer | Binding Mode = Lock To Target On Assign"
///    & Set "Follow Offset" to adjust camera position
/// 5. Set FOV
/// 6. Add pass in your shader to use 'CharacterShadowDepthPass.hlsl' with "CharacterDepth" LightMode. (See below example)
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
 *       #include "Packages/com.unity.toongraphics/CharacterShadowMap/CharacterShadowDepthPass.hlsl"
 *       ENDHLSL
 *   }
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class CharacterShadowMap : ScriptableRendererFeature
    {
        CharacterShadowPass m_Pass;
        public static Camera lightCamera;
        public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPrePasses;
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;
        public float bias;
        public float stepOffset = 0.999f;

        /// <inheritdoc/>
        public override void Create()
        {
            m_Pass = new CharacterShadowPass(injectionPoint, RenderQueueRange.opaque, lightCamera);
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
            m_Pass.Setup("CharacterShadowMapRendererFeature", renderingData, bias, stepOffset);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass.Dispose();
        }


        private class CharacterShadowPass : ScriptableRenderPass
        {
            /* Static Variables */
            private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("CharacterDepth");
            private static int  s_CharShadowAtlasId = Shader.PropertyToID("_CharShadowAtlas");
            private static int  s_CharShadowBias = Shader.PropertyToID("_CharShadowBias");
            private static int  s_ViewMatrixId = Shader.PropertyToID("_CharShadowViewM");
            private static int  s_ProjMatrixId = Shader.PropertyToID("_CharShadowProjM");
            private static int  s_ShadowOffset0Id = Shader.PropertyToID("_CharShadowOffset0");
            private static int  s_ShadowOffset1Id = Shader.PropertyToID("_CharShadowOffset1");
            private static int  s_ShadowMapSize = Shader.PropertyToID("_CharShadowmapSize");
            private static int  s_ShadowStepOffset = Shader.PropertyToID("_CharShadowStepOffset");
            private static int  s_atlasSize = 4096;


            /* Member Variables */
            private RTHandle m_CharShadowRT;
            // private RTHandle m_TransparentShadowRT;
            private ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;
            private ProfilingSampler m_ProfilingSampler;
            private PassData m_PassData;

            FilteringSettings m_FilteringSettings;

            public CharacterShadowPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, Camera lightCamera)
            {
                m_PassData = new PassData();
                m_FilteringSettings = new FilteringSettings(renderQueueRange);
                renderPassEvent = evt;
                this.shaderTagId = k_ShaderTagId;
                if (lightCamera != null)
                {
                    float widthScale = (float)Screen.width / (float)Screen.height;
                    m_PassData.viewM = lightCamera.worldToCameraMatrix;
                    m_PassData.viewM.m00 *= widthScale;
                    m_PassData.projectM = lightCamera.projectionMatrix;
                }
            }

            public void Dispose()
            {
                m_CharShadowRT?.Release();
            }

            public void Setup(string featureName, in RenderingData renderingData, float bias, float stepOffset)
            {
                m_ProfilingSampler = new ProfilingSampler(featureName);
                m_PassData.bias = bias;
                m_PassData.stepOffset = stepOffset;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // var descriptor = new RenderTextureDescriptor(s_atlasSize, s_atlasSize, RenderTextureFormat.RGB111110Float, 0);
                var descriptor = new RenderTextureDescriptor(s_atlasSize, s_atlasSize, RenderTextureFormat.Shadowmap, 32);
                // RTHandles.Alloc(descriptor, FilterMode.Point, name:"_CharShadowAtlas");
                RenderingUtils.ReAllocateIfNeeded(ref m_CharShadowRT, descriptor, FilterMode.Bilinear, name:"_CharShadowAtlas");
                cmd.SetGlobalTexture(s_CharShadowAtlasId, m_CharShadowRT.nameID);

                ConfigureTarget(m_CharShadowRT);
                ConfigureClear(ClearFlag.All, Color.black);
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                m_PassData.shaderTagId = this.shaderTagId;
                m_PassData.filteringSettings = m_FilteringSettings;
                m_PassData.profilingSampler = m_ProfilingSampler;

                ExecuteCharShadowPass(context, m_PassData, ref renderingData);
            }

            private static void ExecuteCharShadowPass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                var filteringSettings = passData.filteringSettings;

                float invShadowAtlasWidth = 1.0f / s_atlasSize;
                float invShadowAtlasHeight = 1.0f / s_atlasSize;
                float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
                float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

                using (new ProfilingScope(cmd, passData.profilingSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.SetGlobalFloat(s_CharShadowBias, passData.bias);
                    cmd.SetGlobalMatrix(s_ViewMatrixId, passData.viewM);
                    cmd.SetGlobalMatrix(s_ProjMatrixId, passData.projectM);

                    // Soft shadow
                    cmd.SetGlobalVector(s_ShadowOffset0Id,
                        new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight,
                            invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight));
                    cmd.SetGlobalVector(s_ShadowOffset1Id,
                        new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight,
                            invHalfShadowAtlasWidth, invHalfShadowAtlasHeight));
                    cmd.SetGlobalVector(s_ShadowMapSize, new Vector4(invShadowAtlasWidth,
                        invShadowAtlasHeight,
                        s_atlasSize, s_atlasSize));
                    cmd.SetGlobalFloat(s_ShadowStepOffset, passData.stepOffset);

                    // var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                    var drawSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
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
                public float bias;
                public float stepOffset;

            }
        }
    }
}