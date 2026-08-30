Shader "Rose Revolution/Skybox"
{
    Properties
    {
        [MainTexture] _DayTex ("Day Texture", 2D) = "white" {}
        _NightTex ("Night Texture", 2D) = "black" {}
        _Blend ("Day/Night Blend", Range(0, 1)) = 0
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _FogIntensity ("Fog Intensity", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Background"
            "RenderType" = "Background"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            TEXTURE2D(_DayTex);
            SAMPLER(sampler_DayTex);

            TEXTURE2D(_NightTex);
            SAMPLER(sampler_NightTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _DayTex_ST;
                float4 _NightTex_ST;
                float _Blend;
                float4 _TintColor;
                float _FogIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionHCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _DayTex);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 day = SAMPLE_TEXTURE2D(_DayTex, sampler_DayTex, input.uv);
                half4 night = SAMPLE_TEXTURE2D(_NightTex, sampler_NightTex, input.uv);

                half4 color = lerp(night, day, _Blend);
                color *= _TintColor;

                half3 foggedColor = MixFog(color.rgb, input.fogFactor);
                color.rgb = lerp(color.rgb, foggedColor, _FogIntensity);

                return color;
            }

            ENDHLSL
        }
    }
}