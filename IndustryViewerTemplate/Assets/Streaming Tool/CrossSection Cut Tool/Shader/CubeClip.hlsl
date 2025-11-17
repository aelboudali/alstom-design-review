void CubeClip_float(float3 WorldPosition, float4x4 WorldToCube, out float Out)
{

    float4 cubePos = mul(WorldToCube, float4(WorldPosition, 1.0));

    float4 planes[6];
    planes[0] = float4(1, 0, 0, -0.5); // r
    planes[1] = float4(-1, 0, 0, -0.5); // l
    planes[2] = float4(0, 1, 0, -0.5); // u
    planes[3] = float4(0, -1, 0, -0.5); // d
    planes[4] = float4(0, 0, 1, -0.5); // f
    planes[5] = float4(0, 0, -1, -0.5); // b

    Out = 1.0;

    for (int i = 0; i < 6; i++)
    {
        if (dot(cubePos.xyz, planes[i].xyz) + planes[i].w >= 0.0)
        {
            Out = 0.0;
            break;
        }
    }
}