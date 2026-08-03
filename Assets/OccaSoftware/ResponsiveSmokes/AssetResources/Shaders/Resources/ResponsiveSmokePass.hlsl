#ifndef OS_INTERACTIVE_VOLUMETRIC_SMOKE_INCLUDE
#define OS_INTERACTIVE_VOLUMETRIC_SMOKE_INCLUDE


///////////////////////////////////////////////////////////////////////////////
//                      Includes                                             //
///////////////////////////////////////////////////////////////////////////////

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
// See ShaderVariablesFunctions.hlsl in com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl


///////////////////////////////////////////////////////////////////////////////
//                      Global Defines                                       //
///////////////////////////////////////////////////////////////////////////////
#define _EPSILON 0.2
#define _MAX_EXPLOSIONS 3
#define _MAX_PROJECTILES 10

///////////////////////////////////////////////////////////////////////////////
//                      Global Vars                                          //
///////////////////////////////////////////////////////////////////////////////
const SamplerState linear_clamp_sampler;



///////////////////////////////////////////////////////////////////////////////
//                      Local Properties                                     //
///////////////////////////////////////////////////////////////////////////////

CBUFFER_START(UnityPerMaterial)
    TEXTURE3D(_NoiseTexture);
    SAMPLER(sampler_NoiseTexture);
    Texture3D _PropagationVolume;
    float _ErosionScale;
    
    float _Density;
    int _MaxSteps;
    float _MinimumVisibility;
    
    float3 _NoiseWind;
    float _NoiseScale;
    
    float4 _ExplosionTransform[_MAX_EXPLOSIONS];
    float _ExplosionIntensity[_MAX_EXPLOSIONS];
    int _ExplosionCount;

    float3 _ProjectileStart[_MAX_PROJECTILES];
    float3 _ProjectileEnd[_MAX_PROJECTILES];
    float _ProjectileRadius[_MAX_PROJECTILES];
    float _ProjectileIntensity[_MAX_PROJECTILES];
    int _ProjectileCount;

    float3 _ObjectPosition;
    float _ObjectScale;
    float _InvObjectScale;
    float _InvErosionScale;

    float _ShadowStepSize;
    int _ShadowStepCount;

    float3 _Albedo;
    float3 _MainLightTint;
    float3 _AmbientColorTint;
CBUFFER_END



///////////////////////////////////////////////////////////////////////////////
//                      Helper Functions                                     //
///////////////////////////////////////////////////////////////////////////////

half InverseLerp(half a, half b, half v)
{
	return (v - a) / (b - a);
}

half RemapUnclamped(half iMin, half iMax, half oMin, half oMax, half v)
{
	half t = InverseLerp(iMin, iMax, v);
	return lerp(oMin, oMax, t);
}

half Remap(half iMin, half iMax, half oMin, half oMax, half v)
{
	v = clamp(v, iMin, iMax);
	return RemapUnclamped(iMin, iMax, oMin, oMax, v);
}

float CheapSqrt(float a)
{
    return 1.0 - ((1.0 - a) * (1.0 - a));
}

float dot01(float3 a, float3 b)
{
    return saturate(dot(a, b));
}

float luminance(float3 c)
{
    return dot(c, float3(0.2126, 0.7152, 0.0722));
}

float max_component(float3 a)
{
    return max(a.x, max(a.y, a.z));
}

float min_component(float3 a)
{
    return min(a.x, min(a.y, a.z));
}

float sdfLineSegment(float3 position, float3 start, float3 end, float radius)
{
    float3 startDir = position - start;
    float3 lineDir = end - start;
    float t = saturate(dot(startDir, lineDir) / dot(lineDir, lineDir));
    return length(startDir - lineDir * t) - radius;
}

float4 GetPositionNDC(float4 positionHCS)
{
    float4 ndc = positionHCS * 0.5f;
    ndc.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    ndc.zw = positionHCS.zw;
    return ndc;
}

float nrand(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}


float3 GetObjectPositionWS()
{
    return _ObjectPosition;
   // return unity_ObjectToWorld._m03_m13_m23;
}

float GetObjectScale()
{
    return _ObjectScale;
    //return length(mul(unity_ObjectToWorld, float4(1.0, 0, 0, 0)).xyz);
}

float GetInvObjectScale()
{
    //return 1.0f / GetObjectScale();
    return _InvObjectScale;
}


bool ExitEarly(float3 uvw)
{
    float3 boxTest = floor(0.5 + (abs(0.5 - uvw)));
    float discriminant = boxTest.x + boxTest.y + boxTest.z;
    return discriminant >= 1;
}


///////////////////////////////////////////////////////////////////////////////
//                      Common Structs                                       //
///////////////////////////////////////////////////////////////////////////////

struct Ray
{
    float3 dir;
    float3 origin;
};

struct AABB
{
    float3 min;
    float3 max;
};

