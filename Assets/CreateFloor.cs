#region

using UnityEngine;

#endregion

public class CreateFloor : MonoBehaviour
{
    public static CreateFloor Instance;
    public GameObject CubePrefab;

    public Material Material;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    private void Start()
    {
        const int radius = 0;

        for (int x = -radius; x < (radius + 1); x++)
        for (int z = -radius; z < (radius + 1); z++)
        {
            Instantiate(CubePrefab, new Vector3(x, 0f, z), Quaternion.identity, transform);
        }
    }

    // Update is called once per frame
    private void Update() { }
}
