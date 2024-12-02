// Aaron Lanterman, July 2, 2023
// Based heavily on https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/
Shader "GPU23SRP/MyUnlit"
{
    Properties {
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }
    
    SubShader {
        Pass {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            
// Unity defaults to target 2.5                        
#pragma target 3.5
#pragma multi_compile_instancing
                    
#pragma vertex MyUnlitPassVertex
#pragma fragment MyUnlitPassFragment

// Contains CBUFFER macros  
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
// This may redefine UNITY_MATRIX_M in case of GPU instancing
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
  
// Unity will populate these variables for us
// CBUFFER definitions must be consistent throughout project
CBUFFER_START(UnityPerFrame) 
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
CBUFFER_END

CBUFFER_START(UnityPerMaterial)
    float4 _Color;
CBUFFER_END

struct VertexInput {
    float4 pos : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID 
};

struct VertexOutput {
    float4 clipPos : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID 
};

VertexOutput MyUnlitPassVertex (VertexInput input) {
    VertexOutput output;
    // UNITY_MATRIX_M is basically unity_ObjectToWorld, extended to instancing
    UNITY_SETUP_INSTANCE_ID(input);
    float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
    output.clipPos = mul(unity_MatrixVP, worldPos);
    return output;
}

float4 MyUnlitPassFragment (VertexOutput input) : SV_TARGET {
    return _Color;
}

            ENDHLSL
        }
    }
}