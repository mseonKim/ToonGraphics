using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;
using System.Collections.Generic;

namespace ToonGraphics
{
    public class CharacterShadowPass : ScriptableRenderPass
    {
        /* Static Variables */
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("CharacterDepth");
        private static int s_CharShadowAtlasId = Shader.PropertyToID("_CharShadowAtlas");
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

        private FilteringSettings m_FilteringSettings;
        


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

        public void Setup(string featureName, in RenderingData renderingData, Vector4 bias, Vector2 stepOffset, int size, int precision, bool enableAdditionalShadow)
        {
            m_ProfilingSampler = new ProfilingSampler(featureName);
            m_PassData.bias = bias;
            m_PassData.stepOffset = stepOffset;
            m_PassData.atlasSize = size;
            m_PassData.precision = precision;
            m_PassData.enableAdditionalShadow = enableAdditionalShadow;
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
            CharacterShadowUtils.SetShadowmapLightData(cmd, ref renderingData);

            var lightCameras = CharShadowCamera.Instance.lightCameras;
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
                    var lightCameras = CharShadowCamera.Instance.lightCameras;
                    for (int i = 0; i < 3; i++)
                    {
                        CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, i + 1);
                        cmd.SetGlobalFloat(s_ShadowMapIndex, i + 1);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                        charShadowDirections[i] = lightCameras[i + 1].transform.rotation * Vector3.forward;
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