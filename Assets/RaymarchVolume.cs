#region

using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#endregion

public class RaymarchVolume : MonoBehaviour
{
    private const int _DEPTH_TEXTURE_SCALING_FACTOR = 2;
    private const float _FREQUENCY = 0.0075f;
    private const float _PERSISTENCE = 0.6f;

    private static readonly int _RandomSamplerTexture = Shader.PropertyToID("_RandomSamplerTexture");
    private static readonly int _AccelerationDataBufferKernel = Shader.PropertyToID("_AccelerationData");
    private static readonly int _DepthTextureKernel = Shader.PropertyToID("_DepthTexture");
    private static readonly int _ColorPaletteKernel = Shader.PropertyToID("_ColorPalette");
    private static readonly int _WorldEdgeLengthKernel = Shader.PropertyToID("_WorldEdgeLength");

    private int _Seed;

    private readonly Color[] _ColorPalette =
    {
        new Color(0f, 0f, 0f, 0f),
        new Color(0.38f, 0.59f, 0.20f, 1f),
        new Color(0.36f, 0.25f, 0.2f, 1f),
        new Color(0.41f, 0.41f, 0.41f, 1f),
    };

    public Transform Transform;
    public Material RaymarcherMaterial;
    public RenderTexture DepthTexture;
    public Texture2D RandomSamplerTexture;
    public Mesh CubeMesh;

    public int GridSize = 128;
    public bool Regenerate = true;

    private void Start()
    {
        RaymarcherMaterial.SetColorArray(_ColorPaletteKernel, _ColorPalette);

        DepthTexture = new RenderTexture(Screen.currentResolution.width / _DEPTH_TEXTURE_SCALING_FACTOR,
            Screen.currentResolution.height / _DEPTH_TEXTURE_SCALING_FACTOR, 0, RenderTextureFormat.RFloat)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            antiAliasing = 2
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
        RaymarcherMaterial.SetTexture(_RandomSamplerTexture, RandomSamplerTexture);

        _Seed = "afffakka".GetHashCode();
    }

    private void Update()
    {
        if (!Regenerate)
        {
            return;
        }

        try
        {
            Generate();
        }
        finally
        {
            Regenerate = false;
        }
    }

    private void OnRenderObject()
    {
        RenderTexture previousRenderTarget = Camera.current.activeTexture;

        if (RaymarcherMaterial.SetPass(0))
        {
            Graphics.SetRenderTarget(DepthTexture);

            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            Graphics.DrawMeshNow(CubeMesh, Matrix4x4.identity);
            RaymarcherMaterial.SetTexture(_DepthTextureKernel, DepthTexture);
        }


        if (RaymarcherMaterial.SetPass(1))
        {
            Graphics.SetRenderTarget(previousRenderTarget);
            Graphics.DrawMeshNow(CubeMesh, Transform.position, Transform.rotation);
        }
    }

    private void Generate()
    {
        int gridSizeCubed = GridSize * GridSize * GridSize;

        CreateWorldDataJob createWorldDataJob = new CreateWorldDataJob(GridSize, _Seed, _FREQUENCY, _PERSISTENCE);
        JobHandle worldDataJobHandle = createWorldDataJob.Schedule(gridSizeCubed, 64);

        CreateRaymarchTextureJob createRaymarchTextureJob = new CreateRaymarchTextureJob(GridSize, createWorldDataJob.WorldData);
        createRaymarchTextureJob.Schedule(gridSizeCubed, 64, worldDataJobHandle).Complete();
        ComputeBuffer accelerationData = new ComputeBuffer(gridSizeCubed, sizeof(float), ComputeBufferType.Structured);
        accelerationData.SetData(createRaymarchTextureJob.OutputDistances);

        RaymarcherMaterial.SetInt(_WorldEdgeLengthKernel, GridSize);
        RaymarcherMaterial.SetBuffer(_AccelerationDataBufferKernel, accelerationData);

        createRaymarchTextureJob.OutputDistances.Dispose();
        createRaymarchTextureJob.Blocks.Dispose();
    }
}
