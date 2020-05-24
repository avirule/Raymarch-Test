#region

using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = System.Random;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private const int _GRID_SIZE = 32;
    private const int _DEPTH_TEXTURE_RESOLUTION = 512;
    private const int _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING = 2;

    private static readonly int _RaymarchTextureKernel = Shader.PropertyToID("_RaymarchTexture");
    private static readonly int _DepthTextureKernel = Shader.PropertyToID("_DepthTexture");

    private short[] _Blocks;

    public Material RaymarchMaterial;
    public RenderTexture DepthTexture;
    public Texture3D RaymarchVolumeTexture;
    public Mesh CubeMesh;

    private void Start()
    {
        RaymarchVolumeTexture = new Texture3D(_GRID_SIZE, _GRID_SIZE, _GRID_SIZE, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        DepthTexture = new RenderTexture(Screen.currentResolution.width / _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING,
            Screen.currentResolution.height / _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING, 0, RenderTextureFormat.RFloat)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            antiAliasing = 2
        };

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
    }

    private void OnRenderObject()
    {
        RenderTexture previousRenderTarget = Camera.current.activeTexture;

        if (RaymarchMaterial.SetPass(0))
        {
            Graphics.SetRenderTarget(DepthTexture);

            GL.Clear(true, true, Color.black);

            Graphics.DrawMeshNow(CubeMesh, Matrix4x4.identity, 0);
            RaymarchMaterial.SetTexture(_DepthTextureKernel, DepthTexture);
        }


        if (RaymarchMaterial.SetPass(1))
        {
            Graphics.SetRenderTarget(previousRenderTarget);
            Graphics.DrawMeshNow(CubeMesh, Matrix4x4.identity, 0);
        }
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
