Shader "Custom/EnemyDirectionalTelegraph_URP"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.65, 0.2, 0.45)
        _FillColor("Fill Color", Color) = (1, 0.9, 0.55, 0.45)
        _Range("Range", Float) = 6
        _HalfAngleDeg("Half Angle (Deg)", Range(1, 89)) = 14
        _EdgeSoftness("Edge Softness", Float) = 0.25
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
                // Rectangle in local XZ: +Z is forward (length), X is width.
                float x = IN.positionOS.x;
                float z = IN.positionOS.z;

                float clampedRange = max(_Range, 0.001);
                float halfWidth = tan(radians(_HalfAngleDeg)) * clampedRange;
                halfWidth = max(halfWidth, 0.001);
                float soft = max(_EdgeSoftness, 0.0005);

                float xMask = smoothstep(halfWidth + soft, halfWidth - soft, abs(x));
                float zFront = smoothstep(-soft, soft, z);
                float zBack = smoothstep(clampedRange + soft, clampedRange - soft, z);
                float zMask = saturate(zFront * zBack);
                float baseMask = saturate(xMask * zMask);

                float fillRange = clampedRange * saturate(_Fill);
                float fillBack = smoothstep(fillRange + soft, fillRange - soft, z);
                float fillMask = saturate(xMask * zFront * fillBack);

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