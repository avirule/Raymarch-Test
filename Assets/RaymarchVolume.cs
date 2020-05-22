#region

using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    public const int GridSize = 24;
    private static readonly int _VoxelGridSize = Shader.PropertyToID("_VoxelGridSize");
    private static readonly int _RaymarchTexture = Shader.PropertyToID("_RaymarchTexture");

    private bool[] _Blocks;

    private Texture3D _Texture;

    public MeshRenderer MeshRenderer;


    // Start is called before the first frame update
    private void Start()
    {
        _Texture = new Texture3D(GridSize, GridSize, GridSize, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Vector3 origin = transform.position * GridSize;
        _Blocks = new bool[GridSize * GridSize * GridSize];

        int index = 0;
        for (int y = 0; y < GridSize; y++)
        {
            for (int z = 0; z < GridSize; z++)
            {
                for (int x = 0; x < GridSize; x++, index++)
                {
                    Vector3 asOrigin = origin + new Vector3(x, 0f, z);
                    float noise = Mathf.PerlinNoise(asOrigin.x / 1000f, asOrigin.z / 100f);
                    int noiseHeight = (int)(GridSize * noise);

                    _Blocks[index] = y <= noiseHeight;
                }
            }
        }

        MakeJumpTexture();

        _Texture.Apply();

        MeshRenderer.material.SetTexture(_RaymarchTexture, _Texture);
        MeshRenderer.material.SetInt(_VoxelGridSize, GridSize);
    }

    private void MakeJumpTexture()
    {
        CreateTerrainTexture createTerrainTexture = new CreateTerrainTexture(GridSize, _Blocks);
        JobHandle jobHandle = createTerrainTexture.Schedule(_Blocks.Length, 256);
        jobHandle.Complete();

        int index = 0;
        for (int y = 0; y < GridSize; y++)
        for (int z = 0; z < GridSize; z++)
        for (int x = 0; x < GridSize; x++, index++)
        {
            byte distance = createTerrainTexture.OutputDistances[index];

            _Texture.SetPixel(x, y, z, distance >= 1f ? Color.white : new Color(0f, 0f, 0f, distance / (float)GridSize));

            // if (IsSolid(x, y, z))
            // {
            //     float3 colorNoise = random.Next(-50, 51) * 0.0005f;
            //
            //     if (y == _Blocks[x][y][z])
            //     {
            //         float3 color = new float3(0.38f, 0.59f, 0.20f) + colorNoise;
            //         _Texture.SetPixel(x, y, z, new Color(color.x, color.y, color.z, 1f));
            //     }
            //     else if ((y < _Blocks[x][y][z]) && (y > (_Blocks[x][y][z] - 3)))
            //     {
            //         float3 color = new float3(0.36f, 0.25f, 0.2f) + colorNoise;
            //         _Texture.SetPixel(x, y, z, new Color(color.x, color.y, color.z, 1f));
            //     }
            //     else
            //     {
            //         float3 color = new float3(0.41f) + colorNoise;
            //         _Texture.SetPixel(x, y, z, new Color(color.x, color.y, color.z, 1f));
            //     }
            // }
            // else
            // {
            //     _Texture.SetPixel(x, y, z, new Color(0f, 0f, 0f, FindMaximumJump(x, y, z) / (float)GridSize));
            // }
        }

        createTerrainTexture.OutputDistances.Dispose();
        createTerrainTexture.Blocks.Dispose();
    }

    // private int FindMaximumJump(int startX, int startY, int startZ)
    // {
    //     int jumpSize = 0;
    //
    //     while (IsEmpty(startX - (jumpSize + 1), startY - (jumpSize + 1), startZ - (jumpSize + 1), startX + jumpSize + 1, startY + jumpSize + 1,
    //                startZ + jumpSize + 1)
    //            && (jumpSize < GridSize))
    //     {
    //         jumpSize += 1;
    //     }
    //
    //     return jumpSize;
    // }
    //
    // private bool IsEmpty(int startX, int startY, int startZ, int endX, int endY, int endZ)
    // {
    //     for (int x = startX; x <= endX; x++)
    //     for (int y = startY; y <= endY; y++)
    //     for (int z = startZ; z <= endZ; z++)
    //     {
    //         if (IsSolid(x, y, z))
    //         {
    //             return false;
    //         }
    //     }
    //
    //     return true;
    // }
    //
    // private bool IsSolid(int x, int y, int z) =>
    //     (x >= 0) && (y >= 0) && (z >= 0) && (x < GridSize) && (y < GridSize) && (z < GridSize) && (_Blocks[x][y][z] > -1);
}
