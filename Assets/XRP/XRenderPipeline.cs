using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class XRenderPipeline : RenderPipeline
{
    //depth attachment
    RenderTexture gDepth;
    RenderTexture[] gBuffers = new RenderTexture[4];
    RenderTargetIdentifier[] gBufferID = new RenderTargetIdentifier[4];

    //construction function of render pipeline:
    public XRenderPipeline()
    {
        
        gDepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gBuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gBuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gBuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);


        // 给纹理 ID 赋值
        for (int i = 0; i < 4; i++)
            gBufferID[i] = gBuffers[i];

    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //set cameras
        Camera camera = cameras[0];
        context.SetupCameraProperties(camera);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";

        //set gbuffers to global texture;
        cmd.SetGlobalTexture("_gDepth", gDepth);
        for (int i = 0; i < 4; i++)
         cmd.SetGlobalTexture("_GT" + i, gBuffers[i]);

        //clear the window
        cmd.SetRenderTarget(gBufferID, gDepth);

        cmd.ClearRenderTarget(true, true, Color.red);
        context.ExecuteCommandBuffer(cmd);

        LightPass(context, camera);
        
        //context.ExecuteCommandBuffer(cmd);
        //culling454 
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

        //config setting

        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");   // 使用 LightMode 为 gbuffer 的 shader
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        // 绘制
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // skybox and Gizmos
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // 提交绘制命令
        context.Submit();

    }

    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        Material mat = new Material(Shader.Find("XPR/lightpass"));
        cmd.Blit(gBufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
    }
}
