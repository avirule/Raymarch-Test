#region

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Wyd.Noise;

#endregion

[BurstCompile]
public struct CreateWorldDataJob : IJobParallelFor
{
    private readonly int _GridSize;
    private readonly float _Frequency;
    private readonly float _Persistence;
    private readonly int _Seed;
    private readonly int _SeedA;
    private readonly int _SeedB;

    public NativeArray<short> WorldData;

    public CreateWorldDataJob(int gridSize, int seed, float frequency, float persistence)
    {
        _GridSize = gridSize;
        _Frequency = frequency;
        _Persistence = persistence;
        _Seed = seed;
        _SeedA = _Seed ^ 2;
        _SeedB = _Seed ^ 3;

        WorldData = new NativeArray<short>(_GridSize * _GridSize * _GridSize, Allocator.TempJob);
    }

    public void Execute(int index)
    {
        StaticMath.Project3D_XYZ(index, _GridSize, out int3 localPosition);
        float simplexNoise = GetHeightByGlobalPosition(localPosition.xz);
        short noiseHeight = (short)simplexNoise;

        if ((localPosition.y > noiseHeight) || (GetCaveNoiseByGlobalPosition(localPosition) < 0.000225f))
        {
            WorldData[index] = -1;
        }
        else
        {
            WorldData[index] = noiseHeight;
        }
    }

    private float GetCaveNoiseByGlobalPosition(int3 globalPosition)
    {
        float currentHeight = (globalPosition.y + (((_GridSize / 4f) - (globalPosition.y * 1.25f)) * _Persistence)) * 0.85f;
        float heightDampener = math.unlerp(0f, _GridSize, currentHeight);
        float noiseA = OpenSimplexSlim.GetSimplex(_SeedA, 0.01f, globalPosition) * heightDampener;
        float noiseB = OpenSimplexSlim.GetSimplex(_SeedB, 0.01f, globalPosition) * heightDampener;
        float noiseAPow2 = math.pow(noiseA, 2f);
        float noiseBPow2 = math.pow(noiseB, 2f);

        return (noiseAPow2 + noiseBPow2) / 2f;
    }

    private int GetHeightByGlobalPosition(int2 globalPosition)
    {
        float noise = OpenSimplexSlim.GetSimplex(_Seed, _Frequency, globalPosition);
        float noiseAsWorldHeight = math.unlerp(-1f, 1f, noise) * _GridSize;
        float noisePersistedWorldHeight =
            noiseAsWorldHeight + (((_GridSize * 0.5f) - (noiseAsWorldHeight * 1.25f)) * _Persistence);

        return (int)math.floor(noisePersistedWorldHeight);
    }
}
