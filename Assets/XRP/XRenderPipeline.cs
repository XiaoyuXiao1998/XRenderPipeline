using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Rendering;
public class XRenderPipeline : RenderPipeline
{
    //depth attachment
    RenderTexture gDepth;
    RenderTexture[] gBuffers = new RenderTexture[4];
    RenderTargetIdentifier[] gBufferID = new RenderTargetIdentifier[4];
    RenderTargetIdentifier gDepthID;

    //cluster light 
     ClusterLight clusterLight;

    //test simple TAA
    RenderTexture HistoryBuffer;
    RenderTargetIdentifier HistoryBufferID;

    RenderTexture outputTAA;
    RenderTargetIdentifier outputTAAID;
    float BlendAlpha = 0.1f;
    RenderTexture temp;
    TAA taa;
    int frameID;
    Matrix4x4 basematrix;


    void InitTAATexture()
    {
        HistoryBuffer = new RenderTexture(Screen.width, Screen.height, 0);
        HistoryBuffer.dimension = TextureDimension.Tex2D;
        HistoryBuffer.Create();

        outputTAA = new RenderTexture(Screen.width, Screen.height, 0);
        outputTAA.dimension = TextureDimension.Tex2D;
        outputTAA.Create();

        HistoryBufferID = HistoryBuffer;
        outputTAAID = outputTAA;
    }

    //construction function of render pipeline:
    public XRenderPipeline()
    {
        
        gDepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gBuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gBuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gBuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);


        // 给纹理 ID 赋值
        gDepthID = gDepth;
        for (int i = 0; i < 4; i++)
            gBufferID[i] = gBuffers[i];
        InitTAATexture();

        taa = new TAA(SAMPLE_METHOD.HALTON_X2_Y3);
        frameID = 0;


     //   clusterLight = new ClusterLight();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //set cameras
        Camera camera = cameras[0];
        if (frameID == 0)
            basematrix = camera.projectionMatrix;



       

        //**********************set gbuffer global textures **********************************
        Shader.SetGlobalTexture("_gdepth", gDepth);
        for (int i = 0; i < 4; i++)
            Shader.SetGlobalTexture("_GT" + i, gBuffers[i]);


        //****************************set TAA Pass**********************************************
        var jitteredProjection = basematrix;
        
        var cmd = new CommandBuffer();
        //cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, jitteredProjection);
        taa.setJitterProjectionMatrix(ref jitteredProjection, ref camera);
        cmd.SetViewMatrix(camera.worldToCameraMatrix );
        cmd.SetProjectionMatrix(jitteredProjection);
        camera.projectionMatrix = jitteredProjection;



        // ********************set camera matrix *********************************************
        Matrix4x4 view = camera.worldToCameraMatrix;
        Matrix4x4 projection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = jitteredProjection * view;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
        Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

        GBufferPass(context, camera);
      //  ClusterLightingPass(context, camera);
        LightPass(context, camera);


        // ******************************TAA pass ****************************************************

        if (frameID >= 1)
           
        {

            Material mat = new Material(Shader.Find("XRP/PostProcessing/TemporalAntiAliasing"));
            cmd.SetGlobalTexture("_HistoryBuffer", HistoryBuffer);
            cmd.SetGlobalFloat("_BlendAlpha", BlendAlpha);
            

            cmd.Blit(BuiltinRenderTextureType.CameraTarget, outputTAAID, mat);

            cmd.Blit(outputTAAID, HistoryBufferID); // Save current frame for next frame.
            cmd.Blit(outputTAAID, BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(cmd);

            context.Submit();
        }
        else
        {
            cmd.Blit(BuiltinRenderTextureType.CameraTarget, HistoryBufferID);
            cmd.SetGlobalTexture("_HistoryBuffer", HistoryBuffer);

        }

        frameID++;



        // TAAPass(context, camera);
        context.DrawSkybox(camera);
       // 
        // skybox and Gizmos

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

        context.Submit();
    }
    void GBufferPass(ScriptableRenderContext context, Camera camera)
    {
        context.SetupCameraProperties(camera);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";

        // 清屏
        cmd.SetRenderTarget(gBufferID, gDepthID);
        cmd.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 剔除
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

        // config settings
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");   // 使用 LightMode 为 gbuffer 的 shader
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        // 绘制一般几何体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        context.Submit();

    }

    void ClusterLightingPass(ScriptableRenderContext context, Camera camera)
    {
        camera.TryGetCullingParameters(out var cullingParameters);
        var CullingResults = context.Cull(ref cullingParameters);
        clusterLight.ClusterGenerate(camera);
        //clusterLight.DebugGenerateClusters();

        //update light buffers
        clusterLight.UpdateLightBuffer(CullingResults.visibleLights.ToArray());
        //light assign 
        clusterLight.AssignLightsToClusters();
        // clusterLight.DebugLightAssign();
        clusterLight.SetShaderParameters();


    }


    void TAAPass(ScriptableRenderContext context, Camera camera)
    {
  
        Vector2 offset = taa.getOffset();// ... Get a sampling offset from sampling pattern.
        var jitteredProjection = camera.projectionMatrix;
        jitteredProjection.m02 += (offset.x * 2 - 1) / camera.pixelWidth;
        jitteredProjection.m12 += (offset.y * 2 - 1) / camera.pixelHeight;
        var cmd = new CommandBuffer();
        cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, jitteredProjection); 
        Material mat = new Material(Shader.Find("XRP/PostProcessing/TemporalAntiAliasing"));
        cmd.SetGlobalTexture("_HistoryBuffer", HistoryBuffer);
        cmd.SetGlobalFloat("_BlendAlpha", BlendAlpha);
        RenderTexture outputTAA = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0);
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, outputTAA, mat);

        cmd.Blit(outputTAA, HistoryBuffer); // Save current frame for next frame.
        cmd.Blit(outputTAA, BuiltinRenderTextureType.CameraTarget);

        context.ExecuteCommandBuffer(cmd);

        context.Submit();
    }


}


