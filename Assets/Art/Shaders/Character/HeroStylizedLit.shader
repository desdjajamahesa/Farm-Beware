Shader "FarmBeware/Character/HeroStylizedLit"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.35
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0

        [Header(Stylized Silhouette Rim)]
        [HDR] _FresnelColor ("Fresnel Rim Color", Color) = (1.6, 1.2, 0.6, 1.0)
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 5.0)) = 1.8

        [Header(Combat Feedback)]
        [HDR] _EmissionColor ("Base Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _HitFlashAmount ("Hit Flash Amount", Range(0.0, 1.0)) = 0.0
        [HDR] _HitFlashColor ("Hit Flash Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _EmissionColor;
            float _FresnelPower;
            float _Smoothness;
            float _Metallic;
        CBUFFER_END

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _FresnelColor)
            UNITY_DEFINE_INSTANCED_PROP(float4, _HitFlashColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _FresnelIntensity)
            UNITY_DEFINE_INSTANCED_PROP(float, _HitFlashAmount)
        UNITY_INSTANCING_BUFFER_END(Props)

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        inline float3 CalculateHeroFresnel(float3 normalWS, float3 viewDirWS)
        {
            float NdotV = saturate(dot(normalize(normalWS), normalize(viewDirWS)));
            float rim = pow(saturate(1.0 - NdotV), _FresnelPower);
            float intensity = UNITY_ACCESS_INSTANCED_PROP(Props, _FresnelIntensity);
            float3 rimColor = UNITY_ACCESS_INSTANCED_PROP(Props, _FresnelColor).rgb;
            return rimColor * (rim * intensity);
        }
        ENDHLSL

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
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = albedoTex.rgb * _BaseColor.rgb;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Prepare InputData for URP clustered lighting and shadows
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                // Main Directional Light
                Light mainLight = GetMainLight(inputData.shadowCoord);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 directLight = albedo * mainLight.color * (NdotL * mainLight.shadowAttenuation);

                // Additional Lights (Clustered loop)
            #if defined(_ADDITIONAL_LIGHTS)
                uint lightsCount = GetAdditionalLightsCount();
                uint meshRenderingLayers = GetMeshRenderingLayer();

                LIGHT_LOOP_BEGIN(lightsCount)
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS, inputData.shadowMask);
                #if defined(_LIGHT_LAYERS)
                    if (IsMatchingLightLayer(addLight.layerMask, meshRenderingLayers))
                #endif
                    {
                        float addNdotL = saturate(dot(normalWS, addLight.direction));
                        directLight += albedo * addLight.color * (addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation);
                    }
                LIGHT_LOOP_END
            #endif

                // Ambient Spherical Harmonics
                float3 ambient = SampleSH(normalWS) * albedo;

                // Stylized Hero Silhouette Rim
                float3 heroRim = CalculateHeroFresnel(normalWS, viewDirWS);

                // Combined Color
                float3 finalColor = directLight + ambient + _EmissionColor.rgb + heroRim;

                // Hit Flash Blend
                float hitFlash = UNITY_ACCESS_INSTANCED_PROP(Props, _HitFlashAmount);
                float3 hitColor = UNITY_ACCESS_INSTANCED_PROP(Props, _HitFlashColor).rgb;
                finalColor = lerp(finalColor, hitColor, hitFlash);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

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
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
