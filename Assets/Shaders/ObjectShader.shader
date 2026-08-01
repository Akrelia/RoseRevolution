Shader "Custom/ObjectShader"
{
    Properties
    {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _LightTex("Light (RGB)", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
        _Color("Main Color", Color) = (1,1,1,1)
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
                float _Cutoff;
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

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 lightTex = SAMPLE_TEXTURE2D(_LightTex, sampler_LightTex, input.lightmapUV);

                clip(mainTex.a - _Cutoff);

                half3 albedo = mainTex.rgb;
                half3 color = albedo * lightTex.rgb * _Color.rgb * 2.0;

                color *= _GlobalTintColor.rgb;

                half3 normalWS = normalize(input.normalWS);
                half3 lighting = 0;

                Light mainLight = GetMainLight();

                half NdotL = saturate(dot(normalWS, mainLight.direction));

                lighting += mainLight.color * NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)

                    uint additionalLightsCount = GetAdditionalLightsCount();

                    for (uint i = 0; i < additionalLightsCount; i++)
                    {
                        Light light = GetAdditionalLight(i, input.positionWS);

                        half lightNdotL = saturate(dot(normalWS, light.direction));

                        lighting += light.color * lightNdotL * light.distanceAttenuation * light.shadowAttenuation;
                    }

                #endif

                color += albedo * lighting;

                color = MixFog(color, input.fogFactor);

                return half4(color, mainTex.a);
            }

            ENDHLSL
        }
    }
}