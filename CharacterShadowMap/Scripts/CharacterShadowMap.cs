/// How to use
/// 1. Create "CharacterShadow" Tag and set camera prefab's tag as "CharacterShadow"
/// 2. Attach 'CharShadowCamera' script to new empty root hierarchy gameObject.
/// 3. Add pass in your shader to use 'CharacterShadowDepthPass.hlsl' with "CharacterDepth" LightMode. (See below example)
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
        R8 = 16,
        R16 = 28,
        RFloat = 14,
        RHalf = 15,
    }

    public class CharacterShadowMap : ScriptableRendererFeature
    {
        private CharacterShadowPass m_Pass;
        private Camera[] _LightCameras;
        public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPrePasses;
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;
        public float bias;
        public float normalBias;
        public float additionalBias;
        public float additionalNormalBias;
        public float stepOffset = 0.999f;
        public float additionalStepOffset = 0.999f;
        public CustomShadowMapSize atlasSize = CustomShadowMapSize._4096;
        public CustomShadowMapPrecision atlasPrecision = CustomShadowMapPrecision.RHalf;
        public bool enableAdditionalShadow = false;

        /// <inheritdoc/>
        public override void Create()
        {
            // [Deprecated]
            // if (_LightCameras[0] == null)
            //     _LightCameras[0] = GameObject.FindGameObjectWithTag("CharacterShadow")?.GetComponent<Camera>();

            _LightCameras = CharShadowCamera.lightCameras;
            m_Pass = new CharacterShadowPass(injectionPoint, RenderQueueRange.opaque);
            m_Pass.ConfigureInput(requirements);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_Pass.Setup(   "CharacterShadowMapRendererFeature", renderingData,
                            new Vector4(bias, normalBias, additionalBias, additionalNormalBias),
                            new Vector2(stepOffset, additionalStepOffset),
                            (int)atlasSize, (int)atlasPrecision, _LightCameras, enableAdditionalShadow);
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
            private static int s_CharShadowAtlasId = Shader.PropertyToID("_CharShadowAtlas");
            // private static int[] s_CharAddShadowAtlasIds = new int[3] {    Shader.PropertyToID("_CharAddShadowAtlas0"),
            //                                                                 Shader.PropertyToID("_CharAddShadowAtlas1"),
            //                                                                 Shader.PropertyToID("_CharAddShadowAtlas2") };
            private static int s_CharShadowBias = Shader.PropertyToID("_CharShadowBias");
            private static int s_ViewMatrixId = Shader.PropertyToID("_CharShadowViewM");
            private static int s_ProjMatrixId = Shader.PropertyToID("_CharShadowProjM");
            private static int s_ShadowOffset0Id = Shader.PropertyToID("_CharShadowOffset0");
            private static int s_ShadowOffset1Id = Shader.PropertyToID("_CharShadowOffset1");
            private static int s_ShadowMapSize = Shader.PropertyToID("_CharShadowmapSize");
            private static int s_ShadowStepOffset = Shader.PropertyToID("_CharShadowStepOffset");
            private static int s_ShadowMapIndex = Shader.PropertyToID("_CharShadowmapIndex");
            private static int s_CharShadowLightDirections = Shader.PropertyToID("_CharShadowLightDirections");


            /* Member Variables */
            private RTHandle m_CharShadowRT;
            private ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;
            private ProfilingSampler m_ProfilingSampler;
            private PassData m_PassData;

            FilteringSettings m_FilteringSettings;

            public CharacterShadowPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
            {
                m_PassData = new PassData();
                m_FilteringSettings = new FilteringSettings(renderQueueRange);
                renderPassEvent = evt;
                this.shaderTagId = k_ShaderTagId;
            }

            public void Dispose()
            {
                m_CharShadowRT?.Release();
            }

            public void Setup(string featureName, in RenderingData renderingData, Vector4 bias, Vector2 stepOffset, int size, int precision, Camera[] lightCameras, bool enableAdditionalShadow)
            {
                m_ProfilingSampler = new ProfilingSampler(featureName);
                m_PassData.bias = bias;
                m_PassData.stepOffset = stepOffset;
                m_PassData.atlasSize = size;
                m_PassData.precision = precision;
                m_PassData.enableAdditionalShadow = enableAdditionalShadow;

                int length = lightCameras.Length;
                m_PassData.viewM = new Matrix4x4[length];
                if (lightCameras != null && lightCameras[0] != null)
                {
                    float widthScale = (float)Screen.width / (float)Screen.height;
                    m_PassData.projectM = lightCameras[0].projectionMatrix;
                    for (int i = 0; i < length; i++)
                    {
                        if (lightCameras[i] != null)
                        {
                            m_PassData.viewM[i] = lightCameras[i].worldToCameraMatrix;
                            m_PassData.viewM[i].m00 *= widthScale;
                        }
                    }
                }
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = new RenderTextureDescriptor(m_PassData.atlasSize, m_PassData.atlasSize, (RenderTextureFormat)m_PassData.precision, 0);
                descriptor.dimension = TextureDimension.Tex2DArray;
                descriptor.sRGB = false;
                descriptor.volumeDepth = m_PassData.enableAdditionalShadow ? 4 : 1;
                RenderingUtils.ReAllocateIfNeeded(ref m_CharShadowRT, descriptor, FilterMode.Bilinear, name:"_CharShadowAtlas");
                cmd.SetGlobalTexture(s_CharShadowAtlasId, m_CharShadowRT.nameID);

                m_PassData.target = m_CharShadowRT;
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

                float invShadowAtlasWidth = 1.0f / passData.atlasSize;
                float invShadowAtlasHeight = 1.0f / passData.atlasSize;
                float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
                float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

                using (new ProfilingScope(cmd, passData.profilingSampler))
                {
                    cmd.SetGlobalVector(s_CharShadowBias, passData.bias);
                    cmd.SetGlobalMatrixArray(s_ViewMatrixId, passData.viewM);
                    cmd.SetGlobalMatrix(s_ProjMatrixId, passData.projectM);

                    // Soft shadow
                    cmd.SetGlobalVector(s_ShadowOffset0Id, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight));
                    cmd.SetGlobalVector(s_ShadowOffset1Id, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, invHalfShadowAtlasWidth, invHalfShadowAtlasHeight));
                    cmd.SetGlobalVector(s_ShadowMapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight, passData.atlasSize, passData.atlasSize));
                    cmd.SetGlobalVector(s_ShadowStepOffset, passData.stepOffset);
                    cmd.SetGlobalFloat(s_ShadowMapIndex, 0);
                    CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, 0);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var drawSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
                    // drawSettings.perObjectData = PerObjectData.None;

                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

                    if (passData.enableAdditionalShadow)
                    {
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                        var charShadowDirections = new Vector4[3] { Vector4.zero, Vector4.zero, Vector4.zero };
                        for (int i = 0; i < 3; i++)
                        {
                            CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, i + 1);
                            cmd.SetGlobalFloat(s_ShadowMapIndex, i + 1);
                            context.ExecuteCommandBuffer(cmd);
                            cmd.Clear();
                            charShadowDirections[i] = CharShadowCamera.lightCameras[i + 1].transform.rotation * Vector3.forward;
                            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                        }
                        cmd.SetGlobalVectorArray(s_CharShadowLightDirections, charShadowDirections);
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
                public Matrix4x4[] viewM;
                public Matrix4x4 projectM;
                public Vector4 bias;
                public Vector4 stepOffset;
                public int atlasSize;
                public int precision;
                public bool enableAdditionalShadow;
                public RTHandle target;
            }
        }
    }
}