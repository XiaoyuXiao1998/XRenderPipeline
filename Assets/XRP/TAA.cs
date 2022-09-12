using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public enum SAMPLE_METHOD
{
    HALTON_X2_Y3,
}

public class TAA 
{
    // Start is called before the first frame update

    static int Samples = 16;
    int FrameID;
    SAMPLE_METHOD sampleMethod;
    List<Vector2> samplePatterns;

    //reprojection variables for reprojection
    Matrix4x4 previousViewProjection;
    Vector2 previousOffset;
    Vector2 currentOffset;






     public TAA(SAMPLE_METHOD _sampleMethod)
    {
        sampleMethod = sampleMethod;
        FrameID = 0;
        if(sampleMethod == SAMPLE_METHOD.HALTON_X2_Y3)
        {
            samplePatterns = Sampler.HaltonSequence(2, 3).Skip(1).Take(Samples).ToList();
        }
    }

    public Vector2 getOffset()
    {
        return samplePatterns[(FrameID++) % Samples];

    }

    public void setJitterProjectionMatrix(ref Matrix4x4 jitteredProjection, ref Camera camera)
    {
        Vector2 offset = samplePatterns[(FrameID++) % Samples];
        jitteredProjection.m02 += (offset.x * 2 - 1) / Screen.width;
        jitteredProjection.m12 += (offset.y * 2- 1) / Screen.height;
    }







    







}
