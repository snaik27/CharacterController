using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IK_Foot_Stabilize : MonoBehaviour
{
    [SerializeField] public Transform IKTarget;
    // Start is called before the first frame update
    void Start()
    {
        //IKTarget = GetComponent<TwoBoneIKConstraint>().data.target;
    }

    // Update is called once per frame
    void Update()
    {
       // AdjustFeetToGround();
    }

    /// <summary>
    /// TODO:
    ///      1. Add layermask at some point
    /// </summary>
    //private void AdjustFeetToGround()
    //{
    //    //get normal of ground if its there
    //    RaycastHit hit = new RaycastHit();

    //    StopCoroutine(RotateFoot(hit));

    //    if (Physics.Raycast(IKTarget.transform.position, -IKTarget.transform.forward, out hit, 0.5f))
    //    {
    //        //slerp to the correct rotation
    //        if (hit.normal != IKTarget.transform.up)
    //        {
    //            StartCoroutine(RotateFoot(hit));
    //        }
    //    }  
    //}

    //private IEnumerator RotateFoot(RaycastHit hit)
    //{
    //    while (Vector3.Angle(transform.up, hit.normal) > 10f)
    //    {
    //        Vector3 newDir = Vector3.RotateTowards(transform.up, -hit.normal, 0.001f * Time.deltaTime, 0f);
    //        Debug.DrawRay(IKTarget.transform.position, newDir * 2, Color.red);

    //        transform.rotation = Quaternion.LookRotation(newDir, transform.up);
    //        yield return null;
    //    }
    //}
}
