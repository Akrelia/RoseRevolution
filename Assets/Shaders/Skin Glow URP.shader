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
            Tags {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "RenderPipeline" = "UniversalPipeline"
            }
            LOD 100

            // ------------------------------------------------
            // Activation du blending alpha pour la transparence
            // ------------------------------------------------
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            Pass
            {
                Name "FORWARD"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

            // Inclut les macros/fonctions de base URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ------------------------------------------------
            // Déclarations des textures et samplers
            // ------------------------------------------------
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ------------------------------------------------
            // Paramètres exposés à l’inspecteur
            // ------------------------------------------------
            float4 _EmissiveColor;
            float  _EmissiveIntensity;

            // ------------------------------------------------
            // Structure d’entrée du vertex shader
            // ------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;   // position en espace objet (float4)
                float2 uv         : TEXCOORD0;  // coordonnées UV (float2)
            };

            // ------------------------------------------------
            // Structure de sortie du vertex shader / entrée du fragment
            // ------------------------------------------------
            struct Varyings
            {
                float4 positionH : SV_POSITION; // position en clip space (float4)
                float2 uv        : TEXCOORD0;   // coordonnées UV transmises (float2)
            };

            // ------------------------------------------------
            // Vertex Shader : transforme la position et transmet les UV
            // ------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 1) Transforme la position objet → monde  (float4)
                float4 posWS = mul(unity_ObjectToWorld, IN.positionOS);

                // 2) Projection monde → clip space  (float4)
                float4 posH = mul(UNITY_MATRIX_VP, posWS);

                OUT.positionH = posH;
                OUT.uv = IN.uv;
                return OUT;
            }

            // ------------------------------------------------
            // Fragment Shader : échantillonne la texture, calcule l’émissif, gère l’alpha
            // ------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                // 1) Échantillonne la texture de base (RGBA)
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 2) Calcule la couleur émissive (texture RGB × couleur × intensité)
                half3 emissive = texCol.rgb * _EmissiveColor.rgb * _EmissiveIntensity;

                // 3) Récupère l’alpha de la texture pour le blending
                half alpha = texCol.a;

                // 4) Retourne la couleur émissive avec l’alpha
                return half4(emissive, alpha);
            }
            ENDHLSL
        }
        }
}
