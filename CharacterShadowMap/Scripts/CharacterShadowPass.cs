using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;
using System.Collections.Generic;

namespace ToonGraphics
{
    public class CharacterShadowPass : ScriptableRenderPass
    {
        /* ID */
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("CharacterDepth");
        private static class IDs
        {
            public static int _CharShadowMap = Shader.PropertyToID("_CharShadowMap");
            public static int _CharShadowBias = Shader.PropertyToID("_CharShadowBias");
            public static int _ViewMatrix = Shader.PropertyToID("_CharShadowViewM");
            public static int _ProjMatrix = Shader.PropertyToID("_CharShadowProjM");
            public static int _ShadowOffset0 = Shader.PropertyToID("_CharShadowOffset0");
            public static int _ShadowOffset1 = Shader.PropertyToID("_CharShadowOffset1");
            public static int _ShadowMapSize = Shader.PropertyToID("_CharShadowmapSize");
            public static int _ShadowStepOffset = Shader.PropertyToID("_CharShadowStepOffset");
            public static int _ShadowMapIndex = Shader.PropertyToID("_CharShadowmapIndex");
            public static int _CharShadowLightDirections = Shader.PropertyToID("_CharShadowLightDirections");
        }


        /* Member Variables */
        private RTHandle m_CharShadowRT;
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;
        private static int[] s_TextureSize = new int[2] { 1, 1 };

        private FilteringSettings m_FilteringSettings;
        


        public CharacterShadowPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
        {
            m_PassData = new PassData();
            m_FilteringSettings = new FilteringSettings(renderQueueRange);
            renderPassEvent = evt;
        }

        public void Dispose()
        {
            m_CharShadowRT?.Release();
        }

        public void Setup(string featureName, in RenderingData renderingData, Vector4 bias, Vector2 stepOffset, int scale, int precision, bool enableAdditionalShadow)
        {
            m_ProfilingSampler = new ProfilingSampler(featureName);
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_PassData.bias = bias;
            m_PassData.stepOffset = stepOffset;
            s_TextureSize[0] = descriptor.width * scale; s_TextureSize[1] = descriptor.height * scale;
            m_PassData.precision = precision;
            m_PassData.enableAdditionalShadow = enableAdditionalShadow;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = new RenderTextureDescriptor(s_TextureSize[0], s_TextureSize[1], (RenderTextureFormat)m_PassData.precision, 0);
            descriptor.dimension = TextureDimension.Tex2DArray;
            descriptor.sRGB = false;
            descriptor.volumeDepth = m_PassData.enableAdditionalShadow ? 4 : 1;
            RenderingUtils.ReAllocateIfNeeded(ref m_CharShadowRT, descriptor, FilterMode.Bilinear, name:"CharShadowMap");
            cmd.SetGlobalTexture(IDs._CharShadowMap, m_CharShadowRT);

            m_PassData.target = m_CharShadowRT;
            CharacterShadowUtils.SetShadowmapLightData(cmd, ref renderingData);

            var lightCameras = CharShadowCamera.Instance.lightCameras;
            if (lightCameras != null && lightCameras[0] != null)
            {
                int length = lightCameras.Length;
                m_PassData.viewM = new Matrix4x4[length];
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

            // Set global properties
            float invShadowMapWidth = 1.0f / s_TextureSize[0];
            float invShadowMapHeight = 1.0f / s_TextureSize[1];
            float invHalfShadowMapWidth = 0.5f * invShadowMapWidth;
            float invHalfShadowMapHeight = 0.5f * invShadowMapHeight;

            cmd.SetGlobalVector(IDs._CharShadowBias, m_PassData.bias);
            cmd.SetGlobalMatrixArray(IDs._ViewMatrix, m_PassData.viewM);
            cmd.SetGlobalMatrix(IDs._ProjMatrix, m_PassData.projectM);

            cmd.SetGlobalVector(IDs._ShadowOffset0, new Vector4(-invHalfShadowMapWidth, -invHalfShadowMapHeight, invHalfShadowMapWidth, -invHalfShadowMapHeight));
            cmd.SetGlobalVector(IDs._ShadowOffset1, new Vector4(-invHalfShadowMapWidth, invHalfShadowMapHeight, invHalfShadowMapWidth, invHalfShadowMapHeight));
            cmd.SetGlobalVector(IDs._ShadowMapSize, new Vector4(invShadowMapWidth, invShadowMapHeight, s_TextureSize[0], s_TextureSize[1]));
            cmd.SetGlobalVector(IDs._ShadowStepOffset, m_PassData.stepOffset);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.filteringSettings = m_FilteringSettings;
            m_PassData.profilingSampler = m_ProfilingSampler;

            ExecuteCharShadowPass(context, m_PassData, ref renderingData);
        }

        private static void ExecuteCharShadowPass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            var filteringSettings = passData.filteringSettings;

            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                cmd.SetGlobalFloat(IDs._ShadowMapIndex, 0);
                CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, 0);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = RenderingUtils.CreateDrawingSettings(k_ShaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
                // drawSettings.perObjectData = PerObjectData.None;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

                if (passData.enableAdditionalShadow)
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    var charShadowDirections = new Vector4[3] { Vector4.zero, Vector4.zero, Vector4.zero };
                    var lightCameras = CharShadowCamera.Instance.lightCameras;
                    for (int i = 0; i < CharacterShadowUtils.activeSpotLightCount; i++)
                    {
                        if (lightCameras[i + 1] == null)
                            continue;
                        charShadowDirections[i] = lightCameras[i + 1].transform.rotation * Vector3.forward;
                        CoreUtils.SetRenderTarget(cmd, passData.target, ClearFlag.Color, 0, CubemapFace.Unknown, i + 1);
                        cmd.SetGlobalFloat(IDs._ShadowMapIndex, i + 1);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                    }
                    cmd.SetGlobalVectorArray(IDs._CharShadowLightDirections, charShadowDirections);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private class PassData
        {
            public FilteringSettings filteringSettings;
            public ProfilingSampler profilingSampler;
            public Matrix4x4[] viewM;
            public Matrix4x4 projectM;
            public Vector4 bias;
            public Vector4 stepOffset;
            public int precision;
            public bool enableAdditionalShadow;
            public int activeSpotlightCount;
            public RTHandle target;
        }
    }
}