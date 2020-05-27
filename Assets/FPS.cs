#region

using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

public class FPS : MonoBehaviour
{
    private int skippedUpdates;
    public TextMeshProUGUI TextObject;

    // Start is called before the first frame update
    private void Start() { }

    // Update is called once per frame
    private void Update()
    {
        if (skippedUpdates == 8)
        {
            skippedUpdates = 0;

            TextObject.text = $"{1f / Time.deltaTime:0.00}";
        }
        else
        {
            skippedUpdates += 1;
        }
    }
}
