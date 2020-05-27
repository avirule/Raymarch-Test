#region

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

#endregion

[BurstCompile]
public struct CreateRaymarchTextureJob : IJobParallelFor
{
    private readonly int _GridSize;

    private Random _Random;

    [ReadOnly]
    public NativeArray<short> Blocks;

    [WriteOnly]
    public NativeArray<float> OutputDistances;

    public CreateRaymarchTextureJob(int gridSize, NativeArray<short> blocks)
    {
        _GridSize = gridSize;
        _Random = new Random((uint)new int3(0).GetHashCode());

        Blocks = blocks;
        OutputDistances = new NativeArray<float>(blocks.Length, Allocator.TempJob);
    }

    public void Execute(int index)
    {
        StaticMath.Project3D_XYZ(index, _GridSize, out int3 coords);
        float colorNoise = _Random.NextFloat(0f, 0.075f);

        if (Blocks[index] > -1)
        {
            if (coords.y == Blocks[index])
            {
                OutputDistances[index] = 1f + colorNoise + 1;
            }
            else if ((coords.y < Blocks[index]) && (coords.y > (Blocks[index] - 4)))
            {
                OutputDistances[index] = 1f + colorNoise + 2;
            }
            else
            {
                OutputDistances[index] = 1f + colorNoise + 3;
            }
        }
        else
        {
            OutputDistances[index] = FindMaximumJump(coords) / (float)_GridSize;
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
        int index = StaticMath.Project1D_XYZ(start, _GridSize);
        for (int x = start.x; x <= end.x; x++)
        for (int y = start.y; y <= end.y; y++)
        for (int z = start.z; z <= end.z; z++, index++)
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
