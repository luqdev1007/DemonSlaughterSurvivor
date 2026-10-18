Shader "Game/BloodDecal"
{
    Properties
    {
        _MainTex ("Field Atlas (R)", 2D) = "black" {}
        _WetColor ("Wet", Color) = (0.32, 0.0, 0.01, 1)
        _CoreColor ("Core", Color) = (0.48, 0.01, 0.02, 1)
        _DryColor ("Dry", Color) = (0.14, 0.02, 0.01, 1)
        _RimDarken ("Rim Darken", Range(0, 1)) = 0.55
        _EdgeSoftness ("Edge Softness", Range(0.005, 0.3)) = 0.06
        _Sheen ("Sheen", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "BloodDecal"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _WetColor;
                half4 _CoreColor;
                half4 _DryColor;
                half _RimDarken;
                half _EdgeSoftness;
                half _Sheen;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half field = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).r;
                half spread = input.color.r;
                half dryness = input.color.g;
                half threshold = 1.0h - spread;

                half mask = smoothstep(threshold, threshold + _EdgeSoftness, field);
                half depth = saturate((field - threshold) / max(spread, 0.05h));

                half3 color = lerp(_WetColor.rgb, _CoreColor.rgb, depth);
                color *= lerp(1.0h - _RimDarken, 1.0h, smoothstep(0.0h, 0.25h, depth));
                color = lerp(color, _DryColor.rgb, dryness);

                Light mainLight = GetMainLight();
                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half spec = pow(saturate(halfDir.y), 48.0h) * _Sheen * (1.0h - dryness) * smoothstep(0.1h, 0.6h, depth);
                color = color * (0.55h + 0.45h * mainLight.color) + spec * mainLight.color;

                return half4(color, mask * input.color.a);
            }
            ENDHLSL
        }
    }
}
