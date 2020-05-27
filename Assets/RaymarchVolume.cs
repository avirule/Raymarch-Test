#region

using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private const int _GRID_SIZE = 128;
    private const int _DEPTH_TEXTURE_RESOLUTION = 512;
    private const int _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING = 2;
    private const float _FREQUENCY = 0.0075f;
    private const float _PERSISTENCE = 0.6f;

    private static readonly int _RaymarchTextureKernel = Shader.PropertyToID("_RaymarchTexture");
    private static readonly int _DepthTextureKernel = Shader.PropertyToID("_DepthTexture");

    private int _Seed;
    private short[] _Blocks;

    public Material RaymarchMaterial;
    public RenderTexture DepthTexture;
    public Texture3D RaymarchVolumeTexture;
    public Texture2D RandomSamplerTexture;
    public Mesh CubeMesh;
    private static readonly int _RandomSamplerTexture = Shader.PropertyToID("_RandomSamplerTexture");

    private void Start()
    {
        DepthTexture = new RenderTexture(Screen.currentResolution.width / _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING,
            Screen.currentResolution.height / _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING, 0, RenderTextureFormat.RFloat)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            antiAliasing = 2
        };
        RaymarchVolumeTexture = new Texture3D(_GRID_SIZE, _GRID_SIZE, _GRID_SIZE, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        RandomSamplerTexture = new Texture2D(Screen.currentResolution.width / _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING,
            Screen.currentResolution.height / _DEPTH_TEXTURE_RESOLUTION_DOWNSCALING, TextureFormat.RFloat, false)
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

        _Blocks = new short[_GRID_SIZE * _GRID_SIZE * _GRID_SIZE];

        _Seed = "afffakka".GetHashCode();

        CreateWorldDataJob createWorldDataJob = new CreateWorldDataJob(_GRID_SIZE, _Seed, _FREQUENCY, _PERSISTENCE);
        createWorldDataJob.Schedule(_Blocks.Length, 64).Complete();
        createWorldDataJob.WorldData.CopyTo(_Blocks);
        createWorldDataJob.WorldData.Dispose();

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
        CreateJumpTextureJob createJumpTextureJob = new CreateJumpTextureJob((uint)_Seed, _GRID_SIZE, _Blocks);
        JobHandle jobHandle = createJumpTextureJob.Schedule(_Blocks.Length, 64);
        jobHandle.Complete();

        RaymarchVolumeTexture.SetPixelData(createJumpTextureJob.OutputDistances, 0);

        createJumpTextureJob.OutputDistances.Dispose();
        createJumpTextureJob.Blocks.Dispose();
    }
}
