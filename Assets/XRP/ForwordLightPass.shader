Shader "XPR/ForwordLightPass"
{
     Properties
     {
         _MainTex("Texture", 2D) = "white" {}
     }
     SubShader
     {
         // No culling or depth
         Cull Off ZWrite Off ZTest Always

         Pass
         {
             CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag

             #include "UnityCG.cginc"
             #include"Shaders/BRDF.cginc"
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

             v2f vert(appdata v)
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
             float4 frag(v2f i) : SV_Target
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
                 float4 ndcPos = float4(uv * 2 - 1, d, 1);
                 float4 worldPos = mul(_vpMatrixInv, ndcPos);
                 worldPos /= worldPos.w;


                 //*************************sert light properties *******************************8
                 float3 L = normalize(_WorldSpaceLightPos0.xyz);
                 float3 N = normalize(normal);
                 float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                 float3 radiance = _LightColor0.rgb;
                 float3 PBR = CookTorranceBRDF(N, V, L, albedo, radiance, roughness, metallic);



                 float4 color = float4(PBR, 1.0);
                 //   color = float4(albedo, 1.0);
                    return color;

                }


                ENDCG
            }
     }

}
