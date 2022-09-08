#include"IBL.cginc"

#define PI 3.14159265359


float3 FreshnelSchlick(float3 F0, float3 V, float3 H) {
	float CosTheta = max(dot(V, H), 0);
	return F0 + (1.0 - F0) * pow(CosTheta, 5);

}

float DistributionGGX(float3 N, float3 H, float roughness) {
	float alpha = roughness * roughness;
	float alpha_2 = alpha * alpha;
	float NdotH = max(dot(N, H),0);
	return alpha_2 / PI / pow(NdotH * NdotH * (alpha_2 - 1.0) + 1.0, 2.0);
}

float GeometrySchlickGGX(float NdotV, float k)
{
	// TODO: To calculate Schlick G1 here

	return NdotV / (NdotV * (1.0 - k) + k);
}
float GeometrySmith(float3 N, float3 V, float3 L, float roughness) {
	float k = pow(roughness + 1.0, 2) / 8.0;
	float NdotV = max(dot(N, V), 0.0);
	float NdotL = max(dot(N, L), 0.0);
	return GeometrySchlickGGX(NdotV, k) * GeometrySchlickGGX(NdotL, k);

}

float3 CookTorranceBRDF(float3 N, float3 V, float3 L, float3 albedo, float3 radiance, float roughness, float metallic) {

	// V and L are both shooted from the object
	//make  sure that both N,V and L are normalized;
	float3 H = normalize(L + V);
	float NdotL = max(dot(N, L), 0.0);
	float NdotV = max(dot(N, V), 0.0);

	float G = GeometrySmith(N, V, L, roughness);
	float NDF = DistributionGGX(N, H, roughness);

	float3 F0 = float3(0.04,0.04,0.04);
	F0 = lerp(F0, albedo, metallic);
	float3 F = FreshnelSchlick(F0, V, H);



	float3 numerator = NDF * G * F;
	float denominator = max((4.0 * NdotL * NdotV), 0.001);
	float3 BRDF = numerator / denominator;


	float3 specular = BRDF  * PI;
	float3 diffuse = (1.0 - F) * (1.f - metallic);
	//return albedo;
	return  (specular + diffuse * albedo) * radiance * NdotL;

}