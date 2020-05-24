﻿#region

using System;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = System.Random;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private const int _GRID_SIZE = 32;

    private static readonly int _RaymarchTextureKernel = Shader.PropertyToID("_RaymarchTexture");

    private short[] _Blocks;

    public Texture3D RaymarchVolumeTexture;
    public RenderTexture DepthTexture;
    public Material RaymarchMaterial;
    public Mesh CubeMesh;
    private static readonly int _DepthTextureKernel = Shader.PropertyToID("_DepthTexture");

    // Start is called before the first frame update
    private void Start()
    {
        RaymarchVolumeTexture = new Texture3D(_GRID_SIZE, _GRID_SIZE, _GRID_SIZE, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        DepthTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.Depth);

        Vector3 origin = transform.position * _GRID_SIZE;
        _Blocks = new short[_GRID_SIZE * _GRID_SIZE * _GRID_SIZE];

        int index = 0;
        for (int y = 0; y < _GRID_SIZE; y++)
        {
            for (int z = 0; z < _GRID_SIZE; z++)
            {
                for (int x = 0; x < _GRID_SIZE; x++, index++)
                {
                    Vector3 asOrigin = origin + new Vector3(x, 0f, z);
                    float noise = Mathf.PerlinNoise(asOrigin.x / 1000f, asOrigin.z / 100f);
                    short noiseHeight = (short)(_GRID_SIZE * noise);

                    _Blocks[index] = y <= noiseHeight ? noiseHeight : (short)-1;
                }
            }
        }

        MakeJumpTexture();

        RaymarchVolumeTexture.Apply();

        RaymarchMaterial.SetTexture(_RaymarchTextureKernel, RaymarchVolumeTexture);
        RaymarchMaterial.SetTexture(_DepthTextureKernel, DepthTexture);
    }

    private void OnPreRender()
    {
        RaymarchMaterial.SetPass(0);
        Graphics.Blit(DepthTexture, RaymarchMaterial);
    }

    private void OnRenderObject()
    {
        RaymarchMaterial.SetPass(1);
        Graphics.DrawMesh(CubeMesh, Matrix4x4.identity, RaymarchMaterial, 0);
    }

    private void MakeJumpTexture()
    {
        CreateTerrainTexture createTerrainTexture = new CreateTerrainTexture(_GRID_SIZE, _Blocks);
        JobHandle jobHandle = createTerrainTexture.Schedule(_Blocks.Length, 256);
        jobHandle.Complete();

        Random random = new Random();

        int index = 0;
        for (int y = 0; y < _GRID_SIZE; y++)
        for (int z = 0; z < _GRID_SIZE; z++)
        for (int x = 0; x < _GRID_SIZE; x++, index++)
        {
            if (_Blocks[index] > -1)
            {
                float3 colorNoise = random.Next(-50, 51) * 0.0005f;

                if (y == _Blocks[index])
                {
                    float3 color = new float3(0.38f, 0.59f, 0.20f) + colorNoise;
                    RaymarchVolumeTexture.SetPixel(x, y, z, new Color(color.x, color.y, color.z, 1f));
                }
                else if ((y < _Blocks[index]) && (y > (_Blocks[index] - 4)))
                {
                    float3 color = new float3(0.36f, 0.25f, 0.2f) + colorNoise;
                    RaymarchVolumeTexture.SetPixel(x, y, z, new Color(color.x, color.y, color.z, 1f));
                }
                else
                {
                    float3 color = new float3(0.41f) + colorNoise;
                    RaymarchVolumeTexture.SetPixel(x, y, z, new Color(color.x, color.y, color.z, 1f));
                }
            }
            else
            {
                RaymarchVolumeTexture.SetPixel(x, y, z, new Color(0f, 0f, 0f, createTerrainTexture.OutputDistances[index] / (float)_GRID_SIZE));
            }
        }

        createTerrainTexture.OutputDistances.Dispose();
        createTerrainTexture.Blocks.Dispose();
    }
}
