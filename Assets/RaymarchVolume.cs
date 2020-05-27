#region

using System.Diagnostics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private const int _GRID_SIZE = 12;
    private const int _GRID_SIZE_CUBED = _GRID_SIZE * _GRID_SIZE * _GRID_SIZE;
    private const int _DEPTH_TEXTURE_SCALING_FACTOR = 2;
    private const float _FREQUENCY = 0.0075f;
    private const float _PERSISTENCE = 0.6f;

    private static readonly int _RandomSamplerTexture = Shader.PropertyToID("_RandomSamplerTexture");
    private static readonly int _RaymarchTextureKernel = Shader.PropertyToID("_RaymarchTexture");
    private static readonly int _DepthTextureKernel = Shader.PropertyToID("_DepthTexture");

    private int _Seed;

    public Material RaymarchMaterial;
    public RenderTexture DepthTexture;
    public Texture3D RaymarchVolumeTexture;
    public Texture2D RandomSamplerTexture;
    public Mesh CubeMesh;

    private void Start()
    {
        DepthTexture = new RenderTexture(Screen.currentResolution.width / _DEPTH_TEXTURE_SCALING_FACTOR,
            Screen.currentResolution.height / _DEPTH_TEXTURE_SCALING_FACTOR, 0, RenderTextureFormat.RFloat)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            antiAliasing = 2
        };
        RaymarchVolumeTexture = new Texture3D(_GRID_SIZE, _GRID_SIZE, _GRID_SIZE, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        RandomSamplerTexture = new Texture2D(Screen.currentResolution.width / _DEPTH_TEXTURE_SCALING_FACTOR,
            Screen.currentResolution.height / _DEPTH_TEXTURE_SCALING_FACTOR, TextureFormat.RFloat, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        for (int x = 0; x < RandomSamplerTexture.width; x++)
        for (int z = 0; z < RandomSamplerTexture.height; z++)
        {
            float noise = Mathf.Pow(Mathf.PerlinNoise(x / (float)RandomSamplerTexture.width, z / (float)RandomSamplerTexture.height), 0.986f);
            RandomSamplerTexture.SetPixel(x, z, new Color(noise, 0f, 0f, 1f));
        }

        RandomSamplerTexture.Apply();
        RaymarchMaterial.SetTexture(_RandomSamplerTexture, RandomSamplerTexture);

        _Seed = "afffakka".GetHashCode();

        Stopwatch stopwatch = Stopwatch.StartNew();

        CreateWorldDataJob createWorldDataJob = new CreateWorldDataJob(_GRID_SIZE, _Seed, _FREQUENCY, _PERSISTENCE);
        JobHandle worldDataJobHandle = createWorldDataJob.Schedule(_GRID_SIZE_CUBED, 64);

        CreateRaymarchTextureJob createRaymarchTextureJob = new CreateRaymarchTextureJob((uint)_Seed, _GRID_SIZE, createWorldDataJob.WorldData);
        createRaymarchTextureJob.Schedule(_GRID_SIZE_CUBED, 64, worldDataJobHandle).Complete();

        RaymarchVolumeTexture.SetPixelData(createRaymarchTextureJob.OutputDistances, 0);
        RaymarchVolumeTexture.Apply();

        createRaymarchTextureJob.OutputDistances.Dispose();
        createRaymarchTextureJob.Blocks.Dispose();

        RaymarchMaterial.SetTexture(_RaymarchTextureKernel, RaymarchVolumeTexture);

        Debug.Log($"{stopwatch.Elapsed.TotalMilliseconds:0.00}ms");
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
}
