// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_VFX_Vefects_Distortion_Flat_01_BIRP"
{
	Properties
	{
		[Space(13)][Header(Ge Lush was here)][Space(13)] _DistortionOverallIntensity( "Distortion Overall Intensity", Float ) = 1
		[Space(33)][Header(Color)][Space(13)] _ColorTint( "Color Tint", Color ) = ( 1, 1, 1, 0 )
		_ColorEmission( "Color Emission", Float ) = 1
		[Space(33)][Header(Distortion Noise)][Space(13)] _DistortionNoise( "Distortion Noise", 2D ) = "white" {}
		_DistortionNoiseSelector( "Distortion Noise Selector", Vector ) = ( 0, 1, 0, 0 )
		_DistUVS( "Dist UV S", Vector ) = ( 1, 1, 0, 0 )
		_DistUVP( "Dist UV P", Vector ) = ( 0, 0, 0, 0 )
		_DistortionErosion( "Distortion Erosion", Float ) = 0
		_DistortionErosionSmoothness( "Distortion Erosion Smoothness", Float ) = 1
		[Space(33)][Header(Distortion Dist Noise)][Space(13)] _DistortionDist( "Distortion Dist", 2D ) = "white" {}
		_DistortionDistSelector( "Distortion Dist Selector", Vector ) = ( 0, 1, 0, 0 )
		_DistDistUVS( "Dist Dist UV S", Vector ) = ( 1, 1, 0, 0 )
		_DistDistUVP( "Dist Dist UV P", Vector ) = ( 0, 0, 0, 0 )
		_DistortionDistLerp( "Distortion Dist Lerp", Float ) = 0.1
		[Space(33)][Header(Cutout)][Space(13)] _CutoutTexture( "Cutout Texture", 2D ) = "white" {}
		_CutoutMaskSelector( "Cutout Mask Selector", Vector ) = ( 0, 1, 0, 0 )
		_CutoutErosion( "Cutout Erosion", Float ) = 0
		_CutoutErosionSmoothness( "Cutout Erosion Smoothness", Float ) = 0.05
		_CutoutRotation( "Cutout Rotation", Float ) = 0
		_CutoutOffset( "Cutout Offset", Vector ) = ( 0, 0, 0, 0 )
		_FinalOpacityErosion( "Final Opacity Erosion", Float ) = 0
		_FinalOpacityErosionSmoothness( "Final Opacity Erosion Smoothness", Float ) = 0.05
		[Space(33)][Header(Depth Fade)][Space(13)] _DepthFade( "Depth Fade", Float ) = 1
		[Space(33)][Header(Camera Depth Fade)][Space(13)] _CameraDepthFadeLength( "Camera Depth Fade Length", Float ) = 1
		_CameraDepthFadeOffset( "Camera Depth Fade Offset", Float ) = 0
		[Space(33)][Header(Camera Push)][Space(13)] _CameraPush( "Camera Push", Float ) = 0
		[Space(33)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 0
		_Src( "Src", Float ) = 5
		_Dst( "Dst", Float ) = 10
		_ZWrite( "ZWrite", Float ) = 0
		_ZTest( "ZTest", Float ) = 2

	}

	SubShader
	{
		

		

		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

	LOD 0

		ZWrite [_ZWrite]
		Cull [_Cull]
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add

		

		Blend [_Src] [_Dst], One Zero
		BlendOp Add, Add
		

		CGINCLUDE
			#pragma target 3.5
			// ensure rendering platforms toggle list is visible

			float4 ComputeClipSpacePosition( float2 screenPosNorm, float deviceDepth )
			{
				float4 positionCS = float4( screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0 );
			#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
			#endif
				return positionCS;
			}
		ENDCG

		GrabPass{ }

		Pass
		{
			
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }

			Cull [_Cull]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			BlendOp Add, Add

			

			CGPROGRAM
				#define ASE_SURFACE_TRANSPARENT
				#define ASE_VERSION 19912
				#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
				#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
				#else
				#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
				#endif

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				#include "UnityStandardBRDF.cginc"
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_VERT_POSITION
				#define ASE_NEEDS_FRAG_SCREEN_POSITION
				#define ASE_NEEDS_TEXTURE_COORDINATES0
				#define ASE_NEEDS_TEXTURE_COORDINATES1
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
				#define ASE_NEEDS_FRAG_COLOR


				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS float4 pos : SV_POSITION;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform float _Cull;
				uniform float _Src;
				uniform float _Dst;
				uniform float _ZWrite;
				uniform float _ZTest;
				uniform float _CameraPush;
				ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
				uniform float _DistortionErosion;
				uniform float _DistortionErosionSmoothness;
				uniform sampler2D _DistortionNoise;
				uniform float2 _DistUVP;
				uniform float2 _DistUVS;
				uniform sampler2D _DistortionDist;
				uniform float2 _DistDistUVP;
				uniform float2 _DistDistUVS;
				uniform float4 _DistortionDistSelector;
				uniform float _DistortionDistLerp;
				uniform float4 _DistortionNoiseSelector;
				uniform float _DistortionOverallIntensity;
				uniform float _CutoutErosion;
				uniform float _CutoutErosionSmoothness;
				uniform sampler2D _CutoutTexture;
				uniform float2 _CutoutOffset;
				uniform float _CutoutRotation;
				uniform float4 _CutoutMaskSelector;
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				uniform float4 _CameraDepthTexture_TexelSize;
				uniform float _DepthFade;
				uniform float _CameraDepthFadeLength;
				uniform float _CameraDepthFadeOffset;
				uniform float4 _ColorTint;
				uniform float _ColorEmission;
				uniform float _FinalOpacityErosion;
				uniform float _FinalOpacityErosionSmoothness;


				float3 ASESafeNormalize(float3 inVec)
				{
					float dp3 = max(1.175494351e-38, dot(inVec, inVec));
					return inVec* rsqrt(dp3);
				}
				
				inline float4 ASE_ComputeGrabScreenPos( float4 pos )
				{
					#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
					#else
					float scale = 1.0;
					#endif
					float4 o = pos;
					o.y = pos.w * 0.5f;
					o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
					return o;
				}
				

				v2f vert( appdata v  )
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
					float3 ase_viewVectorOS = mul( ( float3x3 )unity_WorldToObject, ( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - ase_positionWS : UNITY_MATRIX_V[ 2 ].xyz ) );
					float3 ase_viewDirSafeOS = Unity_SafeNormalize( ase_viewVectorOS );
					float3 normalizeResult138 = ASESafeNormalize( ase_viewDirSafeOS );
					float3 vertexPos143 = v.vertex.xyz;
					float4 ase_positionCS143 = UnityObjectToClipPos( vertexPos143 );
					float depthLinearEye143 = LinearEyeDepth( ase_positionCS143.z / ase_positionCS143.w );
					float3 WPO141 = ( normalizeResult138 * min( ( depthLinearEye143 - 0.05 ), _CameraPush ) );
					
					float3 objectToViewPos = UnityObjectToViewPos( v.vertex.xyz );
					float eyeDepth = -objectToViewPos.z;
					o.ase_texcoord2.x = eyeDepth;
					
					o.ase_texcoord = v.ase_texcoord;
					o.ase_texcoord1 = v.ase_texcoord1;
					o.ase_color = v.ase_color;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					o.ase_texcoord2.yzw = 0;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = WPO141;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = v.normal;
					v.tangent = v.tangent;

					o.pos = UnityObjectToClipPos( v.vertex );

					#if defined( ASE_SHADOWS )
						UNITY_TRANSFER_SHADOW( o, v.texcoord );
					#endif
					return o;
				}

				half4 frag( v2f IN 
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
				) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					float4 ScreenPos = ComputeScreenPos( ClipPos );

					float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
					float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
					float2 appendResult18 = (float2(ase_grabScreenPosNorm.r , ase_grabScreenPosNorm.g));
					float2 texCoord32 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float CSV_Rand_Sca114 = IN.ase_texcoord1.y;
					float2 panner35 = ( 1.0 * _Time.y * _DistUVP + ( texCoord32 * ( _DistUVS * CSV_Rand_Sca114 ) ));
					float CSV_Rand_Off113 = IN.ase_texcoord1.x;
					float2 texCoord48 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner50 = ( 1.0 * _Time.y * _DistDistUVP + ( texCoord48 * _DistDistUVS ));
					float dotResult55 = dot( tex2D( _DistortionDist, panner50 ) , _DistortionDistSelector );
					float2 temp_cast_1 = (saturate( dotResult55 )).xx;
					float2 lerpResult59 = lerp( float2( 0,0 ) , temp_cast_1 , _DistortionDistLerp);
					float dotResult37 = dot( tex2D( _DistortionNoise, ( ( panner35 + CSV_Rand_Off113 ) + lerpResult59 ) ) , _DistortionNoiseSelector );
					float smoothstepResult124 = smoothstep( _DistortionErosion , ( _DistortionErosion + _DistortionErosionSmoothness ) , saturate( dotResult37 ));
					float Out_Mask82 = saturate( smoothstepResult124 );
					float2 temp_cast_3 = (Out_Mask82).xx;
					float CVS_Intensity110 = IN.ase_texcoord.z;
					float2 texCoord64 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float cos69 = cos( radians( _CutoutRotation ) );
					float sin69 = sin( radians( _CutoutRotation ) );
					float2 rotator69 = mul( ( texCoord64 + _CutoutOffset ) - float2( 0.5,0.5 ) , float2x2( cos69 , -sin69 , sin69 , cos69 )) + float2( 0.5,0.5 );
					float dotResult41 = dot( tex2D( _CutoutTexture, rotator69 ) , _CutoutMaskSelector );
					float smoothstepResult73 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , saturate( dotResult41 ));
					float screenDepth94 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ScreenPosNorm.xy ));
					float distanceDepth94 = saturate( ( screenDepth94 - LinearEyeDepth( ScreenPosNorm.z ) ) / ( _DepthFade ) );
					float Out_DF96 = saturate( distanceDepth94 );
					float eyeDepth = IN.ase_texcoord2.x;
					float cameraDepthFade98 = (( eyeDepth -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
					float Out_Cam_DF100 = saturate( cameraDepthFade98 );
					float Out_Cutout84 = saturate( ( saturate( ( saturate( smoothstepResult73 ) * Out_DF96 ) ) * Out_Cam_DF100 ) );
					float2 lerpResult21 = lerp( float2( 0,0 ) , temp_cast_3 , ( ( _DistortionOverallIntensity * CVS_Intensity110 ) * Out_Cutout84 ));
					float4 screenColor12 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( appendResult18 + lerpResult21 ));
					float4 VC_RGBA128 = IN.ase_color;
					float4 lerpResult133 = lerp( screenColor12 , ( screenColor12 * ( ( _ColorTint * VC_RGBA128 ) * _ColorEmission ) ) , Out_Cutout84);
					float4 Final_Color88 = lerpResult133;
					
					float smoothstepResult158 = smoothstep( _FinalOpacityErosion , ( _FinalOpacityErosion + _FinalOpacityErosionSmoothness ) , Out_Cutout84);
					float Final_Opacity87 = saturate( ( IN.ase_color.a * saturate( smoothstepResult158 ) ) );
					

					float3 Color = Final_Color88.rgb;
					float Alpha = Final_Opacity87;
					half AlphaClipThreshold = 0.5;
					half AlphaClipThresholdShadow = 0.5;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = IN.pos.z;
					#endif

					#ifdef _ALPHATEST_ON
						clip( Alpha - AlphaClipThreshold );
					#endif

				#if defined( ASE_SURFACE_TRANSPARENT ) || defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, 1.0 );
				#endif
				}
			ENDCG
		}

	
	}
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":91,"pos":[-8368,1232],"params":["Inherit","False","3887.023","556.7161","Cutout","22","84","64","65","67","63","66","69","14","40","41","39","86","73","71","70","68","105","104","106","107","108","109","Cutout","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":115,"pos":[-2482,-1458],"params":["Inherit","False","548","515","Custom Vertex Streams","6","110","47","112","113","114","120","Custom Vertex Streams","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":92,"pos":[-8368,-48],"params":["Inherit","False","3876","1027","Noises","32","54","48","49","50","55","53","52","51","34","36","35","33","32","56","58","57","59","38","37","13","10","60","82","116","117","118","119","121","122","123","124","125","Noises","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":63,"pos":[-7808,1536],"params":["Inherit","False","Property","_CutoutRotation","Cutout Rotation","18","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":65,"pos":[-8064,1408],"params":["Inherit","False","Property","_CutoutOffset","Cutout Offset","19","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":64,"pos":[-8320,1280],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":112,"pos":[-2432,-1152],"params":["Inherit","False","1","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":48,"pos":[-8320,512],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":52,"pos":[-8064,640],"params":["Inherit","False","Property","_DistDistUVS","Dist Dist UV S","11","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":66,"pos":[-7808,1408],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":67,"pos":[-8064,1280],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":114,"pos":[-2176,-1080],"params":["Inherit","False","CSV Rand Sca","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":49,"pos":[-8064,512],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":51,"pos":[-7680,640],"params":["Inherit","False","Property","_DistDistUVP","Dist Dist UV P","12","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","-0.1,-0.2","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":102,"pos":[-8368,2256],"params":["Inherit","False","1188","187","Depth Fade","4","93","96","94","95","Depth Fade","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor","id":69,"pos":[-7808,1280],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0.5,0.5","False","2","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":34,"pos":[-8064,128],"params":["Inherit","False","Property","_DistUVS","Dist UV S","5","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","2,2","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":50,"pos":[-7680,512],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":119,"pos":[-7936,256],"params":["Inherit","False","114","CSV Rand Sca","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":39,"pos":[-7552,1536],"params":["Inherit","False","Property","_CutoutMaskSelector","Cutout Mask Selector","15","0","Create","True","0","0","0","False","0","False","Object","-1","","0,1,0,0","0,1,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":93,"pos":[-8320,2304],"params":["Inherit","False","Property","_DepthFade","Depth Fade","22","0","Create","True","0","0","0","False","3","Space(33)","Header(Depth Fade)","Space(13)","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":14,"pos":[-7552,1280],"params":["Inherit","True","Property","_CutoutTexture","Cutout Texture","14","0","Create","True","0","0","0","False","3","Space(33)","Header(Cutout)","Space(13)","False","","-1","38e5fbcdc59407f4687abb7193140e38","38e5fbcdc59407f4687abb7193140e38","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":32,"pos":[-8320,0],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":54,"pos":[-7168,768],"params":["Inherit","False","Property","_DistortionDistSelector","Distortion Dist Selector","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0,1,0,0","1,0,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":118,"pos":[-7936,128],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":53,"pos":[-7168,512],"params":["Inherit","True","Property","_DistortionDist","Distortion Dist","9","0","Create","True","0","0","0","False","3","Space(33)","Header(Distortion Dist Noise)","Space(13)","False","","-1","a5cc908907bcc99438b8da8e0fae7454","a5cc908907bcc99438b8da8e0fae7454","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":103,"pos":[-8368,3024],"params":["Inherit","False","1188","291","Camera Depth Fade","5","98","99","100","97","101","Camera Depth Fade","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":41,"pos":[-7168,1280],"params":["Inherit","False","2","0","COLOR","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DepthFade, AmplifyShaderEditor","id":94,"pos":[-7936,2304],"params":["Inherit","False","True","True","False","2","1","FLOAT3","0,0,0","False","0","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":68,"pos":[-6912,1664],"params":["Inherit","False","Property","_CutoutErosionSmoothness","Cutout Erosion Smoothness","17","0","Create","True","0","0","0","False","0","False","Object","-1","","0.05","0.05","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":113,"pos":[-2176,-1152],"params":["Inherit","False","CSV Rand Off","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":33,"pos":[-8064,0],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":55,"pos":[-6784,512],"params":["Inherit","False","2","0","COLOR","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":36,"pos":[-7680,128],"params":["Inherit","False","Property","_DistUVP","Dist UV P","6","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0.1,-0.1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":40,"pos":[-7040,1280],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":97,"pos":[-8320,3072],"params":["Inherit","False","Property","_CameraDepthFadeLength","Camera Depth Fade Length","23","0","Create","True","0","0","0","False","3","Space(33)","Header(Camera Depth Fade)","Space(13)","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":101,"pos":[-8320,3200],"params":["Inherit","False","Property","_CameraDepthFadeOffset","Camera Depth Fade Offset","24","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":95,"pos":[-7680,2304],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":70,"pos":[-6912,1536],"params":["Inherit","False","Property","_CutoutErosion","Cutout Erosion","16","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":71,"pos":[-6528,1536],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":57,"pos":[-7040,128],"params":["Inherit","False","Constant","_Vector1","Vector 0","3","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":60,"pos":[-7040,256],"params":["Inherit","False","Property","_DistortionDistLerp","Distortion Dist Lerp","13","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":56,"pos":[-6656,512],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":35,"pos":[-7680,0],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":117,"pos":[-7424,128],"params":["Inherit","False","113","CSV Rand Off","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":96,"pos":[-7424,2304],"params":["Inherit","False","Out DF","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CameraDepthFade, AmplifyShaderEditor","id":98,"pos":[-7936,3072],"params":["Inherit","False","3","2","FLOAT3","0,0,0","False","0","FLOAT","1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":73,"pos":[-6528,1280],"params":["Inherit","True","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":59,"pos":[-6656,128],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":116,"pos":[-7424,0],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":99,"pos":[-7680,3072],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":108,"pos":[-5760,1536],"params":["Inherit","False","96","Out DF","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":86,"pos":[-6272,1280],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":58,"pos":[-6400,0],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":100,"pos":[-7424,3072],"params":["Inherit","False","Out Cam DF","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":104,"pos":[-5760,1408],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":38,"pos":[-6272,256],"params":["Inherit","False","Property","_DistortionNoiseSelector","Distortion Noise Selector","4","0","Create","True","0","0","0","False","0","False","Object","-1","","0,1,0,0","0,0,1,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":10,"pos":[-6272,0],"params":["Inherit","True","Property","_DistortionNoise","Distortion Noise","3","0","Create","True","0","0","0","False","3","Space(33)","Header(Distortion Noise)","Space(13)","False","","-1","a5cc908907bcc99438b8da8e0fae7454","a5cc908907bcc99438b8da8e0fae7454","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":105,"pos":[-5632,1408],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":109,"pos":[-5248,1536],"params":["Inherit","False","100","Out Cam DF","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":37,"pos":[-5888,0],"params":["Inherit","False","2","0","COLOR","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":121,"pos":[-5760,384],"params":["Inherit","False","Property","_DistortionErosionSmoothness","Distortion Erosion Smoothness","8","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":106,"pos":[-5248,1408],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":47,"pos":[-2432,-1408],"params":["Inherit","False","0","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":13,"pos":[-5760,0],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":123,"pos":[-5376,256],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":122,"pos":[-5760,256],"params":["Inherit","False","Property","_DistortionErosion","Distortion Erosion","7","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":135,"pos":[-2738,1230],"params":["Inherit","False","1848.675","700.2252","VC","11","155","156","157","158","159","153","151","128","87","152","46","VC","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":134,"pos":[-4018,-50],"params":["Inherit","False","3108","931","Color","21","12","61","81","19","20","21","17","18","83","111","88","127","129","45","130","133","126","132","80","148","149","Color","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":107,"pos":[-5120,1408],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":110,"pos":[-2176,-1408],"params":["Inherit","False","CVS Intensity","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":124,"pos":[-5376,0],"params":["Inherit","True","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":84,"pos":[-4736,1296],"params":["Inherit","False","Out Cutout","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":46,"pos":[-2688,1280],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":125,"pos":[-5120,0],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":111,"pos":[-3968,640],"params":["Inherit","False","110","CVS Intensity","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":45,"pos":[-3968,512],"params":["Inherit","False","Property","_DistortionOverallIntensity","Distortion Overall Intensity","0","0","Create","True","0","0","0","False","3","Space(13)","Header(Ge Lush was here)","Space(13)","False","Object","-1","","1","0.2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":147,"pos":[-2736,2376],"params":["Inherit","False","1444","547","Camera Push","9","137","138","136","143","144","145","140","139","141","Camera Push","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":155,"pos":[-2688,1792],"params":["Inherit","False","Property","_FinalOpacityErosionSmoothness","Final Opacity Erosion Smoothness","21","0","Create","True","0","0","0","False","0","False","Object","-1","","0.05","0.05","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":82,"pos":[-4736,0],"params":["Inherit","False","Out Mask","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":61,"pos":[-3584,512],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":80,"pos":[-3968,768],"params":["Inherit","False","84","Out Cutout","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":128,"pos":[-1152,1280],"params":["Inherit","False","VC RGBA","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":152,"pos":[-2688,1536],"params":["Inherit","False","84","Out Cutout","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":136,"pos":[-2688,2688],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":157,"pos":[-2304,1664],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":156,"pos":[-2688,1664],"params":["Inherit","False","Property","_FinalOpacityErosion","Final Opacity Erosion","20","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":81,"pos":[-3328,512],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":20,"pos":[-3328,256],"params":["Inherit","False","Constant","_Vector0","Vector 0","3","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor","id":17,"pos":[-3968,0],"params":["Inherit","False","0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":83,"pos":[-3328,384],"params":["Inherit","False","82","Out Mask","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":129,"pos":[-2688,512],"params":["Inherit","False","128","VC RGBA","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":130,"pos":[-2688,256],"params":["Inherit","False","Property","_ColorTint","Color Tint","1","0","Create","True","0","0","0","False","3","Space(33)","Header(Color)","Space(13)","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SurfaceDepthNode, AmplifyShaderEditor","id":143,"pos":[-2432,2688],"params":["Inherit","False","0","1","0","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":158,"pos":[-2176,1536],"params":["Inherit","True","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":21,"pos":[-3072,256],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":18,"pos":[-3712,0],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":127,"pos":[-2304,256],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":149,"pos":[-2048,384],"params":["Inherit","False","Property","_ColorEmission","Color Emission","2","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":137,"pos":[-2688,2432],"params":["Inherit","False","Object","True","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":144,"pos":[-2176,2688],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.05","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":140,"pos":[-1920,2816],"params":["Inherit","False","Property","_CameraPush","Camera Push","25","0","Create","True","0","0","0","False","3","Space(33)","Header(Camera Push)","Space(13)","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":159,"pos":[-1920,1536],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":19,"pos":[-2944,0],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":148,"pos":[-2048,256],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":138,"pos":[-2432,2432],"params":["Inherit","False","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMinOpNode, AmplifyShaderEditor","id":145,"pos":[-1920,2688],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":151,"pos":[-1664,1408],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScreenColorNode, AmplifyShaderEditor","id":12,"pos":[-2688,0],"params":["Inherit","False","Global","_GrabScreen0","Grab Screen 0","2","0","Create","True","0","0","0","False","0","False","","Object","-1","False","False","False","False","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":126,"pos":[-2048,128],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":132,"pos":[-1792,256],"params":["Inherit","False","84","Out Cutout","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":139,"pos":[-1792,2432],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":153,"pos":[-1408,1408],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":133,"pos":[-1792,0],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":150,"pos":[-434,-50],"params":["Inherit","False","292","419","Connect","3","90","142","89","Connect","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":31,"pos":[590,-50],"params":["Inherit","False","1238","166","Ge Lush was here! <3","5","22","23","24","29","30","Ge Lush was here! <3","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":141,"pos":[-1536,2432],"params":["Inherit","False","WPO","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":87,"pos":[-1152,1408],"params":["Inherit","False","Final Opacity","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":88,"pos":[-1152,0],"params":["Inherit","False","Final Color","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":22,"pos":[640,0],"params":["Inherit","False","Property","_Cull","Cull","26","0","Create","True","0","0","0","True","3","Space(33)","Header(AR)","Space(13)","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":23,"pos":[896,0],"params":["Inherit","False","Property","_Src","Src","27","0","Create","True","0","0","0","True","0","False","Object","-1","","5","5","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":24,"pos":[1152,0],"params":["Inherit","False","Property","_Dst","Dst","28","0","Create","True","0","0","0","True","0","False","Object","-1","","10","10","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":29,"pos":[1408,0],"params":["Inherit","False","Property","_ZWrite","ZWrite","29","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":30,"pos":[1664,0],"params":["Inherit","False","Property","_ZTest","ZTest","30","0","Create","True","0","0","0","True","0","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":120,"pos":[-2176,-1312],"params":["Inherit","False","CSV Erosion","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":90,"pos":[-384,128],"params":["Inherit","False","87","Final Opacity","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":142,"pos":[-384,256],"params":["Inherit","False","141","WPO","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":89,"pos":[-384,0],"params":["Inherit","False","88","Final Color","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":161,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ExtraPrePass","0","0","ExtraPrePass","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","12","all","0","False","True","1","1","False","","0","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":162,"pos":[0,0],"params":["Float","False","True","-1","3","AmplifyShaderEditor.MaterialInspector","0","7","Vefects/SH_VFX_Vefects_Distortion_Flat_01_BIRP","0770190933193b94aaa3065e307002fa","True","Unlit","0","1","Unlit","8","True","True","1","1","True","_Src","0","True","_Dst","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","True","True","0","True","_Cull","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","1","True","_ZWrite","False","False","False","True","2","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","True","3","True","12","all","0","True","True","1","5","True","_Src","10","True","_Dst","1","1","False","","10","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","True","True","0","True","_Cull","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","2","True","_ZWrite","True","3","True","_ZTest","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","10","Surface","1","639226813412341130","  Keep Alpha","0","0","  Blend","0","0","Alpha Clipping","0","0","  Use Shadow Threshold","0","0","Cast Shadows","0","639226813802996052","Write Depth","0","0","  Conservative","0","0","Extra Pre Pass","0","0","Vertex Position","1","0","0","3","False","True","False","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":163,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","12","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"wire":[66,0,63,0]}
{"wire":[67,0,64,0]}
{"wire":[67,1,65,0]}
{"wire":[114,0,112,2]}
{"wire":[49,0,48,0]}
{"wire":[49,1,52,0]}
{"wire":[69,0,67,0]}
{"wire":[69,2,66,0]}
{"wire":[50,0,49,0]}
{"wire":[50,2,51,0]}
{"wire":[14,1,69,0]}
{"wire":[118,0,34,0]}
{"wire":[118,1,119,0]}
{"wire":[53,1,50,0]}
{"wire":[41,0,14,0]}
{"wire":[41,1,39,0]}
{"wire":[94,0,93,0]}
{"wire":[113,0,112,1]}
{"wire":[33,0,32,0]}
{"wire":[33,1,118,0]}
{"wire":[55,0,53,0]}
{"wire":[55,1,54,0]}
{"wire":[40,0,41,0]}
{"wire":[95,0,94,0]}
{"wire":[71,0,70,0]}
{"wire":[71,1,68,0]}
{"wire":[56,0,55,0]}
{"wire":[35,0,33,0]}
{"wire":[35,2,36,0]}
{"wire":[96,0,95,0]}
{"wire":[98,0,97,0]}
{"wire":[98,1,101,0]}
{"wire":[73,0,40,0]}
{"wire":[73,1,70,0]}
{"wire":[73,2,71,0]}
{"wire":[59,0,57,0]}
{"wire":[59,1,56,0]}
{"wire":[59,2,60,0]}
{"wire":[116,0,35,0]}
{"wire":[116,1,117,0]}
{"wire":[99,0,98,0]}
{"wire":[86,0,73,0]}
{"wire":[58,0,116,0]}
{"wire":[58,1,59,0]}
{"wire":[100,0,99,0]}
{"wire":[104,0,86,0]}
{"wire":[104,1,108,0]}
{"wire":[10,1,58,0]}
{"wire":[105,0,104,0]}
{"wire":[37,0,10,0]}
{"wire":[37,1,38,0]}
{"wire":[106,0,105,0]}
{"wire":[106,1,109,0]}
{"wire":[13,0,37,0]}
{"wire":[123,0,122,0]}
{"wire":[123,1,121,0]}
{"wire":[107,0,106,0]}
{"wire":[110,0,47,3]}
{"wire":[124,0,13,0]}
{"wire":[124,1,122,0]}
{"wire":[124,2,123,0]}
{"wire":[84,0,107,0]}
{"wire":[125,0,124,0]}
{"wire":[82,0,125,0]}
{"wire":[61,0,45,0]}
{"wire":[61,1,111,0]}
{"wire":[128,0,46,0]}
{"wire":[157,0,156,0]}
{"wire":[157,1,155,0]}
{"wire":[81,0,61,0]}
{"wire":[81,1,80,0]}
{"wire":[143,0,136,0]}
{"wire":[158,0,152,0]}
{"wire":[158,1,156,0]}
{"wire":[158,2,157,0]}
{"wire":[21,0,20,0]}
{"wire":[21,1,83,0]}
{"wire":[21,2,81,0]}
{"wire":[18,0,17,1]}
{"wire":[18,1,17,2]}
{"wire":[127,0,130,0]}
{"wire":[127,1,129,0]}
{"wire":[144,0,143,0]}
{"wire":[159,0,158,0]}
{"wire":[19,0,18,0]}
{"wire":[19,1,21,0]}
{"wire":[148,0,127,0]}
{"wire":[148,1,149,0]}
{"wire":[138,0,137,0]}
{"wire":[145,0,144,0]}
{"wire":[145,1,140,0]}
{"wire":[151,0,46,4]}
{"wire":[151,1,159,0]}
{"wire":[12,0,19,0]}
{"wire":[126,0,12,0]}
{"wire":[126,1,148,0]}
{"wire":[139,0,138,0]}
{"wire":[139,1,145,0]}
{"wire":[153,0,151,0]}
{"wire":[133,0,12,0]}
{"wire":[133,1,126,0]}
{"wire":[133,2,132,0]}
{"wire":[141,0,139,0]}
{"wire":[87,0,153,0]}
{"wire":[88,0,133,0]}
{"wire":[120,0,47,4]}
{"wire":[162,0,89,0]}
{"wire":[162,7,90,0]}
{"wire":[162,15,142,0]}
ASEEND*/
//CHKSM=A2B62F97F4F3848D3241E268950757207BB6725A