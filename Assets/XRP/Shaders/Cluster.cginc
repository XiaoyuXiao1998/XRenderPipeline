

struct LightIndex {
    int start; //start index
    int count; //
};

struct PointLight
{
    float3 color;
    float intensity;
    float3 position;
    float radius;
};

StructuredBuffer<LightIndex> _assignTable;
StructuredBuffer<PointLight> _lightBuffer;
StructuredBuffer<uint> _lightAssignBuffer;

float NumClusterX ;
float NumClusterY ;
float NumClusterZ;
