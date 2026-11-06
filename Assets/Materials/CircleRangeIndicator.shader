Shader "Custom/EnemyCircleTelegraph_URP"
{
    Properties
    {
        _Color("Color", Color) = (0.2, 0.85, 0.45, 0.45)
        _FillColor("Fill Color", Color) = (0.65, 1, 0.8, 0.45)
        _Range("Range", Float) = 4
        _HalfAngleDeg("Half Angle (Deg)", Range(1, 89)) = 89
        _EdgeSoftness("Edge Softness", Float) = 0.35
        _Fill("Fill", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _FillColor;
                float _Range;
                float _HalfAngleDeg;
                float _EdgeSoftness;
                float _Fill;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.positionOS.xz;
                float dist = length(p);

                float baseEdge = min(_EdgeSoftness, max(_Range, 0.001));
                float baseMask = 1.0 - smoothstep(_Range - baseEdge, _Range, dist);

                float fillRange = _Range * saturate(_Fill);
                float radialFill = 1.0 - step(fillRange, dist);
                radialFill *= step(0.0001, fillRange);
                float fillMask = saturate(radialFill);

                float baseAlpha = saturate(_Color.a * baseMask);
                float fillAlpha = saturate(_FillColor.a * fillMask);

                float3 basePremul = _Color.rgb * baseAlpha;
                float3 fillPremul = _FillColor.rgb * fillAlpha;
                float3 outPremul = fillPremul + basePremul * (1.0 - fillAlpha);
                float outAlpha = saturate(fillAlpha + baseAlpha * (1.0 - fillAlpha));

                half4 col;
                col.rgb = outAlpha > 1e-5 ? outPremul / outAlpha : 0;
                col.a = outAlpha;
                return col;
            }
            ENDHLSL
        }
    }
}