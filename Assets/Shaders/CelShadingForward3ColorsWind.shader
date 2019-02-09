// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/CelShadingForwardPlant" {
	Properties {
		_Color1a("Leaves (R)", Color) = (1, 1, 1, 1)
		_Color1b("Leaves Variation (R)", Color) = (1, 1, 1, 1)
		_Color2("Color 2 (G)", Color) = (1, 1, 1, 1)
		_Color3("Color 3 (B)", Color) = (1, 1, 1, 1)
		_MainTex("Color Mask (RGB)", 2D) = "white" {}
		_Wind("Wind Speed and Strnght (X, Z)", Vector) = (1,1,1,1)
		_WindScale("Wind Offset Scale", Float) = 1
		_Ramp("Cell Shading Ramp", 2D) = "white" {}
	}
	SubShader {
		Tags {
			//"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TreeLeaf"
			"RenderType" = "Opaque" "IgnoreProjector" = "True" "Queue" = "Geometry"
		}
		LOD 200
		ZWrite On
		Blend Off

		CGPROGRAM 
		//#pragma surface surf CelShadingForward vertex:vert addshadow
		#pragma surface surf CelShadingForward vertex:vert addshadow
		#pragma target 3.0

		sampler2D _Ramp;
		
		half4 LightingCelShadingForward(SurfaceOutput s, half3 lightDir, half atten) {
			half NdotL = dot(s.Normal, lightDir);
			//NdotL = smoothstep(0, 0.025F, NdotL);
			half cell = tex2D(_Ramp, float2((NdotL + 1) / 2, 0));
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * (cell * atten * 2);
			c.a = s.Alpha;
			return c;
		}

		sampler2D _MainTex;
		fixed4 _Color1a;
		fixed4 _Color1b;
		fixed4 _Color2;
		fixed4 _Color3;

		float4 _Wind;
		float _WindScale;

		struct Input {
			float2 uv_MainTex;
			float3 objectPos;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.objectPos = mul(unity_ObjectToWorld, float4(0,0,0,1));

			float2 scale = float2(o.objectPos.x, o.objectPos.z) * 0.1F * _WindScale;
			float2 wind = sin(float2(sin(_Time.z * _Wind.x + scale.x), cos(_Time.z * _Wind.y + scale.y)));
			v.vertex.xyz += float3(wind.x * _Wind.z, wind.y * _Wind.w, 0) * v.color.r;
		}

		void surf(Input IN, inout SurfaceOutput o) {
			//Calculate leave color
			float leaveBlend = abs(sin(IN.objectPos.x * IN.objectPos.x + IN.objectPos.z * IN.objectPos.z + IN.objectPos.y));
			fixed4 leaves = lerp(_Color1a, _Color1b, leaveBlend);

			// apply color based on mask pixel (r, g, b)
			fixed4 mask = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = (leaves * mask.r + _Color2 * mask.g + _Color3 * mask.b).rgb;
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}