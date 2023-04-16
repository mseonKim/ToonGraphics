using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonGraphics
{
    public class LinkedListOIT : ScriptableRendererFeature
    {
        public Material fullscreenMaterial;
        public ComputeShader oitComputeUtilsCS;
        private OitPass oitPass;

        public override void Create()
        {
            oitPass?.Cleanup();
            oitPass = new OitPass(fullscreenMaterial, oitComputeUtilsCS);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Calling ConfigureInput with the ScriptableRenderPassInput.Color argument ensures that the opaque texture is available to the Render Pass
            oitPass.ConfigureInput(ScriptableRenderPassInput.Color);
            oitPass.Setup(renderingData);
            renderer.EnqueuePass(oitPass);
        }

        protected override void Dispose(bool disposing)
        {
            oitPass.Cleanup();
        }
    }

    public class OitPass : ScriptableRenderPass
    {
        private readonly OitLinkedList orderIndependentTransparency;
        private Material material;
        private RTHandle m_CopiedColor;
        private static readonly int s_BlitTextureShaderID = Shader.PropertyToID("_BlitTexture");

        public OitPass(Material material, ComputeShader oitComputeUtilsCS)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            orderIndependentTransparency = new OitLinkedList(oitComputeUtilsCS);
            RenderPipelineManager.beginContextRendering += PreRender;
            this.material = material;
        }

        private void PreRender(ScriptableRenderContext context, List<Camera> cameras)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.Clear();
            using (new ProfilingScope(cmd, new ProfilingSampler("Order Independent Transparency Pre Render")))
            {
                orderIndependentTransparency.PreRender(cmd);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Setup(in RenderingData renderingData)
        {
            var colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            colorCopyDescriptor.depthBufferBits = (int) DepthBits.None;
            RenderingUtils.ReAllocateIfNeeded(ref m_CopiedColor, colorCopyDescriptor, name: "_OITPassColorCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            using (new ProfilingScope(cmd, new ProfilingSampler("Order Independent Transparency")))
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
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            orderIndependentTransparency.Release();
            m_CopiedColor?.Release();
            RenderPipelineManager.beginContextRendering -= PreRender;
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
            if (Screen.width != screenWidth || Screen.height != screenHeight)
            {
                SetupGraphicsBuffers();
            }

            //reset StartOffsetBuffer to zeros
            oitComputeUtils.Dispatch(clearStartOffsetBufferKernel, dispatchGroupSizeX, dispatchGroupSizeY, 1);

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
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            int bufferSize = screenWidth * screenHeight * MAX_SORTED_PIXELS;
            int bufferStride = sizeof(uint) * 3;
            //the structured buffer contains all information about the transparent fragments
            //this is the per pixel linked list on the gpu
            fragmentLinkBuffer = new ComputeBuffer(bufferSize, bufferStride, ComputeBufferType.Counter);

            int bufferSizeHead = screenWidth * screenHeight;
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