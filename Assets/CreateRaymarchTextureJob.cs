#region

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

#endregion

[BurstCompile]
public struct CreateRaymarchTextureJob : IJobParallelFor
{
    private readonly int _GridSize;

    private Random _Random;

    [ReadOnly]
    public NativeArray<short> Blocks;

    [WriteOnly]
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
            float colorNoise = _Random.NextFloat(-0.05f, 0.05f);

            if (coords.y == Blocks[index])
            {
                float3 color = new float3(0.38f, 0.59f, 0.20f) + colorNoise;
                OutputDistances[index] = new Color(color.x, color.y, color.z, 1f);
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

        while ((jumpSize < _GridSize)
               && IsBoundingBoxEmpty(
                   math.clamp(coords - (jumpSize + 1), 0, _GridSize),
                   math.clamp(coords + (jumpSize + 1), 0, _GridSize)))
        {
            jumpSize += 1;
        }

        return jumpSize;
    }

    private bool IsBoundingBoxEmpty(int3 start, int3 end)
    {
        int index = 0;
        int3 coords;
        for (coords.x = start.x; coords.x <= end.x; coords.x += 1)
        for (coords.y = start.y; coords.y <= end.y; coords.y += 1)
        for (coords.z = start.z; coords.z <= end.z; coords.z += 1, index++)
        {
            if (IsBlockSolid(index))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlockSolid(int index) => Blocks[index] > -1;
}
