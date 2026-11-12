// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Water"
{
	Properties
	{
		_DiscortScale("DiscortScale", Float) = 1
		_Scale("Scale", Float) = 1
		[SingleLineTexture]_Distort("Distort", 2D) = "white" {}
		[SingleLineTexture]_Tex("Tex", 2D) = "white" {}
		_DistortScroll("DistortScroll", Vector) = (0,0,0,0)
		_Scroll("Scroll", Vector) = (0,0,0,0)
		_DistortStr("DistortStr", Float) = 0
		[HDR]_Color1("Color 1", Color) = (0,0,0,0)
		[HDR]_Color2("Color 2", Color) = (1,1,1,0)
		[HDR]_EdgeColor("EdgeColor", Color) = (0,0,0,0)
		_EdgeWidth("EdgeWidth", Float) = 0.2
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma surface surf Standard alpha:fade keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float3 worldPos;
			float4 screenPos;
		};

		uniform float4 _Color1;
		uniform float4 _Color2;
		uniform sampler2D _Tex;
		uniform sampler2D _Distort;
		uniform float _DiscortScale;
		uniform float2 _DistortScroll;
		uniform float _DistortStr;
		uniform float _Scale;
		uniform float2 _Scroll;
		uniform float4 _EdgeColor;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _EdgeWidth;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_worldPos = i.worldPos;
			float3 worldToObj41 = mul( unity_WorldToObject, float4( ase_worldPos, 1 ) ).xyz;
			float3 WP42 = worldToObj41;
			float3 break44 = WP42;
			float2 appendResult2 = (float2(break44.x , break44.z));
			float3 break45 = WP42;
			float2 appendResult12 = (float2(break45.x , break45.z));
			float Tg33 = tex2D( _Tex, ( ( tex2D( _Distort, ( ( appendResult2 * _DiscortScale ) + ( _Time.y * _DistortScroll ) ) ).r * _DistortStr ) + ( ( appendResult12 * _Scale ) + ( _Time.y * _Scroll ) ) ) ).r;
			float4 lerpResult22 = lerp( _Color1 , _Color2 , Tg33);
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float temp_output_32_0 = ( _EdgeWidth + ( _EdgeWidth * Tg33 ) );
			float screenDepth26 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth26 = abs( ( screenDepth26 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( temp_output_32_0 ) );
			float4 lerpResult29 = lerp( lerpResult22 , ( _EdgeColor * float4( 0,0,0,0 ) ) , ( ( 1.0 - saturate( distanceDepth26 ) ) * 1.0 ));
			o.Albedo = lerpResult29.rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.WorldPosInputsNode;1;-4256,112;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformPositionNode;41;-4016,112;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;42;-3760,112;Inherit;False;WP;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;43;-3360,-64;Inherit;False;42;WP;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode;44;-3152,-64;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleTimeNode;7;-2976,352;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;9;-2975.405,477.9675;Inherit;False;Property;_DistortScroll;DistortScroll;4;0;Create;True;0;0;0;False;0;False;0,0;0.02,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;4;-2928,112;Inherit;False;Property;_DiscortScale;DiscortScale;0;0;Create;True;0;0;0;False;0;False;1;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;2;-2896,-48;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;46;-2688,816;Inherit;False;42;WP;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-2720,16;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-2768,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;45;-2480,816;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;6;-2368,112;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;12;-2304,832;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;14;-2384,1232;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;15;-2384,1360;Inherit;False;Property;_Scroll;Scroll;5;0;Create;True;0;0;0;False;0;False;0,0;-0.015,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;13;-2288,976;Inherit;False;Property;_Scale;Scale;1;0;Create;True;0;0;0;False;0;False;1;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-2160,112;Inherit;True;Property;_Distort;Distort;2;1;[SingleLineTexture];Create;True;0;0;0;False;0;False;-1;None;4875605623d30174bad4543cc502c642;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-2128,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-2176,1264;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1968,368;Inherit;False;Property;_DistortStr;DistortStr;6;0;Create;True;0;0;0;False;0;False;0;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-1776,992;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-1808,224;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;19;-1584,272;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;10;-1282.496,121.8963;Inherit;True;Property;_Tex;Tex;3;1;[SingleLineTexture];Create;True;0;0;0;False;0;False;-1;None;4875605623d30174bad4543cc502c642;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;33;-934.8259,238.4571;Inherit;False;Tg;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;34;-992,896;Inherit;False;33;Tg;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-992,656;Inherit;False;Property;_EdgeWidth;EdgeWidth;10;0;Create;True;0;0;0;False;0;False;0.2;0.39;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-752,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;32;-416,672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;26;-240,496;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;38;32,512;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;24;-996.3542,-202.417;Inherit;False;Property;_Color2;Color 2;8;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;0.03150587,0.1132075,0.1057801,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;23;-992,-448;Inherit;False;Property;_Color1;Color 1;7;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0.1688768,0.2169811,0.1766772,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;39;192,432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;30;-110.3645,165.8394;Inherit;False;Property;_EdgeColor;EdgeColor;9;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0.07769669,0.1698113,0.09085591,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;22;-576,-96;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;304,320;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;160,160;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;29;400,112;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;752,384;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;27;0,752;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;992,96;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Water;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;41;0;1;0
WireConnection;42;0;41;0
WireConnection;44;0;43;0
WireConnection;2;0;44;0
WireConnection;2;1;44;2
WireConnection;3;0;2;0
WireConnection;3;1;4;0
WireConnection;8;0;7;0
WireConnection;8;1;9;0
WireConnection;45;0;46;0
WireConnection;6;0;3;0
WireConnection;6;1;8;0
WireConnection;12;0;45;0
WireConnection;12;1;45;2
WireConnection;5;1;6;0
WireConnection;16;0;12;0
WireConnection;16;1;13;0
WireConnection;17;0;14;0
WireConnection;17;1;15;0
WireConnection;18;0;16;0
WireConnection;18;1;17;0
WireConnection;20;0;5;1
WireConnection;20;1;21;0
WireConnection;19;0;20;0
WireConnection;19;1;18;0
WireConnection;10;1;19;0
WireConnection;33;0;10;1
WireConnection;35;0;31;0
WireConnection;35;1;34;0
WireConnection;32;0;31;0
WireConnection;32;1;35;0
WireConnection;26;0;32;0
WireConnection;38;0;26;0
WireConnection;39;0;38;0
WireConnection;22;0;23;0
WireConnection;22;1;24;0
WireConnection;22;2;33;0
WireConnection;36;0;39;0
WireConnection;47;0;30;0
WireConnection;29;0;22;0
WireConnection;29;1;47;0
WireConnection;29;2;36;0
WireConnection;27;0;26;0
WireConnection;27;1;32;0
WireConnection;0;0;29;0
ASEEND*/
//CHKSM=9404A898F00151A8093F4AA7B557DF4BAF5B3B3F