#include "../Libraries/header.cginc"


#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    struct BoxRender
    {
        float3 Position;
        float4 Rot;
        float Pscale;
    };
    #if defined(SHADER_API_GLCORE) || defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PSSL) || defined(SHADER_API_XBOXONE)
        uniform StructuredBuffer<BoxRender> _BufferBoxRender; 
    #endif
#endif



void SetupVtx()   // Get data from buffer and store them in private variables // Here it's just a dummy function
{
        return;
}

void InjectSetup_float(float3 In, out float3 Out) 
{

    Out = float3(0, 0, 0);

    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        BoxRender attribs = _BufferBoxRender[unity_InstanceID];
        Out = In + attribs.Position.xyz;
    #endif  
}

