Shader "PDX/PdxMeshTextureAtlas" {
	Properties{
		_Diffuse("Diffuse (RGBA)", 2D) = "white" {}
		_Normal("Normalmap", 2D) = "bump" {}
		_Specular("Specular (RGB) Smoothness (A)", 2D) = "white" {}
		_Atlas("Atlas", 2D) = "white" {}
		_AtlasCutoff("AtlasCutoff", Range(0, 1)) = 1
		_AtlasHalfColor("AtlasHalfColor", Color) = (1,1,1,1)
		_TextureAtlasCoords("TextureAtlasCoords", Color) = (1,1,1,1)
		_PrimaryColor("PrimaryColor", Color) = (1,1,1,1)
		_SecondaryColor("SecondaryColor", Color) = (1,1,1,1)
		_TertiaryColor("TertiaryColor", Color) = (1,1,1,1)
	}

	CGINCLUDE
	//#include "UnityCG.cginc"
	#include "pdx.cginc"
	sampler2D _Diffuse;
	sampler2D _Normal;
	sampler2D _Specular;
	sampler2D _Atlas;
	float _AtlasCutoff;
	float4 _AtlasHalfColor;
	float4 _PrimaryColor;
	float4 _SecondaryColor;
	float4 _TertiaryColor;
	float4 _TextureAtlasCoords;

	struct Input {
		float2 uv_Diffuse;
		float2 uv_Normal;
		float2 uv_Specular;
		float2 uv2_Atlas;
	};

	void surf(Input IN, inout SurfaceOutputStandard o) {
		fixed4 tex = tex2D(_Diffuse, IN.uv_Diffuse);
		fixed4 spec = tex2D(_Specular, IN.uv_Specular);
		//fixed4 atlas = tex2D(_Atlas, IN.uv2_Atlas);

		float4 atlasColor = float4(1,1,1,1);
		if (_AtlasHalfColor.a > 0.0f && IN.uv2_Atlas.x > 0.5f)
		{
			atlasColor = _AtlasHalfColor;
		}
		else if (_AtlasCutoff >= 0.0f)
		{
			float2 vActualUV = float2(IN.uv2_Atlas.x / _TextureAtlasCoords.x + _TextureAtlasCoords.z, (1.0 - IN.uv2_Atlas.y) / _TextureAtlasCoords.y + _TextureAtlasCoords.w);
			atlasColor = tex2D(_Atlas, vActualUV);
		}
		else
		{
			float2 vActualUV = float2(IN.uv2_Atlas.x / _TextureAtlasCoords.x + _TextureAtlasCoords.z, (1.0 - IN.uv2_Atlas.y) / _TextureAtlasCoords.y + _TextureAtlasCoords.w);

			float4 OutColor = tex2D(_Atlas, vActualUV);
			OutColor = OutColor.r * _PrimaryColor + OutColor.g * _SecondaryColor + OutColor.b * _TertiaryColor;

			atlasColor = OutColor;
		}

		o.Normal = UnpackNormalPdx(_Normal, IN.uv_Normal);
		o.Albedo = lerp(tex.rgb, atlasColor.rgb * tex.rgb, tex.a);
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
