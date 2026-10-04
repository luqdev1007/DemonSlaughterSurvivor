Shader "Game/Gem"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.2, 0.45, 1, 1)
        [HDR] _EmissionColor("Emission", Color) = (0.12, 0.27, 0.6, 1)
        _PulseAmount("Emission Pulse Amount", Range(0, 1)) = 0.35
        _PulsePeriod("Emission Pulse Period (s)", Float) = 1.6
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _ShimmerStrength("Shimmer Strength", Range(0, 4)) = 1.2
        _HueRange("Shimmer Hue Range", Range(0, 0.2)) = 0.06
        _ShimmerPeriod("Shimmer Period (s)", Float) = 2.5
        _ShimmerFrequency("Shimmer Bands per Metre", Float) = 4
        _SpinPeriod("Spin Period (s)", Float) = 2.5
        _HoverHeight("Hover Height (m)", Float) = 0.35
        _BobAmplitude("Bob Amplitude (m)", Float) = 0.06
        _BobPeriod("Bob Period (s)", Float) = 1.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _EmissionColor;
            float _PulseAmount;
            float _PulsePeriod;
            float _FresnelPower;
            float _ShimmerStrength;
            float _HueRange;
            float _ShimmerPeriod;
            float _ShimmerFrequency;
            float _SpinPeriod;
            float _HoverHeight;
            float _BobAmplitude;
            float _BobPeriod;
        CBUFFER_END

        float ObjectPhase()
        {
            float3 origin = GetObjectToWorldMatrix()._m03_m13_m23;
            return frac(dot(origin.xz, float2(0.3719, 0.7123)) + origin.y * 0.1931) * TWO_PI;
        }

        float3 Spin(float3 value)
        {
            float angle = _Time.y * TWO_PI / max(_SpinPeriod, 0.01);
            float s = sin(angle);
            float c = cos(angle);
            return float3(value.x * c - value.z * s, value.y, value.x * s + value.z * c);
        }

        float3 AnimatedPositionWS(float3 positionOS)
        {
            float3 origin = GetObjectToWorldMatrix()._m03_m13_m23;
            float3 positionWS = origin + Spin(TransformObjectToWorld(positionOS) - origin);
            float bob = _HoverHeight + _BobAmplitude * sin(_Time.y * TWO_PI / max(_BobPeriod, 0.01) + ObjectPhase());
            positionWS.y += bob;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = AnimatedPositionWS(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = Spin(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight();
                half3 direct = LightingLambert(mainLight.color, mainLight.direction, normalWS) * mainLight.distanceAttenuation;
                half3 ambient = SampleSH(normalWS);
                half3 lit = _BaseColor.rgb * (direct + ambient);

                float phase = ObjectPhase();
                float pulse = 1.0 + _PulseAmount * sin(_Time.y * TWO_PI / max(_PulsePeriod, 0.01) + phase);
                half3 emission = _EmissionColor.rgb * pulse;

                half fresnel = pow(1.0 - saturate(dot(normalWS, viewWS)), _FresnelPower);
                float wave = sin(TWO_PI * (_Time.y / max(_ShimmerPeriod, 0.01) + input.positionWS.y * _ShimmerFrequency) + phase);
                half3 hsv = RgbToHsv(_BaseColor.rgb);
                hsv.x = frac(hsv.x + _HueRange * wave);
                hsv.y = saturate(hsv.y * 0.85);
                hsv.z = 1.0;
                half3 shimmer = HsvToRgb(hsv) * fresnel * _ShimmerStrength;

                return half4(lit + emission + shimmer, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(AnimatedPositionWS(input.positionOS.xyz));
                return output;
            }

            half Fragment(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
}
