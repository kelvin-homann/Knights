Shader "Hidden/SplitscreenComposite"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Camera2 ("Camera 2", 2D) = "white" {}
		_Camera3 ("Camera 3", 2D) = "white" {}
		_Camera4 ("Camera 4", 2D) = "white" {}

		_Mask ("Mask", 2D) = "white" {}
		_LineColor ("Line Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _Camera2;
			sampler2D _Camera3;
			sampler2D _Camera4;
			sampler2D _Mask;

			uniform fixed4 _MainTex_ST;
			uniform fixed4 _Camera2_ST;
			uniform fixed4 _Camera3_ST;
			uniform fixed4 _Camera4_ST;

			fixed4 _LineColor;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 cam1 = tex2D(_MainTex, i.uv + _MainTex_ST.zw);
				fixed4 cam2 = tex2D(_Camera2, i.uv + _Camera2_ST.zw);
				fixed4 cam3 = tex2D(_Camera3, i.uv + _Camera3_ST.zw);
				fixed4 cam4 = tex2D(_Camera4, i.uv + _Camera4_ST.zw);

				fixed4 mask = tex2D(_Mask, i.uv);
				
				fixed4 col = cam1 * mask.r + cam2 * mask.g + cam3 * mask.b + cam4 * mask.a;

				return _LineColor * (1 - col.a) + col;
			}
			ENDCG
		}
	}
}
