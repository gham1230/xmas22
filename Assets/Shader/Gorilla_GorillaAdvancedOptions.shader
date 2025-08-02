Shader "Gorilla/GorillaAdvancedOptions" {
	Properties {
		[NoScaleOffset] _MainTex ("MainTex", 2D) = "white" {}
		[Enum(None,0,Alpha,1,Red,8,Green,4,Blue,2,RGB,14,RGBA,15)] _ColorMask ("Color Mask", Float) = 15
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2
		[IntRange] _StencilReference ("Stencil Reference", Range(0, 255)) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComparison ("Stencil Comparison", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilPassFront ("Stencil Pass Front", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilFailFront ("Stencil Fail Front", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilZFailFront ("Stencil Z Fail Front", Float) = 0
		_StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 255
		_StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 255
		[Enum(Off,0,On,1)] _ZWriteMode ("Z Write Mode", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("Z Test Mode", Float) = 4
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSource ("Blend Mode Source", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDestination ("Blend Mode Destination", Float) = 0
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}