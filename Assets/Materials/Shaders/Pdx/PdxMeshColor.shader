Shader "PDX/PdxMeshColor" {
	Properties{
		_Diffuse("Diffuse (RGBA)", 2D) = "white" {}
		_Normal("Normalmap", 2D) = "bump" {}
		_Specular("Specular (RGB) Smoothness (A)", 2D) = "white" {}
		_PrimaryColor("PrimaryColor", Color) = (1,1,1,1)
		_SecondaryColor("SecondaryColor", Color) = (1,1,1,1)
		_TertiaryColor("TertiaryColor", Color) = (1,1,1,1)
		_AtlasHalfColor("AtlasHalfColor", Color) = (1,1,1,1)
	}

	CGINCLUDE
	//#include "UnityCG.cginc"
	#include "pdx.cginc"
	sampler2D _Diffuse;
	sampler2D _Normal;
	sampler2D _Specular;
	float4 _PrimaryColor;
	float4 _SecondaryColor;
	float4 _TertiaryColor;
	float4 _AtlasHalfColor;

	struct Input {
		float2 uv_Diffuse;
		float2 uv_Normal;
		float2 uv_Specular;
	};

	void surf(Input IN, inout SurfaceOutputStandard o) {

		fixed4 tex = tex2D(_Diffuse, IN.uv_Diffuse);
		fixed4 spec = tex2D(_Specular, IN.uv_Specular);

		tex.rgb = lerp(tex.rgb, tex.rgb * (spec.r * _PrimaryColor.rgb), spec.r);
		tex.rgb = lerp(tex.rgb, tex.rgb * (spec.g * _SecondaryColor.rgb), spec.g);
		if (_AtlasHalfColor.a > 0.0f)
		{
			tex.rgb = lerp(tex.rgb, tex.rgb * (spec.b * _AtlasHalfColor.rgb), spec.b);
		}
		else
		{
			tex.rgb = lerp(tex.rgb, tex.rgb * (spec.b * _TertiaryColor.rgb), spec.b);
		}

		o.Normal = UnpackNormalPdx(_Normal, IN.uv_Normal);
		o.Albedo = tex.rgb;
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
