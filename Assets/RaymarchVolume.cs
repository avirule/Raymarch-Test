#region

using System;
using System.Linq;
using System.Xml;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private static readonly int _Positions = Shader.PropertyToID("_Positions");
    private static readonly int _Length = Shader.PropertyToID("_Length");
    private static readonly int _VoxelSize = Shader.PropertyToID("_VoxelSize");
    private static readonly int _VoxelGridSize = Shader.PropertyToID("_VoxelGridSize");
    private static readonly int _RaymarchTexture = Shader.PropertyToID("_RaymarchTexture");

    private bool[][][] Blocks;

    public MeshRenderer MeshRenderer;

    private Texture3D Texture;

    // Start is called before the first frame update
    private void Start()
    {
        Texture = new Texture3D(32, 32, 32, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            anisoLevel = 3
        };

        Vector3 origin = transform.position * 32;

        Blocks = new bool[32][][];

        for (int x = 0; x < 32; x++)
        {
            Blocks[x] = new bool[32][];

            for (int z = 0; z < 32; z++)
            {
                Vector3 asOrigin = origin + new Vector3(x, 0f, z);
                float noise = Mathf.PerlinNoise(asOrigin.x / 1000f, asOrigin.z / 100f);
                int noiseHeight = (int)(32 * noise);

                for (int y = 0; y < 32; y++)
                {
                    if (Blocks[x][y] == null)
                    {
                        Blocks[x][y]  = new bool[32];
                    }

                    Blocks[x][y][z] = y <= noiseHeight;
                }
            }
        }

        MakeJumpTexture();

        Debug.Log($"0, 00, 0: {Texture.GetPixel(0, 00, 0)} {Blocks[0][00][0]}");
        Debug.Log($"0, 10, 0: {Texture.GetPixel(0, 10, 0)} {Blocks[0][10][0]}");
        Debug.Log($"0, 11, 0: {Texture.GetPixel(0, 11, 0)} {Blocks[0][11][0]}");
        Debug.Log($"0, 12, 0: {Texture.GetPixel(0, 12, 0)} {Blocks[0][12][0]}");
        Debug.Log($"0, 13, 0: {Texture.GetPixel(0, 13, 0)} {Blocks[0][13][0]}");
        Debug.Log($"0, 14, 0: {Texture.GetPixel(0, 14, 0)} {Blocks[0][14][0]}");
        Debug.Log($"0, 15, 0: {Texture.GetPixel(0, 15, 0)} {Blocks[0][15][0]}");
        Debug.Log($"0, 16, 0: {Texture.GetPixel(0, 16, 0)} {Blocks[0][16][0]}");
        Debug.Log($"0, 17, 0: {Texture.GetPixel(0, 17, 0)} {Blocks[0][17][0]}");
        Debug.Log($"0, 18, 0: {Texture.GetPixel(0, 18, 0)} {Blocks[0][18][0]}");
        Debug.Log($"0, 19, 0: {Texture.GetPixel(0, 19, 0)} {Blocks[0][19][0]}");
        Debug.Log($"0, 20, 0: {Texture.GetPixel(0, 20, 0)} {Blocks[0][20][0]}");
        Debug.Log($"0, 21, 0: {Texture.GetPixel(0, 21, 0)} {Blocks[0][21][0]}");
        Debug.Log($"0, 22, 0: {Texture.GetPixel(0, 22, 0)} {Blocks[0][22][0]}");
        Debug.Log($"0, 23, 0: {Texture.GetPixel(0, 23, 0)} {Blocks[0][23][0]}");
        Debug.Log($"0, 24, 0: {Texture.GetPixel(0, 24, 0)} {Blocks[0][24][0]}");
        Debug.Log($"0, 25, 0: {Texture.GetPixel(0, 25, 0)} {Blocks[0][25][0]}");
        Debug.Log($"0, 26, 0: {Texture.GetPixel(0, 26, 0)} {Blocks[0][26][0]}");
        Debug.Log($"0, 27, 0: {Texture.GetPixel(0, 27, 0)} {Blocks[0][27][0]}");
        Debug.Log($"0, 28, 0: {Texture.GetPixel(0, 28, 0)} {Blocks[0][28][0]}");
        Debug.Log($"0, 29, 0: {Texture.GetPixel(0, 29, 0)} {Blocks[0][29][0]}");

        MeshRenderer.material.SetTexture(_RaymarchTexture, Texture);
        MeshRenderer.material.SetInt(_VoxelGridSize, 32);
    }

    private void MakeJumpTexture()
    {
        bool green = false;

        for (int x = 0; x < 32; x++)
        for (int y = 0; y < 32; y++)
        for (int z = 0; z < 32; z++)
        {
            Texture.SetPixel(x, y, z, IsSolid(x, y, z)
                    ? green ? new Color(0f, 1f, 0f, 1f) : Color.white
                    : new Color(0f, 0f, 0f, FindMaximumJump(x, y, z) / 32f));

            green = !green;
        }
    }

    private int FindMaximumJump(int startX, int startY, int startZ)
    {
        int jumpSize = 0;

        while (IsEmpty(startX - (jumpSize + 1), startY - (jumpSize + 1), startZ - (jumpSize + 1), startX + (jumpSize + 1), startY + (jumpSize + 1), startZ + (jumpSize + 1)) && (jumpSize < 32))
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

    private bool IsSolid(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < 32 & y < 32 && z < 32 && Blocks[x][y][z];
}
