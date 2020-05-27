#region

using UnityEngine;

#endregion

public class CreateFloor : MonoBehaviour
{
    public GameObject CubePrefab;
    public int Radius;

    // Start is called before the first frame update
    private void Start()
    {
        for (int x = -Radius; x < (Radius + 1); x++)
        for (int z = -Radius; z < (Radius + 1); z++)
        {
            Instantiate(CubePrefab, new Vector3(x, 0f, z), Quaternion.identity, transform);
        }
    }
}
