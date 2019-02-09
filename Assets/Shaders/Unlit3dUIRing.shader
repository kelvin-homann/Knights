﻿Shader "Unlit/Unlit3dUIRing"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_ShadowColor ("Shadow Color", Range(0, 1)) = 0
		//_ShadowColor ("Shadow Color", Color) = (1,1,1,1)
		_ShadowDistance ("Shadow Distance", Range(0, 5)) = 0
		_InnerRadius ("Inner Radius", Float) = 1
		_OuterRadius ("Outer Radius", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		

		Pass
		{
			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag FullForwardShadows
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			fixed4 _Color;
			float _ShadowColor;
			float _ShadowDistance;

			float _InnerRadius;
			float _OuterRadius;

			v2f vert (appdata v)
			{
				v2f o;
				
				v.vertex *= (_InnerRadius * v.color.g + _OuterRadius * v.color.r);

				o.vertex = UnityObjectToClipPos(v.vertex - v.normal * _ShadowDistance);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color * _ShadowColor;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}

		Pass
		{
			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			fixed4 _Color;
			float _InnerRadius;
			float _OuterRadius;

			v2f vert (appdata v)
			{
				v2f o;

				v.vertex *= (_InnerRadius * v.color.g + _OuterRadius * v.color.r);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}

		Pass 
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			Fog {Mode Off}
			ZWrite On ZTest LEqual Cull Off
			Offset 1, 1
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
	
			struct v2f { 
				V2F_SHADOW_CASTER;
			};
	
			v2f vert( appdata_base v )
			{
				v2f o;
				TRANSFER_SHADOW_CASTER(o)
				return o;
			}
	
			float4 frag( v2f i ) : COLOR
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
}
	}
}
