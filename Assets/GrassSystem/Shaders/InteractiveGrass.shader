Shader "Custom/InteractiveGrass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.6, 0.1, 1)
        _TipColor ("Tip Color", Color) = (0.4, 0.9, 0.2, 1)
        _MainTex ("Grass Texture", 2D) = "white" {}
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.0
        _WindStrength ("Wind Strength", Range(0, 2)) = 0.3
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.5, 0)

        [Header(Interaction)]
        _InteractionStrength ("Interaction Strength", Range(0, 3)) = 1.5
        _InteractionRadius ("Interaction Radius", Range(0, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Max interactors (player + objects)
            #define MAX_INTERACTORS 10

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _MainTex_ST;
                float _AlphaCutoff;
                float _WindSpeed;
                float _WindStrength;
                float4 _WindDirection;
                float _InteractionStrength;
                float _InteractionRadius;
            CBUFFER_END

            // Global interaction data (set from C# script)
            float4 _GrassInteractorPositions[MAX_INTERACTORS];
            int _GrassInteractorCount;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // vertex color for height mask
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 color : COLOR;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posOS = input.positionOS.xyz;
                float3 posWS = TransformObjectToWorld(posOS);

                // Height factor: top vertices bend more (using vertex color or UV.y)
                float heightFactor = input.uv.y;

                // ========== WIND ==========
                float windTime = _Time.y * _WindSpeed;
                float3 windDir = normalize(_WindDirection.xyz);

                // Perlin-like variation using world position
                float windNoise = sin(windTime + posWS.x * 0.5) *
                                  cos(windTime * 0.7 + posWS.z * 0.3);
                float windOffset = windNoise * _WindStrength * heightFactor;

                posOS.x += windDir.x * windOffset;
                posOS.z += windDir.z * windOffset;

                // Secondary micro-sway
                float microSway = sin(windTime * 2.3 + posWS.x * 1.5 + posWS.z * 1.2) * 0.02;
                posOS.x += microSway * heightFactor;

                // ========== INTERACTION ==========
                float3 currentWorldPos = TransformObjectToWorld(posOS);

                for (int i = 0; i < _GrassInteractorCount; i++)
                {
                    float3 interactorPos = _GrassInteractorPositions[i].xyz;
                    float radius = _InteractionRadius;

                    float dist = distance(currentWorldPos.xz, interactorPos.xz);
                    float influence = saturate(1.0 - (dist / radius));
                    influence = influence * influence; // quadratic falloff

                    // Push grass away from interactor
                    float3 pushDir = normalize(currentWorldPos - interactorPos);
                    pushDir.y = 0;

                    float bendAmount = influence * _InteractionStrength * heightFactor;
                    posOS.x += pushDir.x * bendAmount;
                    posOS.z += pushDir.z * bendAmount;
                    // Slight downward bend
                    posOS.y -= influence * 0.2 * heightFactor;
                }

                output.positionCS = TransformObjectToHClip(posOS);
                output.positionWS = TransformObjectToWorld(posOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Alpha cutoff
                clip(texColor.a - _AlphaCutoff);

                // Gradient color from base to tip
                float heightGradient = input.uv.y;
                half4 grassColor = lerp(_BaseColor, _TipColor, heightGradient);
                grassColor *= texColor;

                // Simple lighting
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float lighting = NdotL * 0.6 + 0.4; // soft ambient

                grassColor.rgb *= lighting * mainLight.color.rgb;

                // Apply fog
                grassColor.rgb = MixFog(grassColor.rgb, input.fogFactor);

                return grassColor;
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _MainTex_ST;
                float _AlphaCutoff;
                float _WindSpeed;
                float _WindStrength;
                float4 _WindDirection;
                float _InteractionStrength;
                float _InteractionRadius;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.a - _AlphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
