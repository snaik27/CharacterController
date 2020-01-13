using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;
using UnityEngine.Animations.Rigging;

public class Megaman_Rig : Rig_Actions
{
    #region COMPONENT REFERENCES
    //public List<Transform> controlBones;
    //public Rig lookAtRig;
    public Player player;
    #endregion

    #region INHERITED FUNCTIONALITY
    //public override void ToggleLookAtRig()
    #endregion
    private void Awake()
    {
        player = GetComponent<Player>();
    }
    public void ToggleLookAtRig(Transform lookAtTarget)
    {
        StartCoroutine(LerpToggleRig(lookAtRig, lookAtTarget));
    }
    public IEnumerator LerpToggleRig(Rig rig, Transform lookatTarget)
    {
        float min, max;
        float t = 0;
        //if target is null, make rig weight 0 and remove targets for chest and head
        //if target is there, make rig weight 1 and set new targets for chest and head
        try
        {
            min = rig.weight;
            max = 1 - rig.weight;
        }
        catch(MissingReferenceException)
        {
            min = rig.weight;
            max = 0;
        }
        while (rig.weight != max / 2)
        {
            rig.weight = Mathf.Lerp(min, max / 2, t);
            t += 0.5f * Time.deltaTime;
            yield return null;
        }

        if (max > 0.99f)
        {
            foreach (Transform bone in controlBones)
            {
                if (bone.name.Contains("Chest") || bone.name.Contains("Head"))
                {
                    bone.GetComponent<FollowTarget>().target = lookatTarget;
                }
            }
        }
        else if (max < 0.01f)
        {
            foreach (Transform bone in controlBones)
            {
                if (bone.name.Contains("Chest") || bone.name.Contains("Head"))
                {
                    bone.GetComponent<FollowTarget>().target = null;
                }
            }
        }
  
       
        while (rig.weight != max)
        {
            rig.weight = Mathf.Lerp(min, max, t);
            t += 0.5f * Time.deltaTime;
            yield return null;
        }
    }

    public override void ToggleMove(Transform other, string moveName)
    {
        switch (moveName)
        {
            case "AimBuster":
                ToggleAimBuster(other);
                break;
        }
    }

    private void ToggleAimBuster(Transform other)
    {
        foreach (Transform bone in controlBones)
        {
            if (bone.name == "Arm_IK_L")
            {
                IK_Follow ik_follow = bone.GetComponent<IK_Follow>();
                TwoBoneIKConstraint twobone = bone.GetComponent<TwoBoneIKConstraint>();
                if (other != null)
                {
                    ik_follow.target = other;
                    StartCoroutine(LerpFloat(twobone, 1f));
                }
                else
                {
                    ik_follow.target = null;
                    StartCoroutine(LerpFloat(twobone, 0f));
                }
            }
        }
    }
}
