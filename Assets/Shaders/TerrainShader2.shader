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
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 200

            Pass
            {
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv2 : TEXCOORD1;
                    float4 color : COLOR; // Akima : use the colors here
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv2 : TEXCOORD1;
                    float2 lightUV : TEXCOORD2;
                };

                sampler2D _BottomTex;
                sampler2D _TopTex;
                sampler2D _LightTex;

                float4 _TintColor;

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = IN.uv;
                    OUT.uv2 = IN.uv2;
                    OUT.lightUV = IN.color.rgb; // Akima : use the colors as UVs
                    return OUT;
                }

                float4 _GlobalTintColor;

                half4 frag(Varyings IN) : SV_Target
                {
                    half4 bottom = tex2D(_BottomTex, IN.uv);
                    half4 top = tex2D(_TopTex, IN.uv2);
                    half4 light = tex2D(_LightTex, IN.lightUV);

                    half3 albedo = lerp(bottom.rgb, top.rgb, top.a);
                    half3 emission = albedo * light.rgb * 5;

                    float4 tint = _GlobalTintColor;

                    if (tint.a == 0)
                    {
                        tint = float4(1, 1, 1, 1); // fallback
                    }

                    half3 finalColor = (albedo + emission) * tint.rgb;

                    return half4(finalColor, 1.0);
                }
                ENDHLSL
            }
        }
            FallBack "Diffuse"
}
