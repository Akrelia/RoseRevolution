Shader "Rose Revolution/Skybox"
{
    Properties
    {
        [MainTexture] _DayTex ("Day Texture", 2D) = "white" {}
        _NightTex ("Night Texture", 2D) = "black" {}
        _Blend ("Day/Night Blend", Range(0, 1)) = 0
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
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
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _DayTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 day = SAMPLE_TEXTURE2D(_DayTex, sampler_DayTex, input.uv);
                half4 night = SAMPLE_TEXTURE2D(_NightTex, sampler_NightTex, input.uv);

                half4 color = lerp(night, day, _Blend);

                return color * _TintColor;
            }

            ENDHLSL
        }
    }
}