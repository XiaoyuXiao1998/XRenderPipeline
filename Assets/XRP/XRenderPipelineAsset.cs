using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/XRenderPipeline")]
public class XRenderPipelineAsset : RenderPipelineAsset
{

    protected override RenderPipeline CreatePipeline()
    {
 
        return new XRenderPipeline();
    }
     
       
}
