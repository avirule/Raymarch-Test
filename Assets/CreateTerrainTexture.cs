#region

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

#endregion

[BurstCompile]
public struct CreateTerrainTexture : IJobParallelFor
{
    private const int HEIGHTMAP_MASK = 0b0001_1111_1111_1111_1110;
    private const int SOLID_MASK = 0b1;

    private readonly int _GridSize;

    [ReadOnly]
    public NativeArray<short> Blocks;
    public NativeArray<float> OutputDistances;

    public CreateTerrainTexture(int gridSize, short[] blocks)
    {
        _GridSize = gridSize;
        Blocks = new NativeArray<short>(blocks, Allocator.TempJob);
        OutputDistances = new NativeArray<float>(blocks.Length, Allocator.TempJob);
    }

    public void Execute(int index)
    {
        if (Blocks[index] > -1)
        {
            OutputDistances[index] = 1f;
        }
        else
        {
            Project3D(index, _GridSize, out int x, out int y, out int z);
            OutputDistances[index] = FindMaximumJump(x, y, z) / (float)_GridSize;
        }
    }

    private int FindMaximumJump(int startX, int startY, int startZ)
    {
        int jumpSize = 0;

        while (IsEmpty(startX - (jumpSize + 1), startY - (jumpSize + 1), startZ - (jumpSize + 1), startX + jumpSize + 1, startY + jumpSize + 1,
                   startZ + jumpSize + 1)
               && (jumpSize < _GridSize))
        {
            jumpSize += 1;
        }

        return jumpSize;
    }

    private bool IsEmpty(int startX, int startY, int startZ, int endX, int endY, int endZ)
    {
        int index = 0;
        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        for (int z = startZ; z <= endZ; z++, index++)
        {
            if (IsSolid(index, x, y, z))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSolid(int index, int x, int y, int z) =>
        (x >= 0) && (y >= 0) && (z >= 0) && (x < _GridSize) && (y < _GridSize) && (z < _GridSize) && (Blocks[index] > -1);

    private static int Project1D(int x, int y, int z, int size) => x + (size * (z + (size * y)));

    private static void Project3D(int index, int size, out int x, out int y, out int z)
    {
        int xQuotient = Math.DivRem(index, size, out x);
        int zQuotient = Math.DivRem(xQuotient, size, out z);
        y = zQuotient % size;
    }
}
