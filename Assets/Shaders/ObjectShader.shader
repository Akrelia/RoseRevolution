Shader "Custom/ObjectShader"
{
    Properties
    {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _LightTex("Light (RGB)", 2D) = "white" {}
        _HasLightmap("Has Lightmap", Float) = 0
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
        _Color("Main Color", Color) = (1,1,1,1)
        _Cull("Cull", Int) = 2
        _ZTest("ZTest", Int) = 4
        _ZWrite("ZWrite", Float) = 1
        _RefineLevel("Refine Level", Range(0,9)) = 0
        _RefineColor("Refine Color", Color) = (1,1,1,1)
        _RefineIntensity("Refine Intensity", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "RenderType" = "TransparentCutout"
        }

        LOD 100

        Pass
        {
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite [_ZWrite]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 lightmapUV : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_LightTex);
            SAMPLER(sampler_LightTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _LightTex_ST;
                float4 _Color;
                float _HasLightmap;
                float _Cutoff;
                float _RefineLevel;
                float4 _RefineColor;
                float _RefineIntensity;
            CBUFFER_END

            float4 _GlobalTintColor;

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.lightmapUV = input.lightmapUV;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 lightTex = SAMPLE_TEXTURE2D(_LightTex, sampler_LightTex, input.lightmapUV);

                clip(mainTex.a - _Cutoff);

                half3 albedo = mainTex.rgb;

                // Lightmap
                half3 color;

                if (_HasLightmap > 0.5)
                {
                    color = albedo
                        * lightTex.rgb
                        * _Color.rgb
                        * 5.0;
                }
                else
                {
                    color = albedo * _Color.rgb;
                }

                color *= _GlobalTintColor.rgb;

                // Refine
                half refineAmount = saturate(_RefineLevel / 9.0) * _RefineIntensity;

                color += albedo
                    * _RefineColor.rgb
                    * refineAmount;

                half3 normalWS = normalize(input.normalWS);

                if (!isFrontFace)
                {
                    normalWS = -normalWS;
                }

                // Main directional light
                Light mainLight = GetMainLight();

                half NdotL = saturate(dot(normalWS, mainLight.direction));

                half3 lighting = mainLight.color
                    * NdotL
                    * mainLight.distanceAttenuation
                    * mainLight.shadowAttenuation;

                color += albedo * lighting;

                // Additional lights
                uint additionalLightsCount = GetAdditionalLightsCount();

                for (uint i = 0; i < additionalLightsCount; i++)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);

                    half NdotLAdditional = saturate(dot(normalWS, light.direction));

                    half3 additionalLighting = light.color
                        * NdotLAdditional
                        * light.distanceAttenuation
                        * light.shadowAttenuation;

                    color += albedo * additionalLighting;
                }

                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}