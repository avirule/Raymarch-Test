#region

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = System.Random;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private static readonly int _Positions = Shader.PropertyToID("_Positions");
    private static readonly int _Length = Shader.PropertyToID("_Length");
    private static readonly int _VoxelSize = Shader.PropertyToID("_VoxelSize");
    private static readonly int _VoxelGridSize = Shader.PropertyToID("_VoxelGridSize");
    private static readonly int _RaymarchTexture = Shader.PropertyToID("_RaymarchTexture");

    private bool[][][] _Blocks;

    private Color[] _Colors =
    {
        Color.white,
        Color.blue,
        Color.yellow,
        Color.green,
    };

    private Texture3D _Texture;

    public MeshRenderer MeshRenderer;


    // Start is called before the first frame update
    private void Start()
    {
        _Texture = new Texture3D(32, 32, 32, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Vector3 origin = transform.position * 32;
        _Blocks = new bool[32][][];

        for (int x = 0; x < 32; x++)
        {
            _Blocks[x] = new bool[32][];

            for (int z = 0; z < 32; z++)
            {
                _Blocks[x][z] = new bool[32];
            }
        }

        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                Vector3 asOrigin = origin + new Vector3(x, 0f, z);
                float noise = Mathf.PerlinNoise(asOrigin.x / 1000f, asOrigin.z / 100f);
                int noiseHeight = (int)(32 * noise);

                for (int y = 0; y < 32; y++)
                {
                    _Blocks[x][y][z] = y <= noiseHeight;
                }
            }
        }

        MakeJumpTexture();

        _Texture.Apply();

        MeshRenderer.sharedMaterial.SetTexture(_RaymarchTexture, _Texture);
        MeshRenderer.sharedMaterial.SetInt(_VoxelGridSize, 32);
    }

    private void MakeJumpTexture()
    {
        Random rand = new Random(transform.position.GetHashCode());
        int currentColor = 0;

        for (int x = 0; x < 32; x++)
        for (int y = 0; y < 32; y++)
        for (int z = 0; z < 32; z++)
        {
            if (IsSolid(x, y, z))
            {
                _Texture.SetPixel(x, y, z, _Colors[currentColor]);
                currentColor = (currentColor + rand.Next(0, _Colors.Length)) % _Colors.Length;
            }
            else
            {
                _Texture.SetPixel(x, y, z, new Color(0f, 0f, 0f, FindMaximumJump(x, y, z) / 32f));
            }
        }
    }

    private int FindMaximumJump(int startX, int startY, int startZ)
    {
        int jumpSize = 0;

        while (IsEmpty(startX - (jumpSize + 1), startY - (jumpSize + 1), startZ - (jumpSize + 1), startX + jumpSize + 1, startY + jumpSize + 1,
                   startZ + jumpSize + 1)
               && (jumpSize < 32))
        {
            jumpSize += 1;
        }

        return jumpSize;
    }

    private bool IsEmpty(int startX, int startY, int startZ, int endX, int endY, int endZ)
    {
        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        for (int z = startZ; z <= endZ; z++)
        {
            if (IsSolid(x, y, z))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSolid(int x, int y, int z) => (x >= 0) && (y >= 0) && (z >= 0) && ((x < 32) & (y < 32)) && (z < 32) && _Blocks[x][y][z];
}
