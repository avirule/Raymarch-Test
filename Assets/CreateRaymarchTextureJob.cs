#region

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

#endregion

//[BurstCompile]
public struct CreateRaymarchTextureJob : IJobParallelFor
{
    private readonly int _GridSize;

    private Random _Random;

    [ReadOnly]
    public NativeArray<short> Blocks;

    public NativeArray<Color> OutputDistances;

    public CreateRaymarchTextureJob(uint seed, int gridSize, NativeArray<short> blocks)
    {
        _GridSize = gridSize;
        _Random = new Random(seed);
        Blocks = blocks;
        OutputDistances = new NativeArray<Color>(blocks.Length, Allocator.TempJob);
    }

    public void Execute(int index)
    {
        StaticMath.Project3D_XYZ(index, _GridSize, out int3 coords);

        if (Blocks[index] > -1)
        {
            float3 colorNoise = _Random.NextInt(-50, 51) * 0.0005f;

            if (coords.y == Blocks[index])
            {
                float3 color = new float3(0.38f, 0.59f, 0.20f) + colorNoise;
                OutputDistances[index] = new Color(color.x, color.y, color.z);
            }
            else if ((coords.y < Blocks[index]) && (coords.y > (Blocks[index] - 4)))
            {
                float3 color = new float3(0.36f, 0.25f, 0.2f) + colorNoise;
                OutputDistances[index] = new Color(color.x, color.y, color.z, 1f);
            }
            else
            {
                float3 color = new float3(0.41f) + colorNoise;
                OutputDistances[index] = new Color(color.x, color.y, color.z, 1f);
            }
        }
        else
        {
            OutputDistances[index] = new Color(0f, 0f, 0f, FindMaximumJump(coords) / (float)_GridSize);
        }
    }

    private int FindMaximumJump(int3 coords)
    {
        int jumpSize = 0;

        while ((jumpSize < _GridSize) && IsBoundingBoxEmpty(coords - (jumpSize + 1), coords + (jumpSize + 1)))
        {
            jumpSize += 1;
        }

        return jumpSize;
    }

    private bool IsBoundingBoxEmpty(int3 start, int3 end)
    {
        for (; start.x <= end.x; start.x += 1)
        for (; start.y <= end.y; start.y += 1)
        for (; start.z <= end.z; start.z += 1)
        {
            if (IsBlockSolid(StaticMath.Project1D_XYZ(start, _GridSize), start))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlockSolid(int index, int3 coords) =>
        math.all(coords >= 0)
        && math.all(coords < _GridSize)
        && (index >= 0)
        && (index < Blocks.Length)
        && (Blocks[index] > -1);
}
