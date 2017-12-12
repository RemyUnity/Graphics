Shader "Hidden/HDRenderPipeline/Sky/SkyHDRI"
{
    HLSLINCLUDE

    #pragma vertex Vert
    #pragma fragment Frag

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal

    #include "ShaderLibrary/Common.hlsl"
    #include "../../../ShaderVariables.hlsl"
    #include "ShaderLibrary/Color.hlsl"
    #include "ShaderLibrary/CommonLighting.hlsl"
    #include "ShaderLibrary/VolumeRendering.hlsl"

    TEXTURECUBE(_Cubemap);
#ifdef VOLUMETRIC_LIGHTING_ENABLED
    TEXTURE3D(_VBufferLighting);
#endif

    float4   _SkyParam; // x exposure, y multiplier, z rotation
    float4x4 _PixelCoordToViewDirWS; // Actually just 3x3, but Unity can only set 4x4

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        // Points towards the camera
        float3 viewDirWS = normalize(mul(float3(input.positionCS.xy, 1.0), (float3x3)_PixelCoordToViewDirWS));
        // Reverse it to point into the scene
        float3 dir = -viewDirWS;

        // Rotate direction
        float phi = DegToRad(_SkyParam.z);
        float cosPhi, sinPhi;
        sincos(phi, sinPhi, cosPhi);
        float3 rotDirX = float3(cosPhi, 0, -sinPhi);
        float3 rotDirY = float3(sinPhi, 0, cosPhi);
        dir = float3(dot(rotDirX, dir), dir.y, dot(rotDirY, dir));

        float3 skyColor = ClampToFloat16Max(SAMPLE_TEXTURECUBE_LOD(_Cubemap, s_linear_clamp_sampler, dir, 0).rgb * exp2(_SkyParam.x) * _SkyParam.y);

    #ifdef VOLUMETRIC_LIGHTING_ENABLED
        PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
        float4 volumetricLighting = GetInScatteredRadianceAndTransmittance(posInput.positionNDC,
                                                                           TEXTURE3D_PARAM(_VBufferLighting, s_linear_clamp_sampler),
                                                                           _VBufferResolutionAndScale.zw);
        skyColor *= volumetricLighting.a;
        skyColor += volumetricLighting.rgb;
    #endif

        return float4(skyColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            ENDHLSL

        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
            ENDHLSL
        }

    }
    Fallback Off
}
