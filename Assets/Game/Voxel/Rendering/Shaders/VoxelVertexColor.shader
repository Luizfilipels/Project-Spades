Shader "ProjectSpades/VoxelVertexColor"
{
    Properties
    {
        _Brightness("Brightness", Range(0.1, 2.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Back
        ZWrite On
        ZTest LEqual

        // =========================================================
        // FORWARD LIGHTING
        // =========================================================

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float _Brightness;

            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                half4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                half3 normalWS : TEXCOORD1;

                half4 color : COLOR;

                half fogFactor : TEXCOORD2;
            };


            Varyings Vert(
                Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS
                    );

                output.positionHCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    normalInputs.normalWS;

                output.color =
                    input.color;

                output.fogFactor =
                    ComputeFogFactor(
                        positionInputs.positionCS.z
                    );

                return output;
            }


            half4 Frag(
                Varyings input)
                : SV_Target
            {
                half3 normalWS =
                    NormalizeNormalPerPixel(
                        input.normalWS
                    );


                // -----------------------------
                // MAIN LIGHT + SHADOW
                // -----------------------------

                float4 shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS
                    );

                Light mainLight =
                    GetMainLight(
                        shadowCoord
                    );


                half NdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                half3 directLight =
                    mainLight.color
                    * NdotL
                    * mainLight.distanceAttenuation
                    * mainLight.shadowAttenuation;


                // -----------------------------
                // AMBIENT / SKY LIGHT
                // -----------------------------

                half3 ambientLight =
                    SampleSH(
                        normalWS
                    );


                // -----------------------------
                // FINAL LIGHT
                // -----------------------------

                half3 lighting =
                    ambientLight +
                    directLight;


                half3 finalColor =
                    input.color.rgb
                    * lighting
                    * _Brightness;


                finalColor =
                    MixFog(
                        finalColor,
                        input.fogFactor
                    );


                return half4(
                    finalColor,
                    input.color.a
                );
            }

            ENDHLSL
        }


        // =========================================================
        // SHADOW CASTER
        // =========================================================

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back


            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


            float3 _LightDirection;
            float3 _LightPosition;


            struct Attributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };


            float4 GetShadowPositionHClip(
                Attributes input)
            {
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS
                    );


                float3 lightDirectionWS =
                    _LightDirection;


                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    lightDirectionWS =
                        normalize(
                            _LightPosition -
                            positionInputs.positionWS
                        );

                #endif


                float4 positionCS =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            positionInputs.positionWS,
                            normalInputs.normalWS,
                            lightDirectionWS
                        )
                    );


                #if UNITY_REVERSED_Z

                    positionCS.z =
                        min(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );

                #else

                    positionCS.z =
                        max(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );

                #endif


                return positionCS;
            }


            Varyings ShadowVert(
                Attributes input)
            {
                Varyings output;

                output.positionCS =
                    GetShadowPositionHClip(
                        input
                    );

                return output;
            }


            half4 ShadowFrag()
                : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}