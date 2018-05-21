Shader "PDX/PdxMeshSnow" {
	Properties{
		_Diffuse("Diffuse (RGBA)", 2D) = "white" {}
		_Normal("Normalmap", 2D) = "bump" {}
		_Specular("Specular (RGB) Smoothness (A)", 2D) = "white" {}
		_Snow("Snow",  Range(0, 1.0)) = 0.0
	}

	CGINCLUDE
	//#include "UnityCG.cginc"
	#include "pdx.cginc"
	sampler2D _Diffuse;
	sampler2D _Normal;
	sampler2D _Specular;
	float _Snow;

	struct Input {
		float2 uv_Diffuse;
		float2 uv_Normal;
		float2 uv_Specular;
	};

	void surf(Input IN, inout SurfaceOutputStandard o) {
		fixed4 tex = tex2D(_Diffuse, IN.uv_Diffuse);
		fixed4 spec = tex2D(_Specular, IN.uv_Specular);
		o.Normal = UnpackNormalPdx(_Normal, IN.uv_Normal);
		o.Albedo = lerp(tex.rgb, fixed3(1,1,1), _Snow);
		o.Smoothness = spec.a * 4.5;
		o.Metallic = spec.a;
		o.Alpha = 1.0;
	}

	ENDCG

		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 400
		Fog{ Mode Off }
		//Cull Off

		CGPROGRAM
		#pragma surface surf Standard nofog nolightmap
		#pragma target 3.0
		ENDCG
	}

	FallBack "Standard (Specular setup)"
}