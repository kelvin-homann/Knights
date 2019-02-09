Shader "Custom/CelShadingForwardTextureNormal" {
	Properties {
		_MainTex("Texture", 2D) = "white" {}
		_Bump ("Normal Map", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
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
		sampler2D _Bump;
		fixed4 _Color;

		struct Input {
			float2 uv_MainTex;
			float2 uv_Bump;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 col = tex2D(_MainTex, IN.uv_MainTex);
			half3 normal = UnpackNormal(tex2D(_Bump, IN.uv_Bump));

			o.Albedo = col.rgb * _Color.rgb;
			o.Alpha = 1;
			o.Normal = normal;
		}
		ENDCG
	}
	FallBack "Diffuse"
}