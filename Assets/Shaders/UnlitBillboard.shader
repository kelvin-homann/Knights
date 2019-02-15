Shader "Unlit/Billboard"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	    _Color("Color", Color) = (1, 1, 1, 1)
		_ShadowColor("Shadow Color", Color) = (0, 0, 0, 1)
		_ShadowSize("Shadow Size", Range(0, 2)) = 1
		_ShadowOffsetX("Shadow Offset X", Float) = 0
		_ShadowOffsetY("Shadow Offset Y", Float) = 0
	}
		SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "DisableBatching" = "True" }

		ZWrite Off
		Blend One OneMinusSrcAlpha

			Pass
		{
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 pos : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _ShadowColor;
			half _ShadowSize;
			half _ShadowOffsetX;
			half _ShadowOffsetY;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;

				// billboard mesh towards camera
				float3 vpos = mul((float3x3)unity_ObjectToWorld, (v.vertex.xyz + float3(_ShadowOffsetX, _ShadowOffsetY, 0)) * _ShadowSize);
				float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				float4 outPos = mul(UNITY_MATRIX_P, viewPos);

				o.pos = outPos;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb = _ShadowColor.rgb;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				col.rgb *= col.a;
				return col;
			}
			ENDCG
		}

		Pass
		{
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 pos : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;

				// billboard mesh towards camera
				float3 vpos = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
				float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				float4 outPos = mul(UNITY_MATRIX_P, viewPos);

				o.pos = outPos;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb = _Color.rgb;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				col.rgb *= col.a;
				return col;
			}
			ENDCG
		}

		
	}
}