struct Object
{
    float3 positionWS;
    float scale;
};

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Transforms                                  //
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
//                      Density Functions                                    //
///////////////////////////////////////////////////////////////////////////////
struct DensityData
{
    float sdf;
    float transmittance;
    float extinction;
    float invExtinction;
};


float3 GetSamplePosition(float3 rayPosition)
{
    return float3(0.5, 0.5, 0.5) + (rayPosition - GetObjectPositionWS()) * GetInvObjectScale();
}

float GetSDF(float3 uvw)
{
    float sdf = _PropagationVolume.SampleLevel(linear_clamp_sampler, uvw, 0).r;
    return sdf;
}



float GetInteractiveData(float3 rayPositionWS)
{
    const float _FALLOFF_DIST = 0.5;
    float t = 1.0;
    
    [loop]
    for(int i = 0; i < _ExplosionCount; i++)
    {
        float f = _FALLOFF_DIST * _ExplosionTransform[i].w;
        float l = (distance(rayPositionWS, _ExplosionTransform[i].xyz) - _ExplosionTransform[i].w);
        float s = smoothstep(-f, f, l);
        t *= lerp(1.0 - _ExplosionIntensity[i], 1.0, s);
    }
    
    [loop]
    for(int j = 0; j < _ProjectileCount; j++)
    { 
        float f = _FALLOFF_DIST * _ProjectileRadius[i];
        float l = sdfLineSegment(rayPositionWS, _ProjectileStart[j], _ProjectileEnd[j], _ProjectileRadius[j]);
        float s = smoothstep(-f, f, l);
        t *= lerp(1.0 - _ProjectileIntensity[j], 1.0, s);
    }
    
    return t;
}


void CalculateExtinctionUVW(float3 uvw, float3 rayPositionWS, inout DensityData data)
{
    // Get SDF
    float sdf = GetSDF(uvw);
    
    if(sdf > 0)
        return;
    
    data.sdf = sdf;
    
    // Get Interactive Data
    float interactiveResult = GetInteractiveData(rayPositionWS);
    
    
    // Calculate Erosion
    sdf = -sdf;
    float v = sdf * _InvErosionScale;
    
    v *= interactiveResult;
    float dispersion = saturate(_NoiseTexture.SampleLevel(sampler_NoiseTexture, uvw * _NoiseScale - _Time.y * _NoiseWind, 0).r);
    float value = Remap(1.0 - dispersion, 1.0, 0.0, 1.0, v);
    
    
    
    // Calculate Extinction
    float extinction = value * _Density;
    
    
    // Return
    data.extinction = extinction;
}



void CalculateExtinctionWS(float3 rayPositionWS, inout DensityData data)
{
    float3 uvw = GetSamplePosition(rayPositionWS);
    CalculateExtinctionUVW(uvw, rayPositionWS, data);
}


void CalculateDensity(float3 rayPositionWS, float stepSize, inout DensityData data)
{
    // Get Extinction
    CalculateExtinctionWS(rayPositionWS, data);
    if(data.extinction <= _EPSILON)
        return;
    
    // Density Data
    data.transmittance = exp(-data.extinction * stepSize);
    data.extinction = max(data.extinction, _EPSILON);
    data.invExtinction = rcp(data.extinction);
}



///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////

float3 GetAmbientLightColor()
{
    return SampleSH(float3(0,1,0));
}


float CalculateShading(float3 rayPositionWS, float3 rayDirection, float StepSize, int StepCount)
{
    float luminance = 1.0;
    
    for (int i = 0; i < StepCount; i++)
    {
        rayPositionWS += rayDirection * StepSize;
        float3 uvw = GetSamplePosition(rayPositionWS);
        
        if(ExitEarly(uvw)) 
            break; 
        
        DensityData data = (DensityData)0;
        CalculateExtinctionWS(rayPositionWS, data);
        luminance *= exp(-data.extinction * StepSize);
    }
    
    return luminance;
}


float GetIntegrationTerm(float luminance, float transmittance, float invExtinction)
{
    return (luminance - luminance * transmittance) * invExtinction;
}


void CalculateLighting(in float3 rayPosition, in float stepSize, in float3 Albedo, in float3 AmbientLightColor, in float3 ShadowDirection, inout float3 Color, inout float Alpha)
{ 
    float3 uvw = GetSamplePosition(rayPosition);
    
    DensityData densityData = (DensityData)0;
    CalculateDensity(rayPosition, stepSize, densityData);
    
    if(densityData.extinction <= _EPSILON)
        return;
    
    float mainLighting = CalculateShading(rayPosition, ShadowDirection, _ShadowStepSize, _ShadowStepCount);
    float mainLightingIntegrationTerm = GetIntegrationTerm(mainLighting, densityData.transmittance, densityData.invExtinction);
    
    float ambientLightingIntegrationTerm = (1.0 - densityData.transmittance) * densityData.invExtinction;
    
    // Apply Transmittance and Lighting
    Color += Albedo * Alpha * ((mainLightingIntegrationTerm * GetMainLight().color * _MainLightTint) + (ambientLightingIntegrationTerm * AmbientLightColor));
    Alpha *= densityData.transmittance;
}


