#region

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

#endregion

[BurstCompile]
public struct CreateRaymarchTextureJob : IJobParallelFor
{
    private const float _SOLID_BASE_VALUE = 1f;

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
            int paletteIndex = 0;

            if (coords.y == Blocks[index])
            {
                OutputDistances[index] = _SOLID_BASE_VALUE + colorNoise + (paletteIndex = 1);
            }
            else if ((coords.y < Blocks[index]) && (coords.y > (Blocks[index] - 4)))
            {
                OutputDistances[index] = _SOLID_BASE_VALUE + colorNoise + (paletteIndex = 2);
            }
            else
            {
                OutputDistances[index] = _SOLID_BASE_VALUE + colorNoise + (paletteIndex = 3);
            }
        }
        else
        {
            float distance = FindMaximumJump(coords) / (float)_GridSize;

            OutputDistances[index] = distance;
        }
    }

    private int FindMaximumJump(int3 coords)
    {
        int jumpSize = 0;

        while ((jumpSize < _GridSize)
               && IsBoundingBoxEmpty(
                   math.clamp(coords - (jumpSize + 1), 0, _GridSize - 1),
                   math.clamp(coords + (jumpSize + 1), 0, _GridSize - 1)))
        {
            jumpSize += 1;
        }

        return jumpSize;
    }

    private bool IsBoundingBoxEmpty(int3 start, int3 end)
    {
        for (int z = start.z; z <= end.z; z++)
        for (int y = start.y; y <= end.y; y++)
        for (int x = start.x; x <= end.x; x++)
        {
            StaticMath.Project1D_XYZ(x, y, z, _GridSize, out int index);
            if (IsBlockSolid(index))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlockSolid(int index) => Blocks[index] > -1;
}
