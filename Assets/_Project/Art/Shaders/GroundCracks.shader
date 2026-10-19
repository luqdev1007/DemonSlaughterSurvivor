Shader "Game/GroundCracks"
{
    Properties
    {
        [HDR] _CrackColor("Crack Glow", Color) = (3.2, 0.7, 0.12, 1)
        [HDR] _HaloColor("Halo Glow", Color) = (0.35, 0.06, 0.01, 1)
        _CellSize("Crack Cell Size (m)", Float) = 1.4
        _Width("Crack Width", Range(0.005, 0.25)) = 0.03
        _HaloWidth("Halo Width", Range(0.01, 0.6)) = 0.16
        _BreakUpScale("Break-up Scale (m)", Float) = 7
        _BreakUp("Break-up Amount", Range(0, 1)) = 0.6
        _Warp("Line Warp (m)", Range(0, 2)) = 0.6
        _EdgeFade("Strip Edge Fade", Range(0.01, 0.5)) = 0.35
        _PulseSpeed("Pulse Speed (Hz)", Float) = 0.35
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-50"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Cracks"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CrackColor;
                half4 _HaloColor;
                float _CellSize;
                half _Width;
                half _HaloWidth;
                float _BreakUpScale;
                half _BreakUp;
                half _Warp;
                half _EdgeFade;
                half _PulseSpeed;
                half _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float2 Hash22(float2 p)
            {
                float3 a = frac(p.xyx * float3(123.34, 234.34, 345.65));
                a += dot(a, a + 34.45);
                return frac(float2(a.x * a.y, a.y * a.z));
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash22(i).x;
                float b = Hash22(i + float2(1, 0)).x;
                float c = Hash22(i + float2(0, 1)).x;
                float d = Hash22(i + float2(1, 1)).x;
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float VoronoiEdge(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                float first = 8.0;
                float second = 8.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 delta = offset + Hash22(cell + offset) - local;
                        float dist = dot(delta, delta);

                        if (dist < first)
                        {
                            second = first;
                            first = dist;
                        }
                        else if (dist < second)
                        {
                            second = dist;
                        }
                    }
                }

                return sqrt(second) - sqrt(first);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                float2 warp = float2(ValueNoise(p * 0.9), ValueNoise(p * 0.9 + 41.3)) - 0.5;
                float edge = VoronoiEdge((p + warp * _Warp * 2.0) / _CellSize);
                float core = 1.0 - smoothstep(0.0, _Width, edge);
                float halo = 1.0 - smoothstep(0.0, _HaloWidth, edge);

                float breakUp = smoothstep(_BreakUp - 0.15, _BreakUp + 0.15, ValueNoise(p / _BreakUpScale + 13.7));
                float across = min(input.uv.y, 1.0 - input.uv.y);
                float along = min(input.uv.x, 1.0 - input.uv.x);
                float fade = smoothstep(0.0, _EdgeFade, across) * smoothstep(0.0, 0.05, along);

                float phase = ValueNoise(p * 0.15) * 6.2831;
                float pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2831 + phase));

                half3 color = (_CrackColor.rgb * core + _HaloColor.rgb * halo) * breakUp * fade * pulse;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
