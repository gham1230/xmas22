Shader "Gorilla/Halloween22/HornsSlingshotProjectile" {
	Properties {
		[SingleLineTexture] [NoScaleOffset] _Masks ("Masks", 2D) = "white" {}
		[Vec2] _SpeedRG ("SpeedRG", Vector) = (-0.2,-0.2,0,0)
		[Vec2] _ScaleRG ("ScaleRG", Vector) = (0.08,0.08,0,0)
		_Color ("Color", Vector) = (1,1,1,1)
		[ASEEnd] [Toggle(_VERTEX_DEFORMATION)] _VERTEX_DEFORMATION ("_VERTEX_DEFORMATION", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
}