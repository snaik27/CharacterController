using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IK_Follow : MonoBehaviour
{
    public Vector3 localPos;
    public Quaternion localRot;

    public Transform target = null;

    private void Awake()
    {
        localPos = transform.localPosition;
        localRot = transform.localRotation;
    }

    private void Update()
    {
        if (target != null)
        {
            transform.position = target.transform.position + new Vector3(0, 1, 0); ;
            //transform.position = Vector3.Lerp(transform.position, target.transform.position, Time.deltaTime);
        }
    }
}
