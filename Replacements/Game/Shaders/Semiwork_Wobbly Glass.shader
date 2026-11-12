Shader "Semiwork/Wobbly Glass"
{
	Properties
	{
		[Header(Albedo)][NoScaleOffset][SingleLineTexture] _AlbedoTexture("Albedo Texture", 2D) = "white" {}
		_AlbedoColor("Albedo Color", Color) = (1,1,1,1)
		_TextureBlend("Texture Blend", Range(0,1)) = 0

		[Header(Normal)][NoScaleOffset][Normal][SingleLineTexture] _NormalTexture("Normal Texture", 2D) = "bump" {}
		_NormalStrength("Normal Strength", Range(-5,5)) = 1

		[Header(Glass)] _Transparency("Transparency", Range(0,1)) = 0.8
		_Thickness("Thickness", Range(0,1)) = 0.25

		[Header(Grime)][Toggle(_GRIME_ON)] _GrimeOn("Grime", Float) = 0
		[NoScaleOffset][SingleLineTexture] _GrimeTexture("Grime Texture (mask in R)", 2D) = "black" {}
		_GrimeColor("Grime Color", Color) = (0.25,0.22,0.18,1)
		_GrimeAmount("Grime Amount", Range(0,1)) = 0.5

		[Header(Tiling)] _TilingX("Tiling X", Float) = 1
		_TilingY("Tiling Y", Float) = 1

		[Header(Offset)] _OffsetX("Offset X", Float) = 0
		_OffsetY("Offset Y", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"ForceNoShadowCasting" = "True"
			"PreviewType" = "Plane"
		}

		Cull Back
		ZWrite Off
		ColorMask RGB

		GrabPass { "_SemiworkGlassGrabTexture" }

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _GRIME_ON
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma instancing_options nolightprobe nolightmap nolodfade

			#include "UnityCG.cginc"

			sampler2D _SemiworkGlassGrabTexture;
			float4 _SemiworkGlassGrabTexture_TexelSize;
			sampler2D _AlbedoTexture;
			half4 _AlbedoColor;
			half _TextureBlend;
			sampler2D _NormalTexture;
			half _NormalStrength;
			half _Transparency;
			half _Thickness;
			sampler2D _GrimeTexture;
			half4 _GrimeColor;
			half _GrimeAmount;
			half _TilingX;
			half _TilingY;
			half _OffsetX;
			half _OffsetY;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 grabPosition : TEXCOORD0;
				float2 uv : TEXCOORD1;
				half4 viewData : TEXCOORD2;
				UNITY_FOG_COORDS(3)
			};

			v2f vert(appdata v)
			{
				v2f _output;
				UNITY_SETUP_INSTANCE_ID(v);
				float3 _worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
				half3 _worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
				half3 _worldViewDirection = normalize(UnityWorldSpaceViewDir(_worldPosition));
				half3 _viewDirection = mul((half3x3)UNITY_MATRIX_V, _worldViewDirection);

				_output.vertex = UnityObjectToClipPos(v.vertex);
				_output.grabPosition = ComputeGrabScreenPos(_output.vertex);
				_output.uv = v.uv * float2(_TilingX, _TilingY) + float2(_OffsetX, _OffsetY);
				_output.viewData = half4(_viewDirection.xy, saturate(dot(_worldNormal, _worldViewDirection)), _ScreenParams.y / _ScreenParams.x);
				UNITY_TRANSFER_FOG(_output, _output.vertex);
				return _output;
			}

			half4 frag(v2f i) : SV_Target
			{
				half3 _normal = UnpackNormal(tex2D(_NormalTexture, i.uv));
				half _viewDepth = 1.0h - i.viewData.z;
				half _thicknessDepth = _Thickness * (0.5h + _viewDepth * 2.0h);
				half3 _fogVisibilityColor = 1.0h;
				UNITY_APPLY_FOG_COLOR(i.fogCoord, _fogVisibilityColor, half4(0.0h, 0.0h, 0.0h, 0.0h));
				half _fogVisibility = _fogVisibilityColor.r;

				float2 _screenScale = float2(i.viewData.w, 1.0);
				float2 _screenUV = i.grabPosition.xy / i.grabPosition.w;
				float2 _normalOffset = _normal.xy * (_NormalStrength * 0.0125h) * (1.0h + _thicknessDepth) * _fogVisibility;
				float2 _thicknessOffset = i.viewData.xy * (_Thickness * 0.0075h) * _fogVisibility;
				float2 _screenEdge = abs(_SemiworkGlassGrabTexture_TexelSize.xy) * 0.5;

				_screenUV += (_normalOffset + _thicknessOffset) * _screenScale;
				_screenUV = clamp(_screenUV, _screenEdge, 1.0 - _screenEdge);

				half3 _sceneColor = tex2D(_SemiworkGlassGrabTexture, _screenUV).rgb;
				half4 _albedo = tex2D(_AlbedoTexture, i.uv) * _AlbedoColor;
				half3 _texturedScene = lerp(_sceneColor, _sceneColor * _albedo.rgb, _TextureBlend * _fogVisibility);
				half _surfaceAmount = (1.0h - _Transparency) * _albedo.a * _fogVisibility;
				half3 _result = lerp(_texturedScene, _albedo.rgb, _surfaceAmount);

				#if defined(_GRIME_ON)
				half _grimeMask = saturate(tex2D(_GrimeTexture, i.uv).r * _GrimeAmount) * _fogVisibility;
				_result = lerp(_result, _GrimeColor.rgb, _grimeMask * _GrimeColor.a);
				#endif

				return half4(_result, 1.0h);
			}
			ENDCG
		}
	}

	Fallback Off
}
