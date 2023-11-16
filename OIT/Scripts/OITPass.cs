using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class OitPass : ScriptableRenderPass
    {
        private readonly OitLinkedList orderIndependentTransparency;
        private Material material;
        private RTHandle m_CopiedColor;
        private static readonly int s_BlitTextureShaderID = Shader.PropertyToID("_BlitTexture");
        private static ShaderTagId s_OutlineShaderTagId = new ShaderTagId("TransparentOutline");
        private ProfilingSampler m_ProfilingSampler;

        public OitPass(Material material, ComputeShader oitComputeUtilsCS)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            orderIndependentTransparency = new OitLinkedList(oitComputeUtilsCS);
            this.material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Order Independent Transparency PreRender")))
            {
                orderIndependentTransparency.PreRender(cmd);
            }
            var colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            colorCopyDescriptor.depthBufferBits = (int) DepthBits.None;
            RenderingUtils.ReAllocateIfNeeded(ref m_CopiedColor, colorCopyDescriptor, name: "_OITPassColorCopy");
        }

        public void Setup(string featureName)
        {
            m_ProfilingSampler = new ProfilingSampler(featureName);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var src = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (material != null && src.rt != null)
                {
                    orderIndependentTransparency.SetMaterialData(cmd, material);
                    Blitter.BlitCameraTexture(cmd, src, m_CopiedColor);
                    material.SetTexture(s_BlitTextureShaderID, m_CopiedColor);

                    CoreUtils.SetRenderTarget(cmd, src);
                    CoreUtils.DrawFullScreen(cmd, material);
                    // Blitter.BlitTexture(cmd, src, src, material, 0);

                    var drawSettings = RenderingUtils.CreateDrawingSettings(s_OutlineShaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                    var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            orderIndependentTransparency.Release();
            m_CopiedColor?.Release();
        }
    }


    public class OitLinkedList
    {
        private int screenWidth, screenHeight;
        private ComputeBuffer fragmentLinkBuffer;
        private readonly int fragmentLinkBufferId;
        private ComputeBuffer startOffsetBuffer;
        private readonly int startOffsetBufferId;
        private readonly Material linkedListMaterial;
        private const int MAX_SORTED_PIXELS = 8;

        public ComputeShader oitComputeUtils;
        private readonly int clearStartOffsetBufferKernel;
        private int dispatchGroupSizeX, dispatchGroupSizeY;

        public OitLinkedList(ComputeShader oitComputeUtilsCS)
        {
            fragmentLinkBufferId = Shader.PropertyToID("FLBuffer");
            startOffsetBufferId = Shader.PropertyToID("StartOffsetBuffer");

            oitComputeUtils = oitComputeUtilsCS;
            clearStartOffsetBufferKernel = oitComputeUtils.FindKernel("ClearStartOffsetBuffer");
            SetupGraphicsBuffers();
        }

        public void PreRender(CommandBuffer command)
        {
            // validate the effect itself
            if (Screen.width * 2 != screenWidth || Screen.height * 2 != screenHeight)
            {
                SetupGraphicsBuffers();
            }

            //reset StartOffsetBuffer to zeros
            command.DispatchCompute(oitComputeUtils, clearStartOffsetBufferKernel, dispatchGroupSizeX, dispatchGroupSizeY, 1);

            // set buffers for rendering
            command.SetRandomWriteTarget(1, fragmentLinkBuffer);
            command.SetRandomWriteTarget(2, startOffsetBuffer);
        }

        public void SetMaterialData(CommandBuffer command, Material material)
        {
            command.ClearRandomWriteTargets();
            material.SetBuffer(fragmentLinkBufferId, fragmentLinkBuffer);
            material.SetBuffer(startOffsetBufferId, startOffsetBuffer);
        }

        public void Release()
        {
            fragmentLinkBuffer?.Dispose();
            startOffsetBuffer?.Dispose();
        }

        private void SetupGraphicsBuffers()
        {
            Release();
            screenWidth = Screen.width; // * 2
            screenHeight = Screen.height; // * 2

            int bufferSize = Mathf.Max(screenWidth * screenHeight * MAX_SORTED_PIXELS, 1);
            int bufferStride = sizeof(uint) * 3;
            //the structured buffer contains all information about the transparent fragments
            //this is the per pixel linked list on the gpu
            fragmentLinkBuffer = new ComputeBuffer(bufferSize, bufferStride, ComputeBufferType.Counter);

            int bufferSizeHead = Mathf.Max(screenWidth * screenHeight, 1);
            int bufferStrideHead = sizeof(uint);
            //create buffer for addresses, this is the head of the linked list
            startOffsetBuffer = new ComputeBuffer(bufferSizeHead, bufferStrideHead, ComputeBufferType.Raw);

            oitComputeUtils.SetBuffer(clearStartOffsetBufferKernel, startOffsetBufferId, startOffsetBuffer);
            oitComputeUtils.SetInt("screenWidth", screenWidth);
            dispatchGroupSizeX = Mathf.CeilToInt(screenWidth / 32.0f);
            dispatchGroupSizeY = Mathf.CeilToInt(screenHeight / 32.0f);
        }
    }
}