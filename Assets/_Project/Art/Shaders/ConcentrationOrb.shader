Shader "Game/UI/ConcentrationOrb"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Deep ("Deep", Color) = (0.01, 0.04, 0.22, 1)
        _Mid ("Mid", Color) = (0.05, 0.28, 0.85, 1)
        _Bright ("Bright", Color) = (0.35, 0.85, 1.0, 1)
        _Glow ("Glow", Color) = (0.75, 0.97, 1.0, 1)
        _EmptyColor ("Empty", Color) = (0.02, 0.03, 0.07, 0.85)
        _NoiseScale ("Noise Scale", Float) = 3.5
        _FlowSpeed ("Flow Speed", Float) = 0.25
        _BubbleDensity ("Bubble Density", Float) = 9
        _Level ("Level", Range(0, 1)) = 1
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
                float4 world : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _Deep;
            fixed4 _Mid;
            fixed4 _Bright;
            fixed4 _Glow;
            fixed4 _EmptyColor;
            float _NoiseScale;
            float _FlowSpeed;
            float _BubbleDensity;
            float _Level;
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
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int o = 0; o < 4; o++)
                {
                    v += a * noise(p);
                    p = p * 2.03 + float2(1.7, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            float bubbles(float2 uv, float t)
            {
                float result = 0.0;

                for (int layer = 0; layer < 2; layer++)
                {
                    float density = _BubbleDensity * (1.0 + layer * 0.8);
                    float speed = 0.18 + layer * 0.1;
                    float2 grid = float2(uv.x * density, uv.y * density - t * speed * density);
                    float2 cell = floor(grid);
                    float seed = hash(cell + layer * 17.0);
                    float wobble = sin(t * 3.0 + seed * 31.0) * 0.18;
                    float2 center = float2(0.5 + (seed - 0.5) * 0.5 + wobble, 0.5);
                    float radius = lerp(0.08, 0.2, frac(seed * 7.3));
                    float d = length(frac(grid) - center);
                    float ring = smoothstep(radius, radius * 0.6, d) - smoothstep(radius * 0.6, radius * 0.2, d) * 0.6;
                    result += ring * step(0.62, seed);
                }

                return saturate(result);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, i.uv);
                float t = _Clock * _FlowSpeed;
                float2 uv = i.uv;
                float2 centered = uv - 0.5;
                float radial = saturate(length(centered) * 2.0);

                float wave = sin(uv.x * 18.0 + _Clock * 2.2) * 0.009 + sin(uv.x * 7.0 - _Clock * 1.4) * 0.007;
                float surfaceY = _Level * 1.02 - 0.01 + wave;
                float filled = smoothstep(surfaceY + 0.004, surfaceY - 0.004, uv.y);

                float2 p = uv * _NoiseScale;
                float2 swirl = float2(fbm(p + float2(t * 0.6, -t * 0.4)), fbm(p + float2(5.2, 1.3) + float2(-t * 0.3, t * 0.5)));
                float n = fbm(p + swirl * 1.8 + float2(0, -t));
                float caustic = pow(saturate(1.0 - abs(fbm(p * 1.7 + swirl * 2.5 - t * 0.8) - 0.5) * 6.0), 3.0);

                float depth = saturate((surfaceY - uv.y) / max(surfaceY, 0.05));
                float3 liquid = lerp(_Mid.rgb, _Deep.rgb, depth * 0.85);
                liquid = lerp(liquid, _Bright.rgb, saturate(n * 1.4 - 0.45) * 0.8);
                liquid += _Glow.rgb * caustic * 0.35 * (1.0 - depth * 0.6);
                liquid += _Glow.rgb * bubbles(uv, _Clock) * 0.55;

                float core = saturate(1.0 - radial * 1.15);
                liquid += _Bright.rgb * core * core * 0.25 * (0.85 + 0.15 * sin(_Clock * 1.6));
                liquid *= lerp(1.0, 0.55, radial * radial);

                float surface = saturate(1.0 - abs(uv.y - surfaceY) * 70.0);
                float foam = surface * (0.6 + 0.4 * noise(float2(uv.x * 40.0 + _Clock * 2.0, _Clock)));
                liquid += _Glow.rgb * foam * 0.9;

                float3 empty = _EmptyColor.rgb + _Mid.rgb * 0.06 * fbm(uv * 6.0 + t);
                float3 color = lerp(empty, liquid, filled);
                float alpha = lerp(_EmptyColor.a, 1.0, filled);

                #if !defined(UNITY_COLORSPACE_GAMMA)
                color = GammaToLinearSpace(saturate(color)) * max(1.0, max(color.r, max(color.g, color.b)));
                #endif

                fixed4 result = fixed4(color * i.color.rgb, sprite.a * alpha * i.color.a);
                result.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                return result;
            }
            ENDCG
        }
    }
}
