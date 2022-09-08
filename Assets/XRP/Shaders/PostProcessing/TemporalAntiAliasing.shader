Shader "XRP/PostProcessing/TemporalAntiAliasing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Pass
        {
            CGPROGRAM


            /*
            float4x4 _LastVP;
            float4x4 _NonJitterVP;
            #define GetScreenPos(pos) ((float(pos.x,pos.y) * 0.5) / pos.w + 0.5)
            inline float2 CalculateMotionVector(float4x4 lastVP,float3 lastWorldPos, float2 screenUV) {
                float4 lastScreenPos = mul(lastVP, float4(lastWorldPos, 1));
                float4 lastScreenUV = GetScreenPos(lastScreenPos);
                return screenUV - lastScreenUV;

            }

            */

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _MainTex_ST;
            sampler2D _MainTex;
        

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            sampler2D _HistoryBuffer;
            float _BlendAlpha;

            fixed4 frag(v2f i) : SV_Target
            {
                
                   float4 hisotry = tex2D(_HistoryBuffer, i.uv);
                   float4 current = tex2D(_MainTex, float2(i.uv.x,1-i.uv.y));
                   return lerp(hisotry, current, _BlendAlpha);
     
            }
            ENDCG
        }
    }
}