///////////////////////////////////////////////////////////////////////////////
//                      Space Functions                                      //
///////////////////////////////////////////////////////////////////////////////

float CalculateDepth(float2 screenUV)
{
    float depth01 = SampleSceneDepth(screenUV);
    float depthEye = LinearEyeDepth(depth01, _ZBufferParams);
	
    float3 viewVector = mul(unity_CameraInvProjection, float4(screenUV * 2 - 1, 0.0, -1)).xyz;
	viewVector = mul(unity_CameraToWorld, float4(viewVector, 0.0)).xyz;
	float viewLength = length(viewVector);
    
	float depthWS = depthEye * viewLength;
    return depthWS;
}

bool RayAABBTest(Ray ray, AABB aabb, float2 screenUV, float3 positionWS, float sceneDepth, out float tNear, out float tFar) {

    float3 invRayDir = rcp(ray.dir);
    float3 tMin = (aabb.min - ray.origin) * invRayDir;
  	float3 tMax = (aabb.max - ray.origin) * invRayDir;
      
  	float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    
    tFar = min_component(t2);
    tNear = max_component(t1);
    
    tNear = max(tNear, 0);
    tFar = max(tFar, 0);
    
    return (tNear <= tFar) && tFar > 0.0 && tNear < sceneDepth;
}


/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
///                                                                           ///
///                      SHADER BODY                                          ///
///                                                                           ///
/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////

struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
            
struct Varyings
{
    float4 positionHCS     : SV_POSITION;
    float3 positionWS      : TEXCOORD0;
    float4 positionNDC     : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};



///////////////////////////////////////////////////////////////////////////////
//                      Vertex                                               //
///////////////////////////////////////////////////////////////////////////////

Varyings Vertex(Attributes IN)
{
    Varyings OUT = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
    
    OUT.positionWS = mul(unity_ObjectToWorld, IN.positionOS).xyz;
    OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
    OUT.positionNDC = GetPositionNDC(OUT.positionHCS);
    
    return OUT;
}



///////////////////////////////////////////////////////////////////////////////
//                      Fragment                                             //
///////////////////////////////////////////////////////////////////////////////


float4 Fragment(Varyings IN) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    
    ///////////////////////////////
    //   SETUP BOUNDS            //
    ///////////////////////////////
    
    Ray ray;
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 cameraVectorWS = normalize(IN.positionWS - cameraPositionWS);
    float2 screenUV = IN.positionNDC.xy / IN.positionNDC.w;
    
    ray.dir = cameraVectorWS;
    ray.origin = cameraPositionWS;
    
    AABB aabb;
    float boundSize = 0.5 * GetObjectScale();
    float3 bounds = float3(boundSize, boundSize, boundSize);
    aabb.min = GetObjectPositionWS() - bounds;
    aabb.max = GetObjectPositionWS() + bounds;
    
    float sceneDepth = CalculateDepth(screenUV);
    
    float tNear, tFar;
    bool intersect = RayAABBTest(ray, aabb, screenUV, IN.positionWS, sceneDepth, tNear, tFar);
    
    if(!intersect)
        discard;
        
    const float thickness = max(tFar - tNear, 0);
    
    ///////////////////////////////
    //   PREP RAYMARCH           //
    ///////////////////////////////
    
    float3 Color = float3(0, 0, 0);
    float Alpha = 1.0;
    
    float maxRayDepth = min(sceneDepth, tFar);
    
    float StepSize = rcp(_MaxSteps) * thickness;
    float totalDistance = tNear + nrand(screenUV * _Time.xx) * StepSize;
    float stepCountDepth = (maxRayDepth - tNear) * rcp(StepSize);
        
    float3 rayPositionWS;
    
    ///////////////////////////////
    //   RAYMARCH                //
    ///////////////////////////////
    
    
    float3 AmbientLightColor = GetAmbientLightColor() * _AmbientColorTint;
    float3 ShadowDirection = GetMainLight().direction;
    float3 shadowDirOffset = float3(nrand(screenUV * 2), nrand(screenUV * 3), nrand(screenUV * 4)) - float3(0.5, 0.5, 0.5);
    shadowDirOffset *= 0.1;
    ShadowDirection += shadowDirOffset;
    ShadowDirection = normalize(ShadowDirection);
    
    for (int i = 0; i < _MaxSteps; i++)
    {
        rayPositionWS = ray.origin + ray.dir * totalDistance;
        CalculateLighting(rayPositionWS, StepSize, _Albedo, AmbientLightColor, ShadowDirection, Color, Alpha);
        
        if(Alpha < _MinimumVisibility)
        {
            Alpha = 0;
            break;
        }
        
        
        totalDistance += StepSize;
        if(totalDistance > maxRayDepth)
            break;
    }
    
    return float4(Color, 1.0 - Alpha);
}
#endif