#include "../Libraries/header.cginc"


#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    struct PCData
    {
        float3 Position;
    };

    #if defined(SHADER_API_GLCORE) || defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PSSL) || defined(SHADER_API_XBOXONE)
        uniform StructuredBuffer<PCData> _PCDataBuffer; 
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
        PCData attribs = _PCDataBuffer[unity_InstanceID];
        Out = In + attribs.Position.xyz;
    #endif  
}

