Shader "Game/UI/DualityOrb"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DemonDeep ("Demon Deep", Color) = (0.12, 0.0, 0.08, 1)
        _DemonHot ("Demon Hot", Color) = (0.75, 0.04, 0.12, 1)
        _DemonEmber ("Demon Ember", Color) = (1.0, 0.35, 0.1, 1)
        _HumanDeep ("Human Deep", Color) = (0.18, 0.3, 0.55, 1)
        _HumanLight ("Human Light", Color) = (0.85, 0.82, 0.6, 1)
        _HumanGlow ("Human Glow", Color) = (1.0, 0.97, 0.85, 1)
        _EmptyColor ("Empty", Color) = (0.03, 0.02, 0.04, 0.85)
        _SeamColor ("Seam", Color) = (0.9, 0.75, 0.45, 1)
        _NoiseScale ("Noise Scale", Float) = 4.0
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _Demonic ("Demonic", Range(0, 1)) = 0.5
        _Human ("Human", Range(0, 1)) = 0.5
        _DemonForm ("Demon Form", Range(0, 1)) = 0
        _HumanForm ("Human Form", Range(0, 1)) = 0
        _Threshold ("Threshold", Range(0, 1)) = 0.6
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
            fixed4 _DemonDeep;
            fixed4 _DemonHot;
            fixed4 _DemonEmber;
            fixed4 _HumanDeep;
            fixed4 _HumanLight;
            fixed4 _HumanGlow;
            fixed4 _EmptyColor;
            fixed4 _SeamColor;
            float _NoiseScale;
            float _FlowSpeed;
            float _Demonic;
            float _Human;
            float _DemonForm;
            float _HumanForm;
            float _Threshold;
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

            float3 demon(float2 uv, float t)
            {
                float2 p = uv * _NoiseScale;
                float2 warp = float2(fbm(p + float2(0, -t * 1.3)), fbm(p + float2(3.1, 7.7) + float2(t * 0.4, 0)));
                float n = fbm(p + warp * 2.2 + float2(-t * 0.2, -t));

                float veins = pow(saturate(1.0 - abs(n - 0.5) * 5.0), 4.0);
                float3 color = lerp(_DemonDeep.rgb, _DemonHot.rgb, saturate(n * 1.6 - 0.35));
                color += _DemonEmber.rgb * veins * (0.5 + _DemonForm * 1.2);
                return color;
            }

            float3 human(float2 uv, float t)
            {
                float2 p = uv * _NoiseScale * 0.7;
                float2 swirl = float2(fbm(p + float2(t * 0.3, t * 0.2)), fbm(p + float2(4.3, 1.9) - float2(t * 0.25, 0)));
                float n = fbm(p + swirl * 1.4 + float2(0, t * 0.35));

                float3 color = lerp(_HumanDeep.rgb, _HumanLight.rgb, saturate(n * 1.5 - 0.1));

                float2 grid = uv * 30.0 + float2(0, t * 1.5);
                float2 cell = floor(grid);
                float spark = hash(cell);
                float bead = saturate(1.0 - length(frac(grid) - 0.5) * 4.0);
                float twinkle = step(0.93, spark) * bead * (0.5 + 0.5 * sin(_Clock * 4.0 + spark * 40.0));
                color += _HumanGlow.rgb * twinkle * (0.35 + _HumanForm * 0.6);
                color += _HumanGlow.rgb * pow(saturate(n), 3.0) * (0.25 + _HumanForm * 0.7);
                return color;
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

                float seamX = 0.5 + sin(uv.y * 9.0 + _Clock * 1.3) * 0.006;
                bool left = uv.x < seamX;

                float level = left ? _Demonic : _Human;
                float form = left ? _DemonForm : _HumanForm;
                float wave = sin(uv.x * 22.0 + _Clock * (left ? 3.1 : -2.4)) * 0.008 + sin(uv.x * 9.0 - _Clock * 1.7) * 0.006;
                float surfaceY = level + wave;
                float filled = smoothstep(surfaceY + 0.004, surfaceY - 0.004, uv.y);

                float3 liquid = left ? demon(uv, t * (1.0 + form)) : human(uv, t * (1.0 + form * 0.6));
                float pulse = 0.5 + 0.5 * sin(_Clock * (left ? 6.5 : 3.5));
                liquid *= 1.0 + form * (0.35 + 0.25 * pulse);

                float3 surfaceTint = left ? _DemonEmber.rgb : _HumanGlow.rgb;
                float surface = saturate(1.0 - abs(uv.y - surfaceY) * 60.0);
                liquid += surfaceTint * surface * 0.7;

                float3 color = lerp(_EmptyColor.rgb, liquid, filled);
                float alpha = lerp(_EmptyColor.a, 1.0, filled);

                float marker = saturate(1.0 - abs(uv.y - _Threshold) * 220.0) * step(0.5, frac(uv.x * 14.0));
                color += surfaceTint * marker * 0.35;

                float seamDistance = abs(uv.x - seamX);
                float seam = saturate(1.0 - seamDistance * 140.0);
                float seamGlow = saturate(1.0 - seamDistance * 35.0);
                color = lerp(color, float3(0.02, 0.01, 0.02), seam * 0.9);
                color += _SeamColor.rgb * seamGlow * (1.0 - seam) * 0.35;
                alpha = max(alpha, seam);

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
