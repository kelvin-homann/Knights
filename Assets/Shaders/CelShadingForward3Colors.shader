Shader "Custom/CelShadingForward3Colors" {
	Properties {
		_Color1("Color 1 (R)", Color) = (1, 1, 1, 1)
		_Color2("Color 2 (G)", Color) = (1, 1, 1, 1)
		_Color3("Color 3 (B)", Color) = (1, 1, 1, 1)
		_MainTex("Color Mask (RGB)", 2D) = "white" {}
		_Ramp("Cell Shading Ramp", 2D) = "white" {}
	}
	SubShader {
		Tags {
			"RenderType" = "Opaque"
		}
		LOD 200

		CGPROGRAM 
		#pragma surface surf CelShadingForward
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
		fixed4 _Color1;
		fixed4 _Color2;
		fixed4 _Color3;

		struct Input {
			float2 uv_MainTex;
			fixed4 color : COLOR;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 mask = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = (_Color1 * mask.r + _Color2 * mask.g + _Color3 * mask.b).rgb * IN.color.rgb;
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}