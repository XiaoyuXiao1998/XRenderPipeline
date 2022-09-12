Shader "XPR/lightpass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite On ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include"Shaders/BRDF.cginc"
             #include"Shaders/Cluster.cginc"
            #include "UnityLightingCommon.cginc"
           

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;
            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;
            float4 frag(v2f i, out float depthOut : SV_Depth) : SV_Target
            {
                float2 uv = i.uv;
                //*************************decode Gbuffer*********************************
                float3 albedo = tex2D(_GT0, uv).rgb;
                float3 normal = 2 * tex2D(_GT1, uv).rgb - 1.0;
                float2 metallicRoughness = tex2D(_GT2, uv).ba;
                float roughness = metallicRoughness.x;
                float metallic = metallicRoughness.y;


                //******************reconstruct world position *************************
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);
                depthOut = d;
                float4 ndcPos = float4(uv * 2 - 1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;


                //*************************sert light properties *******************************8
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 N = normalize(normal);
                float3 V = normalize(_WorldSpaceCameraPos.xyz -  worldPos.xyz);
                float3 radiance = _LightColor0.rgb;
                float3 PBR = CookTorranceBRDF(N, V, L, albedo, radiance, roughness, metallic) ;


                // ********************* clusterd based lighting *********************************
                // ******************** note that it is deferreding rendering pipeline************
                
                uint x = floor(uv.x * NumClusterX);
                uint y = floor(uv.y * NumClusterY);

                //unity use reverse z 
                //
                uint z = floor((1-d_lin) * NumClusterZ);

                uint ClusterID = z * NumClusterY * NumClusterX + y * NumClusterX + x;

                //read the lights table affetcting the cluster
                LightIndex AffectingLightsIndex = _assignTable[ClusterID];
                int start = AffectingLightsIndex.start;
                int end = start + AffectingLightsIndex.count;
                for (int i = start; i < end; i++)
                {
                    uint lightID = _lightAssignBuffer[i];
                    //read lights
                    PointLight pl = _lightBuffer[lightID];

                    radiance = pl.color;
                    L = normalize(pl.position - worldPos.xyz);

                    //compute point light attenuation
                    //Luminosity = 1 / Attenuation
                    //Attenuation = Constant + Linear * Distance + Quadratic * Distance^2

                    float dis = distance(pl.position, worldPos.xyz);
                    float d2 = dis * dis;
                    float r2 = pl.radius * pl.radius;
              
                    float Attenuation = saturate(1 - (d2 / r2) * (d2 / r2));
                     Attenuation *= Attenuation * r2/d2;

                    
                   PBR += CookTorranceBRDF(N, V, L, albedo, radiance, roughness, metallic) * pl.intensity * Attenuation;
                  
                }


                

                float4 color = float4(PBR, 1.0);
               
             //   color = float4(albedo, 1.0);
                return color;

            }

    
            ENDCG
        }
    }
}
