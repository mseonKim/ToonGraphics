/// How to use
/// 1. Add 'CharacterShadowCamera' prefab to your scene.
/// 2. Add pass in your shader to use 'CharacterShadowDepthPass.hlsl' with "CharacterDepth" LightMode. (See below example)
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
            public static int _CharShadowCascadeParams = Shader.PropertyToID("_CharShadowCascadeParams");
            public static int _UseAdditonalCharShadow = Shader.PropertyToID("_UseAdditonalCharShadow");
            public static int _UseBrightestLightOnly = Shader.PropertyToID("_UseBrightestLightOnly");
        }


        /* Member Variables */
        private RTHandle m_CharShadowRT;
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;
        private static int[] s_TextureSize = new int[2] { 1, 1 };
        private CharSoftShadowMode m_SoftShadowMode;
        private float m_cascadeResolutionScale = 1f;
        private float m_cascadeMaxDistance;
        private bool m_UseBrightestLightOnly;

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

        public void Setup(string featureName, in RenderingData renderingData, CharacterShadowConfig config, bool additionalShadowEnabled)
        {
            m_ProfilingSampler = new ProfilingSampler(featureName);
            m_PassData.bias = new Vector4(config.bias, config.normalBias, config.additionalBias, config.additionalNormalBias);
            m_PassData.stepOffset = new Vector2(config.stepOffset, config.additionalStepOffset);
            var scale = (int)config.textureScale;
            m_cascadeResolutionScale = CharacterShadowUtils.FindCascadedShadowMapResolutionScale(renderingData, config.cascadeSplit);
            m_cascadeMaxDistance = config.cascadeSplit.w;
            s_TextureSize[0] = 1024 * scale;
            s_TextureSize[1] = 1024 * scale;
            m_PassData.precision = (int)config.precision;
            m_PassData.enableAdditionalShadow = additionalShadowEnabled;
            m_PassData.useBrightestLightOnly = config.useBrightestLightOnly;
            m_SoftShadowMode = config.softShadowMode;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = new RenderTextureDescriptor(s_TextureSize[0], s_TextureSize[1], (RenderTextureFormat)m_PassData.precision, 0);
            descriptor.dimension = TextureDimension.Tex2DArray;
            descriptor.sRGB = false;
            descriptor.volumeDepth = m_PassData.enableAdditionalShadow ? 4 : 1;
            // Allocate Char Shadowmap
            RenderingUtils.ReAllocateIfNeeded(ref m_CharShadowRT, descriptor, FilterMode.Bilinear, name:"CharShadowMap");
            cmd.SetGlobalTexture(IDs._CharShadowMap, m_CharShadowRT);

            m_PassData.charShadowRT = m_CharShadowRT;
            cmd.SetGlobalVector(IDs._CharShadowCascadeParams, new Vector4(m_cascadeMaxDistance, m_cascadeResolutionScale, 0, 0));
            cmd.SetGlobalInt(IDs._UseAdditonalCharShadow, m_PassData.enableAdditionalShadow ? 1 : 0);
            cmd.SetGlobalInt(IDs._UseBrightestLightOnly, m_PassData.useBrightestLightOnly ? 1 : 0);
            CoreUtils.SetKeyword(cmd, "_HIGH_CHAR_SOFTSHADOW", m_SoftShadowMode == CharSoftShadowMode.High);
        }

        private static void SetCharShadowConfig(CommandBuffer cmd, PassData passData, ref RenderingData renderingData)
        {
            CharacterShadowUtils.SetShadowmapLightData(cmd, ref renderingData, passData.useBrightestLightOnly);

            var lightCameras = CharShadowCamera.Instance.lightCameras;
            if (lightCameras != null && lightCameras[0] != null)
            {
                int length = lightCameras.Length;
                passData.viewM = new Matrix4x4[length];
                float widthScale = (float)Screen.width / (float)Screen.height;
                passData.projectM = lightCameras[0].projectionMatrix;
                for (int i = 0; i < length; i++)
                {
                    if (lightCameras[i] != null)
                    {
                        passData.viewM[i] = lightCameras[i].worldToCameraMatrix;
                        passData.viewM[i].m00 *= widthScale;
                    }
                }
            }

            // Set global properties
            float invShadowMapWidth = 1.0f / s_TextureSize[0];
            float invShadowMapHeight = 1.0f / s_TextureSize[1];
            float invHalfShadowMapWidth = 0.5f * invShadowMapWidth;
            float invHalfShadowMapHeight = 0.5f * invShadowMapHeight;

            cmd.SetGlobalVector(IDs._CharShadowBias, passData.bias);
            cmd.SetGlobalMatrixArray(IDs._ViewMatrix, passData.viewM);
            cmd.SetGlobalMatrix(IDs._ProjMatrix, passData.projectM);

            cmd.SetGlobalVector(IDs._ShadowOffset0, new Vector4(-invHalfShadowMapWidth, -invHalfShadowMapHeight, invHalfShadowMapWidth, -invHalfShadowMapHeight));
            cmd.SetGlobalVector(IDs._ShadowOffset1, new Vector4(-invHalfShadowMapWidth, invHalfShadowMapHeight, invHalfShadowMapWidth, invHalfShadowMapHeight));
            cmd.SetGlobalVector(IDs._ShadowMapSize, new Vector4(invShadowMapWidth, invShadowMapHeight, s_TextureSize[0], s_TextureSize[1]));
            cmd.SetGlobalVector(IDs._ShadowStepOffset, passData.stepOffset);
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
            var drawSettings = RenderingUtils.CreateDrawingSettings(k_ShaderTagId, ref renderingData, SortingCriteria.CommonOpaque);

            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                // Shadowmap
                SetCharShadowConfig(cmd, passData, ref renderingData);
                cmd.SetGlobalFloat(IDs._ShadowMapIndex, 0);
                CoreUtils.SetRenderTarget(cmd, passData.charShadowRT, ClearFlag.Color, 0, CubemapFace.Unknown, 0);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

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
                        CoreUtils.SetRenderTarget(cmd, passData.charShadowRT, ClearFlag.Color, 0, CubemapFace.Unknown, i + 1);
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
            public bool useBrightestLightOnly;
            public int activeSpotlightCount;
            public RTHandle charShadowRT;
        }
    }
}