using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wiggle_TEMP : MonoBehaviour
{
    public float pendulumDistance;
    public float rotationAngles;
     // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(0f, pendulumDistance * Mathf.Sin(Time.time) * Time.deltaTime, 0f));
        transform.Rotate(new Vector3(0f, rotationAngles * Time.deltaTime, 0f));
    }
}
