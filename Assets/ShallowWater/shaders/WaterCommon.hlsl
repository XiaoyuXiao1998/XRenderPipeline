
//������
half3 GetAmbientLight() {
    return half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}

half3 WaterDiffuse(float3 normal, Light light) {
    return saturate(dot(normal, light.direction) * light.color) * light.shadowAttenuation;
}

//����ˮ��ĸ߹�
half3 WaterSpecular(float3 viewDir, float3 normal, float gloss, float shininess) {
    Light mainLight = GetMainLight();
    float3 halfDir = normalize(mainLight.direction + viewDir);
    float nl = max(0, dot(halfDir, normal));
    return gloss * pow(nl, shininess) * mainLight.color;
}

//��պв���
half3 SampleSkybox(samplerCUBE cube, float3 normal, float3 viewDir, float smooth) {
    float3 adjustNormal = float3(normal);
    adjustNormal.xz /= smooth;
    float3 refDir = reflect(-viewDir, adjustNormal);
    half4 color = texCUBE(cube, refDir);
    return color.rgb;
}


//���㷴��ϵ��
float GetReflectionCoefficient(float3 viewDir, float3 normal, float fresnelPower) {
    float a = 1 - dot(viewDir, normal);
    return pow(a, fresnelPower);
}

//������������ 
half4 SampleRefractionColor(float2 screenUV, float3 normalWS, float refractionPower, sampler2D cameraOpaqueTexture) {
    //����һ�������UV�Ŷ�����ģ������Ч��
    float2 refractionUV = screenUV + normalWS.xz * refractionPower;
    half4 color = tex2D(cameraOpaqueTexture, refractionUV);
    if (color.a > 0.1) {
        //alpha��Ϊ0��˵��UVƫ�Ʋ���������͸��������ڵ�������˷���ƫ�ƣ�ֱ����ԭUV������
        color = tex2D(cameraOpaqueTexture, screenUV);
    }
    return color;
}

#define SAMPLE_REFRACTION(screenUV,normalWS,refractionFactor) SampleRefractionColor(screenUV,normalWS,refractionFactor,_CameraOpaqueTexture)