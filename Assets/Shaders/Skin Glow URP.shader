Shader "Custom/URP_EmissiveUnlitTransparent"
{
    Properties
    {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _EmissiveColor("Emissive Color", Color) = (1,1,1,1)
        _EmissiveIntensity("Emissive Intensity", Range(0,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _EmissiveColor;
            float _EmissiveIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionH : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float4 posWS = mul(unity_ObjectToWorld, IN.positionOS);
                float4 posH = mul(UNITY_MATRIX_VP, posWS);

                OUT.positionH = posH;
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 emissive = texCol.rgb * _EmissiveColor.rgb * _EmissiveIntensity;

                return half4(emissive, texCol.a);
            }
            ENDHLSL
        }
    }
}