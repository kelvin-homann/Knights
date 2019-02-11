Shader "Custom/CellShadingForwardDissolve" {
    Properties{
    _Color("Color", Color) = (1,1,1,1)
    _MainTex("Albedo (RGB)", 2D) = "white" {}
	_Ramp("Cell Shading Ramp", 2D) = "white" {}

        //Dissolve properties
        _DissolveTexture("Dissolve Texture", 2D) = "white" {}
        _Amount("Amount", Range(0,1)) = 0
    }

        SubShader{
            Tags { "Queue" = "Transparent" "RenderType" = "TransparentCutout" }
            LOD 200
            Cull Off //Fast way to turn your material double-sided

            CGPROGRAM
            #pragma surface surf CelShadingForward addshadow

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

            struct Input {
                float2 uv_MainTex;
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;

            //Dissolve properties
            sampler2D _DissolveTexture;
			float4 _DissolveTexture_ST;
            half _Amount;

            void surf(Input IN, inout SurfaceOutput o) {

				//Basic shader function
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

                //Dissolve function
                half dissolve_value = tex2D(_DissolveTexture, IN.uv_MainTex * _DissolveTexture_ST.xy + _DissolveTexture_ST.zw).r;
                clip(dissolve_value - _Amount);
				//if ((dissolve_value - _Amount) < 0)
				//	c.a = 0;

                if (dissolve_value - _Amount < .05f) //outline width = .05f
                    c.rgb = fixed3(1, 1, 1); //emits white color


                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
