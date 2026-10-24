using System;
using UnityEngine;

public class Fire : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var spacing = GlobalVars.distance - GlobalVars.fireDistance;
        transform.position = new Vector3(0.0f,-3.0f - (spacing / 10.0f),00.0f);
    }
}
