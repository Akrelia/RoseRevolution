Shader "ROSE/RefineGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _RefineColor ("Refine Glow Color", Color) = (1,1,1,1)
        _RefineIntensity ("Refine Intensity", Range(0,10)) = 1
        _GlowPower ("Glow Texture Power", Range(0.1,4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        LOD 100

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode"="UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half light : TEXCOORD1;
            };


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                half4 _Color;

                half4 _RefineColor;

                half _RefineIntensity;
                half _GlowPower;

            CBUFFER_END



            Varyings vert(Attributes IN)
            {
                Varyings OUT;


                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);


                VertexNormalInputs normal = GetVertexNormalInputs(IN.normalOS);


                OUT.positionHCS = pos.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);


                // Original ROSE vertex lighting:
                // max(dot(N,L),0)+0.5

                Light light = GetMainLight();

                half NdotL = max(dot(normal.normalWS, light.direction), 0);


                OUT.light = NdotL + 0.5;


                return OUT;
            }



            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        IN.uv
                    );


                // Diffuse normal
                half3 diffuse =
                    tex.rgb *
                    _Color.rgb *
                    IN.light;



                // Old fixed pipeline approximation:
                //
                // ZZ_GLOW_TEXTURE
                //
                // Glow = Texture * GlowColor

                half3 glowTex =
                    pow(tex.rgb, _GlowPower);


                half3 glow =
                    glowTex *
                    _RefineColor.rgb *
                    _RefineIntensity;



                half3 finalColor =
                    diffuse +
                    glow;



                return half4(
                    finalColor,
                    tex.a * _Color.a
                );
            }

            ENDHLSL
        }
    }
}