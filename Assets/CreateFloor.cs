#region

using UnityEngine;

#endregion

public class CreateFloor : MonoBehaviour
{
    public GameObject CubePrefab;

    // Start is called before the first frame update
    private void Start()
    {
        const int radius = 1;

        for (int x = -radius; x < (radius + 1); x++)
        for (int z = -radius; z < (radius + 1); z++)
        {
            Instantiate(CubePrefab, new Vector3(x, 0f, z), Quaternion.identity, transform);
        }
    }
}
