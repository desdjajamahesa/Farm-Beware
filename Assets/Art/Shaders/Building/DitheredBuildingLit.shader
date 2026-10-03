Shader "FarmBeware/Building/DitheredBuildingLit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _BaseMap ("Base Map", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.3
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0

        [Header(Dithered Transparency)]
        _DitherFade ("Dither Fade", Range(0.0, 1.0)) = 0.0

        [Header(Normal Map)]
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _BumpMap_ST;
            float _Smoothness;
            float _Metallic;
            float _DitherFade;
            float _BumpScale;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        // ──────────────────────────────────────────────────
        // Matriks Bayer 4×4 yang dinormalisasi ke [0,1).
        // Digunakan untuk operasi screen-door transparency:
        //   Bayer(x, y) = (1/16) * M[y][x]
        // di mana M adalah matriks threshold standar 4×4.
        // ──────────────────────────────────────────────────
        static const float BayerMatrix4x4[4][4] =
        {
            {  0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0 },
            { 12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0 },
            {  3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0 },
            { 15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0 }
        };

        // Fungsi dither: membandingkan _DitherFade dengan threshold Bayer
        // pada posisi piksel layar. Memanggil clip() untuk membuang piksel.
        //   _DitherFade = 0.0 → semua piksel lolos (solid penuh)
        //   _DitherFade = 1.0 → semua piksel dibuang (tembus pandang penuh)
        inline void ApplyBayerDither(float4 positionCS)
        {
            // Dapatkan koordinat piksel layar, lalu modulo 4 untuk index Bayer
            float2 screenUV = positionCS.xy;
            int2 coord = int2(fmod(abs(screenUV), 4.0));

            float ditherValue = BayerMatrix4x4[coord.y][coord.x];

            // clip( (1 - fade) - threshold )
            // Saat fade=0: clip(1 - threshold) → selalu positif → piksel lolos
            // Saat fade=1: clip(0 - threshold) → selalu negatif → piksel dibuang
            clip((1.0 - _DitherFade) - ditherValue);
        }
        ENDHLSL

        // ═══════════════════════════════════════════════════
        // Pass 1: ForwardLit — Rendering utama dengan PBR
        // ═══════════════════════════════════════════════════
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float4 tangentWS    : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // ── Dithered Alpha Clipping ──
                ApplyBayerDither(input.positionCS);

                // ── Sampling Albedo ──
                float4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = albedoTex.rgb * _BaseColor.rgb;

                // ── Normal Mapping ──
                float3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 bitangentWS = cross(normalize(input.normalWS),
                    normalize(input.tangentWS.xyz)) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(
                    normalize(input.tangentWS.xyz), bitangentWS, normalize(input.normalWS));
                float3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // ── Directional Light Utama ──
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 directLight = albedo * mainLight.color *
                    (NdotL * mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                // ── Ambient SH ──
                float3 ambient = SampleSH(normalWS) * albedo;

                float3 finalColor = directLight + ambient;
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════════
        // Pass 2: ShadowCaster — KUNCI: menerapkan clip()
        // yang sama agar bayangan konsisten dengan tampilan
        // dithered di ForwardLit. Saat atap di-dither transparan,
        // bayangan siluet fasad tetap terpancar ke pekarangan.
        // ═══════════════════════════════════════════════════
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            float4 ShadowFrag(Varyings input) : SV_Target
            {
                ApplyBayerDither(input.positionCS);
                return 0;
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════════
        // Pass 3: DepthOnly — Diperlukan oleh URP Deferred+
        // untuk membuat depth prepass yang akurat
        // ═══════════════════════════════════════════════════
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float4 positionOS   : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS   : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 DepthFrag(DepthVaryings input) : SV_Target
            {
                ApplyBayerDither(input.positionCS);
                return 0;
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════════
        // Pass 4: DepthNormals — Digunakan oleh SSAO dan efek
        // screen-space lain yang memerlukan normal + depth
        // ═══════════════════════════════════════════════════
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            struct DNAttributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct DNVaryings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
            };

            DNVaryings DepthNormalsVert(DNAttributes input)
            {
                DNVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 DepthNormalsFrag(DNVaryings input) : SV_Target
            {
                ApplyBayerDither(input.positionCS);
                float3 normalWS = normalize(input.normalWS);
                return float4(normalWS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
