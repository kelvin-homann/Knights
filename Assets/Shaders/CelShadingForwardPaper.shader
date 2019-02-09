Shader "Custom/CelShadingForwardPaper" {
	Properties {
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
		_DetailIntensity("Detail Intensity", Range(0, 1)) = 0
		_Detail("Paper Detail", 2D) = "white" {}
		_Edges("Edge Mask", 2D) = "black" {}
		_EdgeColor("Edge Color", Color) = (1, 1, 1, 1)
		_EdgeThreshold("Edge Threshold", Range(0, 1)) = 0.5
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
		fixed4 _Color;

		sampler2D _Detail;
		half _DetailIntensity;

		sampler2D _Edges;
		fixed4 _EdgeColor;
		half _EdgeThreshold;

		struct Input {
			float2 uv_MainTex;
			float2 uv_Detail;
		};


		void surf(Input IN, inout SurfaceOutput o) {
			//Sample detail texture
			fixed4 detail = tex2D(_Detail, IN.uv_Detail) - fixed4(0.5, 0.5, 0.5, 0.5);

			//Sample main tex
			fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color;

			//Sample edge textrue
			fixed4 edge = tex2D(_Edges, IN.uv_MainTex);
			if (edge.a > _EdgeThreshold) {
				col = _EdgeColor;
			}

			// Albedo comes from a texture tinted by color
			o.Albedo = col.rgb + (detail.rgb * 2 * _DetailIntensity);
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}