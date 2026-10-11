Shader "Game/Hidden"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Hidden"
            Tags { "LightMode" = "GameHidden" }

            ColorMask 0
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 Fragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
