Shader "PDX/PdxMeshAlphaBlend"
{
	Properties{
		_Diffuse("Diffuse (RGBA)", 2D) = "white" {}
		_Normal("Normalmap", 2D) = "bump" {}
		_Specular("Specular (RGB) Smoothness (A)", 2D) = "white" {}
	}

	CGINCLUDE
	//#include "UnityCG.cginc"
	#include "pdx.cginc"
	sampler2D _Diffuse;
	sampler2D _Normal;
	sampler2D _Specular;

	struct Input {
		float2 uv_Diffuse;
		float2 uv_Normal;
		float2 uv_Specular;
		//float3 worldPos;
		//float3 worldNormal; INTERNAL_DATA
	};

	void surf(Input IN, inout SurfaceOutputStandard o) {
		fixed4 tex = tex2D(_Diffuse, IN.uv_Diffuse);
		fixed4 spec = tex2D(_Specular, IN.uv_Specular);
		o.Normal = UnpackNormalPdx(_Normal, IN.uv_Normal);
		//fixed3 vColor = tex.rgb * _Color.rgb;
		o.Albedo = tex.rgb;
		//vColor = CalculateLighting(vColor, IN.worldNormal, _WorldSpaceLightPos0, 1.0, _LightColor0.xyz, 1.0);
		//o.Albedo.rgb = ComposeSpecular(vColor, CalculateSpecular(IN.worldPos, IN.worldNormal, (spec.a * 2.0), normalize(_WorldSpaceLightPos0.xyz)));
		o.Smoothness = spec.a * 4.5;
		o.Metallic = spec.a;
		o.Alpha = tex.a;
	}
	ENDCG

	SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		LOD 400
		Fog{ Mode Off }
		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		//Cull Off

		CGPROGRAM
		#pragma surface surf Standard alpha:blend nofog nolightmap
		#pragma target 3.0
		ENDCG
	}

	FallBack "Unlit/Transparent"
}
