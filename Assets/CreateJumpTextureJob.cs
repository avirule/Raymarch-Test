#region

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

#endregion

[BurstCompile]
public struct CreateJumpTextureJob : IJobParallelFor
{
    private readonly int _GridSize;

    private Random _Random;

    [ReadOnly]
    public NativeArray<short> Blocks;

    public NativeArray<Color> OutputDistances;

    public CreateJumpTextureJob(uint seed, int gridSize, short[] blocks)
    {
        _GridSize = gridSize;
        _Random = new Random(seed);
        Blocks = new NativeArray<short>(blocks, Allocator.TempJob);
        OutputDistances = new NativeArray<Color>(blocks.Length, Allocator.TempJob);
    }

    public void Execute(int index)
    {
        StaticMath.Project3D_XYZ(index, _GridSize, out int x, out int y, out int z);

        if (Blocks[index] > -1)
        {
            float3 colorNoise = _Random.NextInt(-50, 51) * 0.0005f;

            if (y == Blocks[index])
            {
                float3 color = new float3(0.38f, 0.59f, 0.20f) + colorNoise;
                OutputDistances[index] = new Color(color.x, color.y, color.z);
            }
            else if ((y < Blocks[index]) && (y > (Blocks[index] - 4)))
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
            OutputDistances[index] = new Color(0f, 0f, 0f, FindMaximumJump(x, y, z) / (float)_GridSize);
        }
    }

    private int FindMaximumJump(int startX, int startY, int startZ)
    {
        int jumpSize = 0;

        while (IsBoundingBoxEmpty(startX - (jumpSize + 1), startY - (jumpSize + 1), startZ - (jumpSize + 1), startX + jumpSize + 1,
                   startY + jumpSize + 1,
                   startZ + jumpSize + 1)
               && (jumpSize < _GridSize))
        {
            jumpSize += 1;
        }

        return jumpSize;
    }

    private bool IsBoundingBoxEmpty(int startX, int startY, int startZ, int endX, int endY, int endZ)
    {
        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        for (int z = startZ; z <= endZ; z++)
        {
            int index = StaticMath.Project1D_XYZ(x, y, z, _GridSize);
            if (IsBlockSolid(index, x, y, z))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlockSolid(int index, int x, int y, int z) =>
        (x >= 0)
        && (y >= 0)
        && (z >= 0)
        && (x < _GridSize)
        && (y < _GridSize)
        && (z < _GridSize)
        && (index >= 0)
        && (index < Blocks.Length)
        && (Blocks[index] > -1);
}
