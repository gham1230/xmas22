Shader "Gorilla/Halloween22/HornsSlingshotElastic" {
	Properties {
		_TintColor ("Tint Color", Vector) = (0.5,0.5,0.5,0.5)
		[SingleLineTexture] [NoScaleOffset] _Masks ("Masks", 2D) = "white" {}
		_GradientStop1 ("GradientStop1", Float) = 0.98
		_GradientStop2 ("GradientStop2", Float) = 0.9
		_GradientStop3 ("GradientStop3", Float) = 0.6
		_Influences ("Influences", Vector) = (1,1,1,1)
		[Vec2] _SpeedR ("SpeedR", Vector) = (-0.1,-0.1,0,0)
		[Vec2] _SpeedG ("SpeedG", Vector) = (-0.5,-0.2,0,0)
		[Vec2] _SpeedB ("SpeedB", Vector) = (-0.2,-0.1,0,0)
		[Vec2] _SpeedA ("SpeedA", Vector) = (-2,-0.1,0,0)
		_TimeScale ("TimeScale", Float) = 1
		[Vec2] _ScaleOffsetB ("ScaleOffsetB", Vector) = (0,0,0,0)
		[Vec2] _ScaleOffsetG ("ScaleOffsetG", Vector) = (3,1,0,0)
		[Vec2] _ScaleOffsetR ("ScaleOffsetR", Vector) = (4,1,0,0)
		[Toggle(_FADE_START_EDGE)] _FADE_START_EDGE ("Fade Start Edge", Float) = 1
		[ASEEnd] [Toggle(_FADE_END_EDGE)] _FADE_END_EDGE ("Fade End Edge", Float) = 0
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
}