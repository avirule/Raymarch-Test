using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPS : MonoBehaviour
{
    public Text TextObject;

    // Start is called before the first frame update
    void Start()
    {

    }

    private int skippedUpdates = 0;

    // Update is called once per frame
    void Update()
    {
        if (skippedUpdates == 4)
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
