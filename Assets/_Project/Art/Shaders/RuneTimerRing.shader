Shader "Game/RuneTimerRing"
{
    Properties
    {
        _Glyph ("Glyph", 2D) = "black" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Fill ("Fill", Range(0, 1)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Inner ("Ring Inner Radius", Range(0, 0.5)) = 0.4
        _Outer ("Ring Outer Radius", Range(0, 0.5)) = 0.48
        _GlyphScale ("Glyph Scale", Range(0.1, 2)) = 0.95
        _GlyphStrength ("Glyph Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "RuneTimerRing"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Glyph);
            SAMPLER(sampler_Glyph);

            CBUFFER_START(UnityPerMaterial)
                float4 _Glyph_ST;
                half4 _Color;
                half _Fill;
                half _Alpha;
                half _Inner;
                half _Outer;
                half _GlyphScale;
                half _GlyphStrength;
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

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.uv - 0.5;
                float r = length(p);
                float aa = fwidth(r) * 1.5;

                half ring = smoothstep(_Inner - aa, _Inner + aa, r) * (1.0h - smoothstep(_Outer - aa, _Outer + aa, r));

                float angle = atan2(p.x, p.y) / (2.0 * PI);
                angle = frac(angle + 1.0);
                half filled = 1.0h - smoothstep(_Fill - 0.004h, _Fill + 0.004h, angle);
                half head = exp(-pow((angle - _Fill) * 60.0, 2.0)) * step(0.001h, _Fill);

                float2 glyphUv = p / _GlyphScale + 0.5;
                half glyph = SAMPLE_TEXTURE2D(_Glyph, sampler_Glyph, glyphUv).a;
                glyph *= step(abs(glyphUv.x - 0.5), 0.5) * step(abs(glyphUv.y - 0.5), 0.5);

                half intensity = ring * (0.18h + 0.82h * filled) + ring * head * 1.5h + glyph * _GlyphStrength;

                return half4(_Color.rgb * intensity, saturate(intensity) * _Alpha);
            }
            ENDHLSL
        }
    }
}
