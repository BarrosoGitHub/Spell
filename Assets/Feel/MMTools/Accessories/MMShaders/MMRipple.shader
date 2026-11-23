Shader "MoreMountains/MMRipple"
{
    Properties
    {
        _RippleAlpha("Ripple Alpha", Float) = 1
        _RippleIntensity("Ripple Intensity", Float) = 1
        _Hue("Hue", Color) = (1, 1, 1, 1)
        _NormalMap("Normal Map", 2D) = "white" {}
        _Density("Soft Particles Factor", Range(0, 3)) = 1
        [Toggle(_SOFTPARTICLES_ON)] _SoftParticlesEnabled("Soft Particles (requires feature depth input)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "MMRipple"
            // Only MMRippleRendererFeature draws this pass, after capturing transparents.
            // A normal UniversalForward/Universal2D pass would draw too early.
            Tags { "LightMode" = "MMRipple" }
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex RippleVertex
            #pragma fragment RippleFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _SOFTPARTICLES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_MMRippleGrabTexture);
            SAMPLER(sampler_MMRippleGrabTexture);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_ST;
                half4 _Hue;
                float _RippleAlpha;
                float _RippleIntensity;
                float _Density;
                float _SoftParticlesEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 eyeDepthAndClipW : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings RippleVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.eyeDepthAndClipW = float2(-positions.positionVS.z, positions.positionCS.w);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);
                return output;
            }

            half4 RippleFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                half particleAlpha = saturate(input.color.a);

                #if defined(_SOFTPARTICLES_ON)
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneDepth = unity_OrthoParams.w > 0.5
                        ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams);
                    particleAlpha *= saturate(_Density * (sceneDepth - input.eyeDepthAndClipW.x));
                #endif

                half3 ripple = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                // Preserve the original shader's projected distortion strength. Avoid
                // infinities from grazing normals, and keep sampling inside the image.
                float2 offset = ripple.xy / max(ripple.z, 0.001h)
                    * (_RippleIntensity * particleAlpha / max(input.eyeDepthAndClipW.y, 0.0001));
                float2 grabUV = UnityStereoTransformScreenSpaceTex(saturate(screenUV + offset));
                half3 background = SAMPLE_TEXTURE2D_X(_MMRippleGrabTexture, sampler_MMRippleGrabTexture, grabUV).rgb;
                // Scene alpha is not the effect's opacity. Particle alpha must fade tint
                // as well as displacement, otherwise fading particles leave rectangles.
                return half4(background * _Hue.rgb, saturate(_RippleAlpha * particleAlpha));
            }
            ENDHLSL
        }
    }

    // Built-in pipeline fallback retains the original named GrabPass behavior.
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1" "RenderType" = "Transparent"
        }
        Zwrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        GrabPass
        {
            "_BackgroundTexture"
        }
        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma multi_compile_particles
            #pragma fragment frag
            #pragma vertex vert

            float _RippleAlpha;
            float _RippleIntensity;
            fixed4 _Hue;
            sampler2D _BackgroundTexture;
            sampler2D _NormalMap;
            sampler2D_float _CameraDepthTexture;
            float _Density;

            struct v2f
            {
                float4 grabScreenPosition : TEXCOORD0;
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 normalMap : TEXCOORD1;

                #ifdef SOFTPARTICLES_ON
					float4 computedScreenPosition : TEXCOORD2;
                #endif
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);

                #ifdef SOFTPARTICLES_ON
					o.computedScreenPosition = ComputeScreenPos(o.position);
					COMPUTE_EYEDEPTH(o.computedScreenPosition.z);
                #endif

                o.grabScreenPosition = ComputeGrabScreenPos(o.position);

                o.color = v.color;
                o.normalMap = v.texcoord;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                #ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.computedScreenPosition)));
					float partZ = i.computedScreenPosition.z;
					float fade = saturate(_Density * (sceneZ - partZ));
					i.color.a *= fade;
                #endif

                half3 ripple = UnpackNormal(tex2D(_NormalMap, i.normalMap.xy));
                i.grabScreenPosition.xy += ripple.xy / ripple.z * _RippleIntensity * i.color.a;
                half4 backgroundColor = tex2Dproj(_BackgroundTexture, i.grabScreenPosition);
                _Hue.a = _RippleAlpha;
                // return float4(i.grabScreenPosition.xy, 0, 1);
                return backgroundColor * _Hue;
            }
            ENDCG
        }

    }
    FallBack Off
}
