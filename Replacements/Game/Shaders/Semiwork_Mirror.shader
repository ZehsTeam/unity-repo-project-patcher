Shader "Semiwork/Mirror"
{
	Properties
	{
		_Color ("Tint", Color) = (1,1,1,1)
		[HideInInspector] _ReflectionTex ("Reflection", 2D) = "black" {}

		[Space][Header(Surface Distortion)]
		[Normal] _BumpMap ("Surface Normal", 2D) = "bump" {}
		_DistortStrength ("Distortion Strength", Range(0,0.2)) = 0

		[Space][Header(Fresnel Sheen)]
		_FresnelColor ("Fresnel Tint", Color) = (1,1,1,1)
		_FresnelStrength ("Fresnel Strength", Range(0,1)) = 0
		_FresnelPower ("Fresnel Power", Range(1,8)) = 4

		[Space][Header(Grime)]
		_GrimeTex ("Grime (coverage in R)", 2D) = "black" {}
		_GrimeColor ("Grime Color", Color) = (0.5,0.5,0.5,1)
		_GrimeStrength ("Grime Strength", Range(0,1)) = 1
		_GrimeDistort ("Grime Adds Distortion", Range(0,1)) = 0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _ReflectionTex;
			sampler2D _BumpMap;
			float4 _BumpMap_ST;
			sampler2D _GrimeTex;
			float4 _GrimeTex_ST;
			fixed4 _Color;
			float _DistortStrength;
			fixed4 _FresnelColor;
			float _FresnelStrength;
			float _FresnelPower;
			fixed4 _GrimeColor;
			float _GrimeStrength;
			float _GrimeDistort;

			fixed4 _MirrorFogColor;
			float _MirrorDistanceFade;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				float2 uv : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;
			};

			v2f vert(appdata v)
			{
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.pos);
				o.uv = v.uv;

				float3 _worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.viewDir = UnityWorldSpaceViewDir(_worldPos);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 _bumpUV = TRANSFORM_TEX(i.uv, _BumpMap);
				float2 _grimeUV = TRANSFORM_TEX(i.uv, _GrimeTex);

				float3 _bump = UnpackNormal(tex2D(_BumpMap, _bumpUV));
				float _coverage = tex2D(_GrimeTex, _grimeUV).r * _GrimeStrength;

				float2 _screenUV = i.screenPos.xy / i.screenPos.w;
				_screenUV.x = 1.0 - _screenUV.x;
				_screenUV += _bump.xy * (_DistortStrength + _coverage * _GrimeDistort * 0.1);

				fixed3 _reflection = tex2D(_ReflectionTex, _screenUV).rgb;
				_reflection *= _Color.rgb;

				_reflection = lerp(_reflection, _MirrorFogColor.rgb, _MirrorDistanceFade);

				fixed3 _result = lerp(_reflection, _GrimeColor.rgb, _coverage);

				float _fresnel = pow(saturate(1.0 - dot(normalize(i.viewDir), normalize(i.worldNormal))), _FresnelPower);
				_result = lerp(_result, _FresnelColor.rgb, _fresnel * _FresnelStrength);

				return fixed4(_result, 1);
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
