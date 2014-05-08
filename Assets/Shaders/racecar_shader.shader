Shader "Racecar_custom/racecar_shader" {
	Properties {
		_MainTint ("Diffuse Tint", Color) = (1,1,1,1)
		_MainTex ("Diffuse (RGB)", 2D) = "white" {}
		_CubeMapTex ("Ambient Cube", Cube) = "" {}
		_RampTex ("Ramp Texture", 2D) = "white" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
		_SpecPower ("Spec Power", Range(0, 1)) = 0.3
		_SpecGloss ("Spec Gloss", Range(0, 1)) = 0.3
		_Rim ("Rim", Range(0, 1.0)) = 1.0
		_Metalness("Metalness", Range(0, 1.0)) = 0.0
		_Masks("Mask Texture(R=SpecPow, G=SpecGloss, B=Rim, A=Metalness)", 2D) = "white" {}
		_DamageMask("DamageMask(RGB = color, A=Depth)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf ToonRacecar
		#pragma target 3.0

		sampler2D _MainTex;
		samplerCUBE _CubeMapTex;
		sampler2D _RampTex;
		sampler2D _NormalMap;
		float4 _MainTint;
		float _SpecPower;
		float _SpecGloss;
		float _Rim;
		float _Metalness;

		struct Input {
			float2 uv_MainTex;
			float2 uv_NormalMap;
			float3 worldNormal;INTERNAL_DATA
		};
		struct CustomSurfaceOutput {
	        fixed3 Albedo;
	        fixed3 Normal;
	        fixed3 Emission;
	        half Specular;
	        fixed Gloss;
	        fixed Alpha;
	        fixed3 WorldNormal;
		};

		inline float4 LightingToonRacecar (CustomSurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			float NdotL = saturate(dot(s.Normal, normalize(lightDir)));
			float3 halfVector = normalize(lightDir + viewDir);
			float NdotH = saturate(dot(s.Normal, halfVector));
			float NdotV = saturate(dot(s.Normal, normalize(viewDir)));
			float VdotH = saturate(dot(halfVector, normalize(viewDir)));
			
			//not view dependent
			float hLambert = (NdotL * 0.5 + 0.5);// * atten;
			hLambert *= hLambert;
			float3 ramp = pow(tex2D(_RampTex, float2(hLambert)).rgb, 2.2);
			float3 ambientCubeVal = pow(texCUBE(_CubeMapTex, s.WorldNormal).rgb , 2.2) / 4;
			
			//view dependent
			float spec = clamp(pow(NdotH, s.Gloss) * s.Specular,0,1);
			
			float fresnel = pow(1.0 - NdotV, 2.0);
			float rim = saturate(_Rim * fresnel * s.WorldNormal.g * clamp(ambientCubeVal, 0.5, 1.0));
			
			//metalness
			half3 diffuseColor = s.Albedo * clamp((1 - _Metalness),0.2, 1.0);
	
			half3 specColor = lerp(_LightColor0.rgb, s.Albedo, _Metalness);
			
			float4 col;
			
			col.rgb = pow((((ramp + ambientCubeVal) * diffuseColor) + (specColor * spec) + rim) * atten, 0.5454);
			
			//works but could be real slow
			//clip(s.Alpha - 0.5);
			
			//alpha is ignored
			//col.a = s.Alpha;
			
			return col;
		}
		void surf (Input IN, inout CustomSurfaceOutput o) {
			half4 c = pow(tex2D(_MainTex, IN.uv_MainTex), 2.2);
			float3 normals = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap)).rgb;
			//float3 ambientCubeVal = texCUBE(_CubeMapTex, WorldNormalVector(IN, normals)).rgb / 4 + 0.5;
			o.WorldNormal = WorldNormalVector(IN, normals);
			o.Albedo = c.rgb;
			o.Normal = normals;			
			o.Alpha = c.a;
			o.Emission = 0.0;
			o.Specular = _SpecPower * _SpecPower * 50.0;
			o.Gloss = _SpecGloss * _SpecGloss * 50.0 + 10.0;
			
		}
		
		ENDCG
	} 
	FallBack "Diffuse"
}
