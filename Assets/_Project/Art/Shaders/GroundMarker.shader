Shader "Game/GroundMarker"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.2, 0.1, 0.5)
        _Shape("Shape (0 strip, 1 disc)", Float) = 0
        _Pulse("Pulse", Float) = 0
        _PulseRing("Pulse Ring Radius", Float) = 0
        _EdgeWidth("Edge Width", Float) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-10"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Shape;
                half _Pulse;
                half _PulseRing;
                half _EdgeWidth;
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

            half Strip(float2 uv)
            {
                half across = abs(uv.x * 2.0h - 1.0h);
                half edge = smoothstep(1.0h - _EdgeWidth * 2.0h, 1.0h - _EdgeWidth * 0.5h, across);
                half tip = smoothstep(1.0h, 0.85h, uv.y);
                half fill = lerp(0.35h, 0.6h, uv.y);
                return saturate(fill + edge) * tip;
            }

            half Disc(float2 uv)
            {
                half radius = length(uv * 2.0h - 1.0h);
                half inside = 1.0h - smoothstep(0.98h, 1.0h, radius);
                half gradient = lerp(0.85h, 0.25h, radius);
                half rim = smoothstep(1.0h - _EdgeWidth, 1.0h - _EdgeWidth * 0.25h, radius);
                half wave = saturate(1.0h - abs(radius - _PulseRing) / max(_EdgeWidth, 0.01h)) * _Pulse;
                return saturate(gradient + rim + wave) * inside;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half shape = _Shape > 0.5h ? Disc(input.uv) : Strip(input.uv);
                half alpha = saturate(_Color.a * shape * (1.0h + _Pulse * 0.5h));
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
