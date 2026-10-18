Shader "Game/UI/ExperienceBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DeepColor ("Deep", Color) = (0.03, 0.22, 0.42, 1)
        _MainColor ("Main", Color) = (0.12, 0.62, 0.95, 1)
        _GlowColor ("Glow", Color) = (0.7, 0.95, 1.0, 1)
        _BarWidth ("Bar Width (px)", Float) = 680
        _StreakScale ("Streak Scale (px)", Float) = 46
        _StreakSpeed ("Streak Speed (px/s)", Float) = 22
        _SweepPeriod ("Sweep Period (s)", Float) = 5
        _SweepWidth ("Sweep Width (px)", Float) = 90
        _EdgeWidth ("Edge Width (uv)", Float) = 0.05
        _Flash ("Flash", Range(0, 1)) = 0
        _Clock ("Clock", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 local : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _DeepColor;
            fixed4 _MainColor;
            fixed4 _GlowColor;
            float _BarWidth;
            float _StreakScale;
            float _StreakSpeed;
            float _SweepPeriod;
            float _SweepWidth;
            float _EdgeWidth;
            float _Flash;
            float _Clock;
            float4 _ClipRect;

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.local = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Clock;
                float2 px = i.local.xy;
                float v = i.uv.y;

                float3 color = lerp(_DeepColor.rgb, _MainColor.rgb, smoothstep(0.0, 0.85, v));

                float2 flow = float2(px.x - t * _StreakSpeed + px.y * 1.2, px.y * 0.35) / _StreakScale;
                float n = noise(flow) * 0.65 + noise(flow * 2.3 + 7.1) * 0.35;
                color *= 0.82 + 0.36 * n;

                float gloss = smoothstep(0.55, 0.8, v) * (1.0 - smoothstep(0.8, 1.0, v));
                color += _GlowColor.rgb * gloss * 0.35;
                color *= 0.75 + 0.25 * smoothstep(0.0, 0.25, v);

                float cycle = frac(t / max(_SweepPeriod, 0.01));
                float sweepX = lerp(-_SweepWidth * 2.0, _BarWidth + _SweepWidth * 2.0, saturate(cycle * 1.6));
                float slant = px.x - px.y * 0.8;
                float sweep = exp(-pow((slant - sweepX) / _SweepWidth, 2.0)) * step(cycle, 0.625);
                color += _GlowColor.rgb * sweep * 0.45;

                float edge = smoothstep(1.0 - _EdgeWidth, 1.0, i.uv.x);
                float flicker = 0.85 + 0.15 * sin(t * 9.0 + px.y * 0.3);
                color = lerp(color, _GlowColor.rgb * 1.3, edge * 0.8 * flicker);

                float sparkle = step(0.985, hash(floor(float2(px.x - t * _StreakSpeed * 1.6, px.y) / 3.0))) * (0.5 + 0.5 * sin(t * 6.0 + px.x));
                color += _GlowColor.rgb * sparkle * 0.5;

                color = lerp(color, float3(1, 1, 1), _Flash * 0.75);

                #if !defined(UNITY_COLORSPACE_GAMMA)
                color = GammaToLinearSpace(saturate(color)) * max(1.0, max(color.r, max(color.g, color.b)));
                #endif

                fixed4 result = fixed4(color * i.color.rgb, i.color.a * tex2D(_MainTex, i.uv).a);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
