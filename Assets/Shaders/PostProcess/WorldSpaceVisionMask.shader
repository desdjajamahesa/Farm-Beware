Shader "PostProcess/WorldSpaceVisionMask"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _VisionNightColor ("Night Tint Color", Color) = (0.35, 0.45, 0.70, 1.0)
        _VisionCenterBoost ("Center Brightness Boost", Float) = 0.35
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        LOD 100
        ZTest Always 
        ZWrite Off 
        Cull Off

        Pass
        {
            Name "WorldSpaceVisionMaskPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // URP required includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Global parameters controlled via Shader.SetGlobalVector / SetGlobalFloat / SetGlobalColor
            CBUFFER_START(VisionMaskGlobals)
                float3 _VisionWorldPos;
                float _VisionRadius;
                float _VisionSmoothness;
                float _VisionDarkness;
                float _VisionSaturation;
                float _VisionBlend;
                float _VisionCenterBoost;
                half4 _VisionNightColor;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                
                // Sample original camera color
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Sample raw depth from camera depth texture
                float depth = SampleSceneDepth(uv);

                // Reconstruct world space position from depth using inverse View-Projection matrix
                // Works for both Orthographic and Perspective cameras
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                // Horizontal (Cylindrical / XZ-Plane) distance to avoid isometric elevation distortion
                float2 deltaXZ = worldPos.xz - _VisionWorldPos.xz;
                float dist = length(deltaXZ);

                // Transition with smoothstep falloff
                float innerEdge = max(0.0, _VisionRadius - _VisionSmoothness);
                float mask = 1.0 - smoothstep(innerEdge, _VisionRadius, dist);

                // Standard luminance (Rec. 601 standard: dot(RGB, float3(0.299, 0.587, 0.114)))
                half luminance = dot(col.rgb, half3(0.299, 0.587, 0.114));
                half3 grayscale = half3(luminance, luminance, luminance);

                // Desaturated color controlled by _VisionSaturation
                half3 desaturated = lerp(grayscale, col.rgb, _VisionSaturation);

                // Outside radius: Aesthetic moonlight night tint combined with darkness
                half3 nightAmbient = _VisionNightColor.rgb * luminance * (_VisionDarkness * 2.5);
                half3 outerColor = lerp(desaturated * _VisionDarkness, nightAmbient, 0.4);

                // Inner brightness boost: Brightens character and immediate vicinity during night
                float centerFactor = 1.0 - saturate(dist / max(0.001, _VisionRadius * 0.5));
                half3 boostedColor = col.rgb * (1.0 + (_VisionCenterBoost * centerFactor));
                half3 innerColor = lerp(col.rgb, boostedColor, _VisionBlend);

                // Blend inside vision (boosted color) and outside vision (night tinted + darkness)
                half3 maskedRgb = lerp(outerColor, innerColor, mask);

                // Day / Night transition blend (_VisionBlend: 0 = daytime full color, 1 = nighttime vision mask)
                half3 finalRgb = lerp(col.rgb, maskedRgb, _VisionBlend);

                return half4(finalRgb, col.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
