Shader "Custom/EnemyConeTelegraph_URP"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.2, 0.2, 0.45)
        _FillColor("Fill Color", Color) = (1, 1, 1, 0.45)
        _Range("Range", Float) = 5
        _HalfAngleDeg("Half Angle (Deg)", Range(1, 89)) = 30
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
                // Local XZ plane: +Z is forward direction of the telegraph object
                float2 p = IN.positionOS.xz;
                float dist = length(p);

                // Base cone stays visible at full range.
                float baseEdge = min(_EdgeSoftness, max(_Range, 0.001));
                float radialBase = 1.0 - smoothstep(_Range - baseEdge, _Range, dist);

                // Angular mask using dot against forward (0,1) in XZ
                float2 dir = normalize(p + 1e-6);
                float cosHalf = cos(radians(_HalfAngleDeg));
                float cosAng = dot(dir, float2(0, 1));

                // Soft angular edge
                float angSoft = max(_EdgeSoftness / max(_Range, 0.001), 0.001);
                float angular = smoothstep(cosHalf - angSoft, cosHalf, cosAng);

                float baseMask = saturate(radialBase * angular);

                // Fill grows from center to max range as _Fill goes 0..1.
                // Hard leading edge to avoid blinking at low fill values.
                float fillRange = _Range * saturate(_Fill);
                float radialFill = 1.0 - step(fillRange, dist);
                radialFill *= step(0.0001, fillRange);
                float fillMask = saturate(radialFill * angular);

                float baseAlpha = saturate(_Color.a * baseMask);
                float fillAlpha = saturate(_FillColor.a * fillMask);

                // Compose fill over base in a single pass (source-over alpha blend).
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