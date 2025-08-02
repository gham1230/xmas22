Shader "Tazo/BubbleStandard" {
	Properties {
		[Header(Base)] _BaseTex3 ("HightLight(R)", 2D) = "black" {}
		_pow ("POW", Range(0, 20)) = 1
		[Header(Mat)] _MColor ("Mat Color", Vector) = (0.5,0.5,0.5,1)
		[NoScaleOffset] _MatCap ("MatCap which has alpha (RGBA)", 2D) = "white" {}
		_powh ("HightLight POW", Range(0, 20)) = 1
		[Header(Rim)] _RimColor ("Rim Color", Vector) = (1,1,1,1)
		rimWidth ("rimWidth", Range(0, 2)) = 0.75
		_AlphaMode ("Alpha", Range(0, 1)) = 0
		[Header(Project)] [NoScaleOffset] _Project ("Project(RGB)", 2D) = "white" {}
		_tile ("Tile", Range(0, 20)) = 1
		_tileOffsetX ("OffsetX", Range(0, 1)) = 0
		_tileOffsetY ("OffseeY", Range(0, 1)) = 0
		[Header(Distortion)] [NoScaleOffset] _ProjectUV ("UV Dis(R)", 2D) = "black" {}
		_tileUV ("UV Dis Tile", Range(0, 20)) = 1
		_flow_offset ("flow_offset", Range(-10, 10)) = 0
		_flow_strength ("flow_strength", Range(-10, 10)) = 0.5
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
		}
		ENDCG
	}
	Fallback "VertexLit"
}