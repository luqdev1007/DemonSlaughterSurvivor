Shader "Game/UI/RageOrb"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DeepColor ("Deep", Color) = (0.45, 0.02, 0.0, 1)
        _HotColor ("Hot", Color) = (1.0, 0.55, 0.08, 1)
        _CoreColor ("Core", Color) = (1.0, 0.95, 0.6, 1)
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _NoiseScale ("Noise Scale", Float) = 4.0
        _Full ("Full", Range(0, 1)) = 0
        _FullBoost ("Full Boost", Float) = 1.8
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
            fixed4 _DeepColor;
            fixed4 _HotColor;
            fixed4 _CoreColor;
            float _FlowSpeed;
            float _NoiseScale;
            float _Full;
            float _FullBoost;
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
                float t = _Clock * _FlowSpeed * (1.0 + _Full * 1.5);

                float2 p = i.uv * _NoiseScale;
                float2 warp = float2(fbm(p + float2(0, -t)), fbm(p + float2(5.2, 1.3) + float2(t * 0.7, 0)));
                float n = fbm(p + warp * 1.8 + float2(t * 0.3, -t * 1.2));

                float veins = pow(saturate(1.0 - abs(n - 0.5) * 4.0), 3.0);
                float3 lava = lerp(_DeepColor.rgb, _HotColor.rgb, saturate(n * 1.4 - 0.15));
                lava += _CoreColor.rgb * veins * (0.35 + _Full * 0.9);

                float surface = saturate(1.0 - abs(i.uv.y - _Level) * 18.0) * (0.5 + 0.5 * sin(i.uv.x * 20.0 + t * 8.0));
                lava += _CoreColor.rgb * surface * 0.6 * (1.0 - _Full);

                float pulse = 0.5 + 0.5 * sin(_Clock * 6.0);
                lava *= 1.0 + _Full * (_FullBoost - 1.0) * (0.6 + 0.4 * pulse);

                float rim = saturate(length(i.uv - 0.5) * 2.0);
                lava += _HotColor.rgb * pow(rim, 3.0) * _Full * 0.8;

                #if !defined(UNITY_COLORSPACE_GAMMA)
                lava = GammaToLinearSpace(saturate(lava)) * max(1.0, max(lava.r, max(lava.g, lava.b)));
                #endif

                fixed4 color = fixed4(lava * i.color.rgb, sprite.a * i.color.a);
                color.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                return color;
            }
            ENDCG
        }
    }
}
