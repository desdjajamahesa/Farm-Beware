Shader "FarmBeware/Combat/CombatTelegraph"
{
    Properties
    {
        [Header(Telegraph Colors)]
        [HDR] _OuterRingColor ("Outer Danger Ring Color", Color) = (1.6, 0.22, 0.1, 0.95)
        _FillColor ("Inner Charge Fill Color", Color) = (1.0, 0.15, 0.05, 0.30)

        [Header(Ring Dynamics)]
        _Progress ("Telegraph Progress (0 to 1)", Range(0.0, 1.0)) = 0.0
        _RingThickness ("Ring Thickness", Range(0.01, 0.25)) = 0.06
        _Feather ("Edge Feather Smoothing", Range(0.001, 0.05)) = 0.015
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "TelegraphUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Feather;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OuterRingColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FillColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Progress)
                UNITY_DEFINE_INSTANCED_PROP(float, _RingThickness)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 outerColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OuterRingColor);
                float4 fillColor = UNITY_ACCESS_INSTANCED_PROP(Props, _FillColor);
                float progress = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _Progress));
                float thickness = UNITY_ACCESS_INSTANCED_PROP(Props, _RingThickness);

                // Centered coordinates (-0.5 to +0.5)
                float2 p = input.uv - 0.5;
                float dist = length(p);

                // Discard outside unit circle radius 0.5
                float outerAlpha = 1.0 - smoothstep(0.5 - _Feather, 0.5, dist);
                if (outerAlpha <= 0.001)
                {
                    discard;
                }

                // 1. Outer Danger Ring (Fixed circumference boundary)
                float ringInnerEdge = 0.5 - thickness;
                float outerRingMask = smoothstep(ringInnerEdge - _Feather, ringInnerEdge, dist) * outerAlpha;

                // 2. Inner Expanding Progress Disk (0 -> ringInnerEdge)
                float currentFillRadius = progress * ringInnerEdge;
                float fillMask = 1.0 - smoothstep(currentFillRadius - _Feather, currentFillRadius, dist);

                // 3. Leading Glow Wave on the expanding progress frontier
                float rimWidth = 0.035;
                float leadingRim = smoothstep(currentFillRadius - rimWidth - _Feather, currentFillRadius - 0.005, dist)
                                 * (1.0 - smoothstep(currentFillRadius - 0.005, currentFillRadius + _Feather, dist));

                // 4. Critical Flash Cue when attack is >= 92% charged (imminent release)
                float criticalFlash = smoothstep(0.92, 1.0, progress) * 0.45;

                // Combine Color and Alpha
                float3 col = float3(0, 0, 0);
                float alpha = 0.0;

                // Add fill contribution
                col += fillColor.rgb * fillMask;
                alpha += (fillColor.a + criticalFlash * 0.4) * fillMask;

                // Add leading rim glow
                col += outerColor.rgb * (leadingRim * 1.5);
                alpha += leadingRim * 0.8;

                // Add outer ring with high emission
                col += outerColor.rgb * outerRingMask * (1.0 + criticalFlash);
                alpha = max(alpha, outerRingMask * outerColor.a);

                alpha = saturate(alpha * outerAlpha);

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
