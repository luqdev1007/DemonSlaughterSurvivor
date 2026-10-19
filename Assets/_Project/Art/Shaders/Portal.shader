Shader "Game/Portal"
{
    Properties
    {
        [HDR] _CoreColor("Core Color", Color) = (2.6, 0.55, 0.1, 1)
        [HDR] _RimColor("Rim Color", Color) = (1.4, 0.08, 0.04, 1)
        _DarkColor("Depth Color", Color) = (0.05, 0.0, 0.02, 1)
        _SwirlSpeed("Swirl Speed", Float) = 0.25
        _SwirlTwist("Swirl Twist", Float) = 3.5
        _NoiseScale("Noise Scale", Float) = 4
        _Softness("Edge Softness", Range(0.01, 0.5)) = 0.18
        _Intensity("Intensity", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Portal"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _RimColor;
                half4 _DarkColor;
                float _SwirlSpeed;
                float _SwirlTwist;
                float _NoiseScale;
                half _Softness;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x), lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;

                for (int octave = 0; octave < 4; octave++)
                {
                    sum += ValueNoise(p) * amplitude;
                    p = p * 2.02 + 7.3;
                    amplitude *= 0.5;
                }

                return sum;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                centered.y = centered.y * 1.15 + 0.15;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);

                float swirl = angle + _SwirlTwist * (1.0 - radius) + _Time.y * _SwirlSpeed * 6.2831;
                float2 polar = float2(cos(swirl), sin(swirl)) * radius * _NoiseScale;
                float noise = Fbm(polar + float2(0.0, -_Time.y * 0.6));

                float shape = 1.0 - smoothstep(1.0 - _Softness, 1.0, radius + (noise - 0.5) * 0.25);
                float rim = smoothstep(0.55, 0.95, radius + (noise - 0.5) * 0.3) * shape;
                float core = pow(saturate(noise * 1.4 - radius * 0.5), 2.0);

                half3 color = _DarkColor.rgb;
                color = lerp(color, _RimColor.rgb, rim);
                color += _CoreColor.rgb * core;

                return half4(color * _Intensity, shape);
            }
            ENDHLSL
        }
    }
}
