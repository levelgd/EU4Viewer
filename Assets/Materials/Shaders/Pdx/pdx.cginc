float3 UnpackNormalPdx(in sampler2D NormalTex, float2 uv)
{
	float3 vNormalSample = normalize(tex2D(NormalTex, uv).rgb - 0.5f);
	vNormalSample.g = -vNormalSample.g;
	return vNormalSample;
}

float CalculateSpecular(float3 vPos, float3 vNormal, float vInIntensity, float3 vLightDir)
{
	float3 H = normalize(-normalize(vPos - _WorldSpaceCameraPos) + -vLightDir);
	float vSpecWidth = 10.0f;
	float vSpecMultiplier = 2.0f;
	return saturate(pow(saturate(dot(H, vNormal)), vSpecWidth) * vSpecMultiplier) * vInIntensity;
}

float3 CalculateSpecular(float3 vPos, float3 vNormal, float3 vInIntensity, float3 vLightDir)
{
	float3 H = normalize(-normalize(vPos - _WorldSpaceCameraPos) + -vLightDir);
	float vSpecWidth = 10.0f;
	float vSpecMultiplier = 2.0f;
	return saturate(pow(saturate(dot(H, vNormal)), vSpecWidth) * vSpecMultiplier) * vInIntensity;
}

float3 ComposeSpecular(float3 vColor, float vSpecular)
{
	return saturate(vColor + vSpecular);// * STANDARD_HDR_RANGE + ( 1.0f - STANDARD_HDR_RANGE ) * vSpecular;
}

float3 ComposeSpecular(float3 vColor, float3 vSpecular)
{
	return saturate(vColor + vSpecular);
}

float3 CalculateLighting(float3 vColor, float3 vNormal, float3 vLightDirection, float vAmbient, float3 vLightDiffuse, float vLightIntensity)
{
	float NdotL = dot(vNormal, -vLightDirection);

	float vHalfLambert = NdotL * 0.5f + 0.5f;
	vHalfLambert *= vHalfLambert;

	vHalfLambert = vAmbient + (1.0f - vAmbient) * vHalfLambert;

	return  saturate(vHalfLambert * vColor * vLightDiffuse * vLightIntensity);
}