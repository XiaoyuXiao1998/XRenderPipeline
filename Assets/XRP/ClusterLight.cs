using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ClusterLight
{ 
    struct PointLight
    {
        public Vector3 color;
        public float intensity;
        public Vector3 position;
        public float radius;
    };
    struct Box
    {
        public Vector3 p0, p1, p2, p3, p4, p5, p6, p7;
    };

    struct LightIndex
    {
        public int start; //start index
        public int count; //
    };

    ComputeBuffer clusterBuffer;
    ComputeBuffer lightBuffer;
    ComputeBuffer lightAssignBuffer;
    ComputeBuffer assignTable;

    public static int maxNumLight = 16;
    public static int numClusterX = 16;
    public static int numClusterY = 16;
    public static int numClusterZ = 16;
    public static int numClusters = numClusterX * numClusterY * numClusterZ;
    ComputeShader clusterGenerateCS;
    ComputeShader assignLightCS;

    static int SIZE_OF_CLUSTETBOX = 8 * 3 * 4;
    //PointLight size
    static int SIZE_OF_LIGHT = (3 + 3 + 2) * 4;



    public ClusterLight()
    {
        int numClusters = numClusterX * numClusterY * numClusterZ;
        clusterBuffer = new ComputeBuffer(numClusters, SIZE_OF_CLUSTETBOX);
        lightBuffer = new ComputeBuffer(maxNumLight, SIZE_OF_LIGHT);
        lightAssignBuffer = new ComputeBuffer(maxNumLight * numClusters, sizeof(uint));
        assignTable = new ComputeBuffer(numClusters, 2 * sizeof(int));



       // clusterGenerateCS = Resources.Load<ComputeShader>("Shaders/ClusterGenerate");
        clusterGenerateCS = FindComputeShader("ClusterGenerate");
        assignLightCS = FindComputeShader("AssignLight");


        if (clusterGenerateCS == null)
        {
            Debug.Log("failed to load clusterGenerateCS");
        }
        if(assignLightCS == null)
        {
            Debug.Log("failed to load assignLightCS");
        }
    }

    ~ClusterLight()
    {
        lightBuffer.Release();
        clusterBuffer.Release();
        lightAssignBuffer.Release();
        assignTable.Release();
    }

    ComputeShader FindComputeShader(string shaderName)
    {
        ComputeShader[] css = Resources.FindObjectsOfTypeAll(typeof(ComputeShader)) as ComputeShader[];
        for (int i = 0; i < css.Length; i++)
        {
            if (css[i].name == shaderName)
                return css[i];
        }
        return null;
    }
    //cluster generate
    public void ClusterGenerate(Camera camera)
    {

        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 viewMatrixInv = viewMatrix.inverse;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        clusterGenerateCS.SetMatrix("ViewMatrix", viewMatrix);
        clusterGenerateCS.SetMatrix("ViewMatrixInv", viewMatrixInv);
        clusterGenerateCS.SetMatrix("VPMatrix", vpMatrix);
        clusterGenerateCS.SetMatrix("VPMatrixInv", vpMatrixInv);

        clusterGenerateCS.SetFloat("NumClusterX", numClusterX);
        clusterGenerateCS.SetFloat("NumClusterY", numClusterY);
        clusterGenerateCS.SetFloat("NumClusterZ", numClusterZ);

        int ClusterGenerateKernel = clusterGenerateCS.FindKernel("ClusterGenerate");


       
        clusterGenerateCS.SetBuffer(ClusterGenerateKernel, "ClusterBuffer", clusterBuffer);
        clusterGenerateCS.Dispatch(ClusterGenerateKernel, numClusterZ, 1, 1);

    }

    public void AssignLightsToClusters()
    {
        assignLightCS.SetFloat("NumClusterX", numClusterX);
        assignLightCS.SetFloat("NumClusterY", numClusterY);
        assignLightCS.SetFloat("NumClusterZ", numClusterZ);
        assignLightCS.SetInt("_maxLightsPerCluster", maxNumLight);

        int AssignLightKernel = assignLightCS.FindKernel("AssignLight");
        assignLightCS.SetBuffer(AssignLightKernel, "_lightAssignBuffer", lightAssignBuffer);
        assignLightCS.SetBuffer(AssignLightKernel, "_assignTable", assignTable);
        assignLightCS.SetBuffer(AssignLightKernel, "_clusterBuffer", clusterBuffer);
        assignLightCS.SetBuffer(AssignLightKernel, "_lightBuffer", lightBuffer);
        assignLightCS.Dispatch(AssignLightKernel, numClusterZ, 1, 1);
    }

    //transform lights data to gpu
    public void UpdateLightBuffer(VisibleLight[] Lights)
    {
        PointLight[] LightsToGPU = new PointLight[maxNumLight];
        int count = 0;

        for(uint i = 0; i < Lights.Length; i++)
        {
            if(Lights[i].light.type == LightType.Point)
            {
                LightsToGPU[count].color = new Vector3(Lights[i].light.color.r, Lights[i].light.color.g, Lights[i].light.color.b);
                LightsToGPU[count].intensity = Lights[i].light.intensity;
                LightsToGPU[count].position = Lights[i].light.transform.position;
                LightsToGPU[count].radius = Lights[i].light.range;
                count++;

            }
        }
        lightBuffer.SetData(LightsToGPU);
        assignLightCS.SetInt("_numLights",count);

    }


     void DrawBox(Box box, Color color)
    {
        Debug.DrawLine(box.p0, box.p1, color);
        Debug.DrawLine(box.p0, box.p2, color);
        Debug.DrawLine(box.p0, box.p4, color);

        Debug.DrawLine(box.p6, box.p2, color);
        Debug.DrawLine(box.p6, box.p7, color);
        Debug.DrawLine(box.p6, box.p4, color);

        Debug.DrawLine(box.p5, box.p1, color);
        Debug.DrawLine(box.p5, box.p7, color);
        Debug.DrawLine(box.p5, box.p4, color);

        Debug.DrawLine(box.p3, box.p1, color);
        Debug.DrawLine(box.p3, box.p2, color);
        Debug.DrawLine(box.p3, box.p7, color);
    }

    public void DebugGenerateClusters()
    {
        Box[] boxes = new Box[numClusterX * numClusterY * numClusterZ];
        clusterBuffer.GetData(boxes, 0, 0, numClusterX * numClusterY * numClusterZ);

        foreach (var box in boxes)
        {
       
            DrawBox(box, Color.yellow);
        }
    }


    public void DebugLightAssign()
    {

        Box[] boxes = new Box[numClusters];
        clusterBuffer.GetData(boxes, 0, 0, numClusters);

        LightIndex[] assignTables = new LightIndex[numClusters ];
        assignTable.GetData(assignTables, 0, 0, numClusters);


        //   uint[] lightAssignArray = new uint[numClusters * maxNumLight];
        //  lightAssignBuffer.GetData(lightAssignArray, 0, 0, numClusters * maxNumLight);

        for (int i= 0; i < assignTables.Length; i++)
        {
            LightIndex index = assignTables[i];
            if(index.count > 0)
            {

                DrawBox(boxes[i], Color.red);
            }
        }



    }

    public void SetShaderParameters()
    {
        Shader.SetGlobalFloat("NumClusterX", numClusterX);
        Shader.SetGlobalFloat("NumClusterY", numClusterY);
        Shader.SetGlobalFloat("NumClusterZ", numClusterZ);

        Shader.SetGlobalBuffer("_lightBuffer", lightBuffer);
        Shader.SetGlobalBuffer("_lightAssignBuffer", lightAssignBuffer);
        Shader.SetGlobalBuffer("_assignTable", assignTable);
    }

}
