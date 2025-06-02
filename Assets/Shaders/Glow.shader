Shader "Custom/GlowURP"
{
    Properties
    {
        _ColorTint("Color Tint", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(1.0, 6.0)) = 3.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            Pass
            {
                Name "ForwardLit"
                Tags{"LightMode" = "UniversalForward"}

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS   : NORMAL;
                    float2 uv         : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv          : TEXCOORD0;
                    float3 viewDirWS   : TEXCOORD1;
                    float3 normalWS    : TEXCOORD2;
                };

                float4 _ColorTint;
                float4 _RimColor;
                float _RimPower;
                sampler2D _MainTex;
                float4 _MainTex_ST;

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                    OUT.positionHCS = TransformWorldToHClip(positionWS);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                    OUT.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    float3 baseColor = tex2D(_MainTex, IN.uv).rgb * _ColorTint.rgb;
                    float rim = 1.0 - saturate(dot(normalize(IN.viewDirWS), normalize(IN.normalWS)));
                    float3 emission = _RimColor.rgb * pow(rim, _RimPower);
                    return half4(baseColor + emission, 1.0);
                }
                ENDHLSL
            }
        }

            FallBack "Hidden/InternalErrorShader"
}
