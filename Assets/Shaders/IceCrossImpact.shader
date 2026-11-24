Shader "Spell/Ice Cross Impact"
{
    Properties { [HDR] _Tint ("Ice tint", Color) = (0.35,1.1,1.5,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p = (i.uv * 2 - 1);
                p = float2(p.x+p.y,p.y-p.x)*0.707107;
                float bladeA = abs(p.y) / 0.09 + abs(p.x) / 0.96;
                float bladeB = abs(p.x) / 0.065 + abs(p.y) / 0.82;
                float blade = min(bladeA,bladeB);
                half edge = 1-smoothstep(0.8,1.05,blade);
                half core = 1-smoothstep(0.1,0.6,blade);
                half flare = pow(saturate(1-length(p)*5),3);
                half3 color = lerp(_Tint.rgb,half3(2,2.3,2.5),core);
                return half4(color*i.color.rgb, saturate(edge+flare)*i.color.a*_Tint.a);
            }
            ENDHLSL
        }
    }
}
