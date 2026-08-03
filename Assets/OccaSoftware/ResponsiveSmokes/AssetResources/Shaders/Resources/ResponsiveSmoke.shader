Shader"OccaSoftware/ResponsiveSmoke/Smoke"
{
    Properties
    {
        _NoiseTexture ("Noise Data", 3D) = "white" {}
        _PropagationVolume ("Propagation Volume", 3D) = "black" {}
        _ErosionScale ("Erosion Scale", Float) = 0.1
        _Density("Density", Float) = 200
        _MaxSteps("Max Steps", Float) = 128
        _MinimumVisibility("Minimum Visibility", Float) = 0.03
        _NoiseWind("Noise Wind", Vector) = (1,1,1)
        _NoiseScale("Noise Scale", Float) = 1

        _ShadowStepSize("Shadow Step Size", Float) = 0.1
        _ShadowStepCount("Shadow Step Count", Float) = 3

        _Albedo("Smoke Albedo", Vector) = (1, 1, 1)
        _MainLightTint("Main Light Tint", Vector) = (1,1,1)
        _AmbientColorTint("Ambient Color Tint", Vector) = (3, 3, 3)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent"}
        LOD 100
        Pass
        {
            Name "Smoke"
            Tags {"LightMode" = "UniversalForwardOnly"}
            
            Cull Front
            ZWrite Off
            ZTest Always
            ZClip Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma multi_compile_instancing
            
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #include "ResponsiveSmokePass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor"OccaSoftware.ResponsiveSmokes.Editor.ResponsiveSmokeMaterialEditorGUI"
}
