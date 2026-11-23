using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CorgiShaderFix.Rendering
{
    /// <summary>
    /// Captures scene color, including transparents, then draws passes tagged MMRipple.
    /// Requires URP Render Graph (compatibility mode must be disabled).
    /// </summary>
    [DisallowMultipleRendererFeature("MM Ripple Grab Pass")]
    public sealed partial class MMRippleRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Optional GameObject layer filter. The shader's MMRipple LightMode selects the effects.")]
        public LayerMask layerMask = ~0;

        [Tooltip("Bilinear smooths refraction; Point preserves hard pixel edges.")]
        public FilterMode captureFilter = FilterMode.Bilinear;

        public bool renderInSceneView = true;

        [Tooltip("Enable if ripple objects belong to an overlay camera. Usually leave disabled for HUD cameras.")]
        public bool renderInOverlayCameras;

        [Tooltip("Enable together with Soft Particles on ripple materials. Transparent sprites usually do not write depth.")]
        public bool requestDepthTexture;

        private RipplePass _pass;

        public override void Create()
        {
            _pass = new RipplePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection
                || (!renderInSceneView && cameraData.isSceneViewCamera)
                || (!renderInOverlayCameras && cameraData.renderType == CameraRenderType.Overlay))
            {
                return;
            }

            _pass.LayerMask = layerMask;
            _pass.CaptureFilter = captureFilter;
            _pass.RequestDepthTexture = requestDepthTexture;

            // URP 17.5's 2D Renderer uses the Color input to allocate its intermediate
            // color target; Universal Renderer honors requiresIntermediateTexture directly.
            // Renderer2D itself is internal to URP; Renderer2DData is public.
            var inputs = renderer.GetType().Name == "Renderer2D"
                ? ScriptableRenderPassInput.Color : ScriptableRenderPassInput.None;
            if (requestDepthTexture)
            {
                inputs |= ScriptableRenderPassInput.Depth;
            }
            _pass.ConfigureInput(inputs);
            renderer.EnqueuePass(_pass);
        }

        private sealed partial class RipplePass : ScriptableRenderPass
        {
            private static readonly ShaderTagId RippleTag = new ShaderTagId("MMRipple");
            private static readonly int GrabTextureId = Shader.PropertyToID("_MMRippleGrabTexture");
            [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
            private static readonly ProfilingSampler CopySampler = new ProfilingSampler("MMRipple Capture Transparents");
            [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
            private static readonly ProfilingSampler DrawSampler = new ProfilingSampler("MMRipple Draw Distortion");

            public LayerMask LayerMask;
            public FilterMode CaptureFilter;
            public bool RequestDepthTexture;

            public RipplePass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                requiresIntermediateTexture = true;
            }

            private sealed class CopyData
            {
                public TextureHandle Source;
                public bool Bilinear;
            }

            private sealed class DrawData
            {
                public RendererListHandle RendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();

                // Never sample the backbuffer, or use a previous camera's global texture.
                if (resources.isActiveTargetBackBuffer || !resources.activeColorTexture.IsValid())
                {
                    return;
                }

                var source = resources.activeColorTexture;
                var descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "_MMRippleGrabTexture";
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.clearBuffer = false;
                descriptor.filterMode = CaptureFilter;
                descriptor.wrapMode = TextureWrapMode.Clamp;
                var grabTexture = renderGraph.CreateTexture(descriptor);

                using (var builder = renderGraph.AddRasterRenderPass<CopyData>("MMRipple Capture Transparents", out var data, CopySampler))
                {
                    data.Source = source;
                    data.Bilinear = CaptureFilter != FilterMode.Point;
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(grabTexture, 0, AccessFlags.WriteAll);
                    builder.SetGlobalTextureAfterPass(grabTexture, GrabTextureId);
                    builder.SetRenderFunc(static (CopyData passData, RasterGraphContext context) =>
                    {
                        RTHandle sourceHandle = passData.Source;
                        var scale = sourceHandle.useScaling ? sourceHandle.rtHandleProperties.rtHandleScale : Vector4.one;
                        Blitter.BlitTexture(context.cmd, sourceHandle, new Vector4(scale.x, scale.y, 0f, 0f), 0f, passData.Bilinear);
                    });
                }

                var drawingSettings = RenderingUtils.CreateDrawingSettings(RippleTag, renderingData, cameraData,
                    lightData, SortingCriteria.CommonTransparent);
                var filteringSettings = new FilteringSettings(RenderQueueRange.transparent, LayerMask);
                var rendererList = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                    drawingSettings, filteringSettings));

                using (var builder = renderGraph.AddRasterRenderPass<DrawData>("MMRipple Draw Distortion", out var data, DrawSampler))
                {
                    data.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.UseGlobalTexture(GrabTextureId, AccessFlags.Read);
                    // Blending requires the existing destination color.
                    builder.SetRenderAttachment(source, 0, AccessFlags.ReadWrite);
                    if (resources.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                    }
                    if (RequestDepthTexture && resources.cameraDepthTexture.IsValid())
                    {
                        builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    }
                    builder.SetRenderFunc(static (DrawData passData, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(passData.RendererList);
                    });
                }
            }
        }
    }
}
