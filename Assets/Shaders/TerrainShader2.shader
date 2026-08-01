Shader "Custom/TerrainShader2"
{
    Properties
    {
        _BottomTex("Bottom (RGBA)", 2D) = "white" {}
        _TopTex("Top (RGBA)", 2D) = "white" {}
        _LightTex("Light (RGBA)", 2D) = "white" {}
        _NormalMapTop("Normals (RGBA)", 2D) = "white" {}
        _NormalMapBottom("Normals (RGBA)", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 200

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

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
                float2 uv2 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 uv2 : TEXCOORD3;
                float2 lightUV : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            sampler2D _BottomTex;
            sampler2D _TopTex;
            sampler2D _LightTex;

            float4 _TintColor;
            float4 _GlobalTintColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                OUT.uv = IN.uv;
                OUT.uv2 = IN.uv2;
                OUT.lightUV = IN.color.rgb;

                OUT.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 bottom = tex2D(_BottomTex, IN.uv);
                half4 top = tex2D(_TopTex, IN.uv2);
                half4 lightTex = tex2D(_LightTex, IN.lightUV);

                half3 albedo = lerp(bottom.rgb, top.rgb, top.a);
                half3 emission = albedo * lightTex.rgb * 5.0;

                half3 baseColor = (albedo + emission) * _GlobalTintColor.rgb;

                half3 lighting = 0;

                Light mainLight = GetMainLight();

                half NdotL = saturate(dot(normalize(IN.normalWS), mainLight.direction));

                lighting += mainLight.color * NdotL * mainLight.distanceAttenuation* mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)

                    uint additionalLightsCount = GetAdditionalLightsCount();

                    for (uint i = 0; i < additionalLightsCount; i++)
                    {
                        Light light = GetAdditionalLight(i, IN.positionWS);

                        half lightNdotL = saturate(dot(normalize(IN.normalWS), light.direction));

                        lighting += light.color * lightNdotL * light.distanceAttenuation * light.shadowAttenuation;
                    }

                #endif

                half3 finalColor = baseColor + albedo * lighting;

                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}