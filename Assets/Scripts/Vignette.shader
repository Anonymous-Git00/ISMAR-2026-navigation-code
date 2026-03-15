/*
Shader "App/Vignette"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc ("Blend Source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendDst ("Blend Destination", Float) = 0
        _ZWrite ("Z Write", Float) = 0
    }
    SubShader
    {
        Tags { "IgnoreProjector" = "True" }

        Pass
        {
            Blend [_BlendSrc] [_BlendDst]
            ZTest Always
            ZWrite [_ZWrite]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ QUADRATIC_FALLOFF
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ScaleAndOffset0[2];
            float4 _ScaleAndOffset1[2];

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                ZERO_INITIALIZE(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // _ScaleAndOffset0에는 정점(Vertex)을 바깥쪽으로 이동하기 위한 값이, _ScaleAndOffset1에는 안쪽으로 이동하기 위한 값이 저장되어 있다
                // 또한 Vignette의 메쉬를 생성할 때, 홀수 Vertex의 UV.x는 0, 짝수 Vertex의 UV.x는 1이 되도록 설정했기 때문에,
                // Lerp를 통해 홀수 Vertex는 바깥쪽으로, 짝수 Vertex는 안쪽으로 이동된다
                float4 scaleAndOffset = lerp(_ScaleAndOffset0[unity_StereoEyeIndex], _ScaleAndOffset1[unity_StereoEyeIndex], v.uv.x);

                // UNITY_NEAR_CLIP_VALUE는 HClip 공간에서 Vignette를 카메라 앞에 표시하기 위한 값이다
                o.vertex = float4(scaleAndOffset.zw + v.vertex.xy * scaleAndOffset.xy, UNITY_NEAR_CLIP_VALUE, 1);

                o.color.rgb = _Color.rgb;
                // UV의 Y를 알파 값으로 사용한다
                // Vignette의 메쉬를 생성할 때 불투명 부분은 Y를 1로, 반투명 부분은 외부에서 내부로 0->1이 되도록 설정했다
                //o.color.a = v.uv.y;   // Original Code
                o.color.a = v.uv.y * _Color.a;  // Color Alpha 값도 계산에 추가
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
#if QUADRATIC_FALLOFF
                i.color.a *= i.color.a;
#endif
                return i.color;
            }
            ENDHLSL
        }
    }
}
*/
Shader "App/Vignette"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc ("Blend Source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendDst ("Blend Destination", Float) = 0
        _ZWrite ("Z Write", Float) = 0
        _MotionStrength ("Motion Strength", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "IgnoreProjector" = "True" }
        /*
        Pass
        {
            Blend [_BlendSrc] [_BlendDst]
            ZTest Always
            ZWrite [_ZWrite]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ QUADRATIC_FALLOFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 screenPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ScaleAndOffset0[2];
            float4 _ScaleAndOffset1[2];
            
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _MotionStrength;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                ZERO_INITIALIZE(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // 기존 스케일 및 오프셋 계산
                float4 scaleAndOffset = lerp(_ScaleAndOffset0[unity_StereoEyeIndex], _ScaleAndOffset1[unity_StereoEyeIndex], v.uv.x);
                
                // 모션 강도에 따른 동적 스케일 조정
                float2 dynamicScale = scaleAndOffset.xy * (1.0 + _MotionStrength * 0.2);
                
                o.vertex = float4(scaleAndOffset.zw + v.vertex.xy * dynamicScale, UNITY_NEAR_CLIP_VALUE, 1);
                o.screenPos = o.vertex.xy * 0.5 + 0.5;
                
                o.color.rgb = _Color.rgb;
                o.color.a = v.uv.y * _Color.a;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Alpha가 거의 1.0(255/255)일 때는 완전 불투명 처리
                if (_Color.a >= 0.99) // 255에 가까운 값일 때
                {
                    i.color.a = i.color.a; // 추가 처리 없이 원본 알파 유지
        
                    #if QUADRATIC_FALLOFF
                    i.color.a *= i.color.a;
                    #endif
        
                    return i.color;
                }

                // 중심에서의 거리 계산 (동적 효과용)
                float2 centerOffset = i.screenPos - 0.5;
                float distanceFromCenter = length(centerOffset);
                
                // 모션 강도에 따른 알파 조정
                float motionAlpha = 1.0 - _MotionStrength * 0.6;
                i.color.a *= motionAlpha;

                #if QUADRATIC_FALLOFF
                i.color.a *= i.color.a;
                #endif

                // 거리 기반 페이드 효과 (선택사항)
                float fadeEffect = 1.0 - smoothstep(0.3, 0.7, distanceFromCenter * (1.0 + _MotionStrength));
                i.color.a *= fadeEffect;

                return i.color;
            }
            ENDHLSL
        }
        */
        
        Pass
        {
            Blend [_BlendSrc] [_BlendDst]
            ZTest Always
            ZWrite [_ZWrite]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
       
            #pragma multi_compile _ QUADRATIC_FALLOFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ScaleAndOffset0[2];
            float4 _ScaleAndOffset1[2];

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                ZERO_INITIALIZE(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 scaleAndOffset = lerp(_ScaleAndOffset0[unity_StereoEyeIndex], _ScaleAndOffset1[unity_StereoEyeIndex], v.uv.x);
                o.vertex = float4(scaleAndOffset.zw + v.vertex.xy * scaleAndOffset.xy, UNITY_NEAR_CLIP_VALUE, 1);
                o.color.rgb = _Color.rgb;
                // uv.y는 메시 생성 시 설정된 값 (가장자리는 0, 안쪽은 1)을 그대로 사용
                // 스크립트에서 _Color.a를 1로 설정했으므로, 이 값은 최종적으로 uv.y가 됩니다.
                o.color.a = v.uv.y * (1 - _Color.a); 
                return o;
            }

            // 프래그먼트 셰이더를 단순하게 유지합니다.
            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // 부드러운 가장자리를 위한 Quadratic Falloff 처리
                #if QUADRATIC_FALLOFF
                i.color.a *= i.color.a;
                #endif

                // 버텍스 셰이더에서 계산된 색상 값을 그대로 반환합니다.
                return i.color;
            }
            ENDHLSL
        }
        


    }
}
