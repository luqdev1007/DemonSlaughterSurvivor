Shader "Game/UI/BloodOrb"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DeepColor ("Deep", Color) = (0.32, 0.01, 0.03, 1)
        _BloodColor ("Blood", Color) = (0.86, 0.05, 0.08, 1)
        _BrightColor ("Bright", Color) = (1.0, 0.36, 0.32, 1)
        _TrailColor ("Damage Trail", Color) = (1.0, 0.85, 0.75, 0.55)
        _FlowSpeed ("Flow Speed", Float) = 0.12
        _NoiseScale ("Noise Scale", Float) = 3.0
        _WaveHeight ("Wave Height", Float) = 0.018
        _Level ("Level", Range(0, 1)) = 1
        _Trail ("Trail", Range(0, 1)) = 1
        _Slosh ("Slosh", Float) = 0
        _Low ("Low", Range(0, 1)) = 0
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
            fixed4 _DeepColor;
            fixed4 _BloodColor;
            fixed4 _BrightColor;
            fixed4 _TrailColor;
            float _FlowSpeed;
            float _NoiseScale;
            float _WaveHeight;
            float _Level;
            float _Trail;
            float _Slosh;
            float _Low;
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
                    p = p * 2.07 + float2(3.1, 7.7);
                    a *= 0.5;
                }
                return v;
            }

            float bubbles(float2 uv, float t)
            {
                float result = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float2 grid = float2(6.0 + k * 2.0, 4.0 + k);
                    float2 p = uv * grid + float2(k * 0.37, -t * (0.25 + k * 0.08));
                    float2 cell = floor(p);
                    float2 local = frac(p) - 0.5;
                    float seed = hash(cell + k * 17.0);
                    float2 offset = (float2(hash(cell + 3.1), hash(cell + 5.7)) - 0.5) * 0.5;
                    float radius = lerp(0.05, 0.12, seed) * step(0.72, seed);
                    float d = length(local - offset);
                    result += smoothstep(radius, radius * 0.4, d) * (0.6 + 0.4 * smoothstep(radius * 0.9, radius * 0.5, d));
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
                float2 uv = i.uv;
                float t = _Clock;

                float wave = sin(uv.x * 9.0 + t * 2.3) * 0.6 + sin(uv.x * 17.0 - t * 3.1) * 0.4;
                float tilt = (uv.x - 0.5) * _Slosh;
                float surface = _Level + wave * _WaveHeight * (0.4 + saturate(abs(_Slosh) * 8.0)) + tilt;
                float trailSurface = max(_Trail, _Level) + wave * _WaveHeight * 0.5;

                float aa = fwidth(uv.y) * 1.5;
                float liquid = smoothstep(surface + aa, surface - aa, uv.y);
                float trail = smoothstep(trailSurface + aa, trailSurface - aa, uv.y) * (1.0 - liquid);

                float2 p = uv * _NoiseScale;
                float flow = t * _FlowSpeed;
                float2 warp = float2(fbm(p + float2(flow, -flow * 0.6)), fbm(p + float2(4.3, 2.8) - float2(flow * 0.8, flow * 0.3)));
                float n = fbm(p + warp * 2.2 + float2(-flow * 0.5, flow));

                float depth = saturate(1.0 - (surface - uv.y) * 0.9);
                float3 blood = lerp(_DeepColor.rgb, _BloodColor.rgb, saturate(n * 1.3 - 0.1));
                blood = lerp(_DeepColor.rgb, blood, 0.7 + 0.3 * depth);

                float streaks = pow(saturate(1.0 - abs(n - 0.55) * 5.0), 4.0);
                blood += _BrightColor.rgb * streaks * 0.35;

                float2 c = uv - 0.5;
                float r = saturate(length(c) * 2.0);
                float3 normal = normalize(float3(c * 2.0, sqrt(saturate(1.0 - r * r)) + 0.15));
                float3 lightDir = normalize(float3(-0.45, 0.6, 0.65));
                float diffuse = saturate(dot(normal, lightDir));
                blood *= 0.65 + 0.6 * diffuse;
                blood *= 1.0 - pow(r, 5.0) * 0.45;
                blood += _BrightColor.rgb * pow(r, 6.0) * 0.25;

                float3 halfDir = normalize(lightDir + float3(0, 0, 1));
                float spec = pow(saturate(dot(normal, halfDir)), 28.0);
                blood += spec * float3(1.0, 0.8, 0.75) * 0.7;
                float sheen = pow(saturate(dot(normal, normalize(float3(0.5, 0.2, 0.85)))), 12.0);
                blood += sheen * _BrightColor.rgb * 0.18;

                float meniscus = smoothstep(0.03, 0.0, surface - uv.y) * liquid;
                blood = lerp(blood, _BrightColor.rgb * 1.2, meniscus * 0.65);

                float bubble = bubbles(uv, t) * liquid * smoothstep(0.0, 0.05, surface - uv.y);
                blood += _BrightColor.rgb * bubble * 0.45;

                float pulse = 0.5 + 0.5 * sin(t * 7.0);
                blood *= 1.0 + _Low * pulse * 0.6;

                float3 color = blood * liquid + _TrailColor.rgb * trail;
                float alpha = saturate(liquid + trail * _TrailColor.a);

                #if !defined(UNITY_COLORSPACE_GAMMA)
                color = GammaToLinearSpace(saturate(color)) * max(1.0, max(color.r, max(color.g, color.b)));
                #endif

                fixed4 result = fixed4(color * i.color.rgb, alpha * sprite.a * i.color.a);
                result.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                return result;
            }
            ENDCG
        }
    }
}
