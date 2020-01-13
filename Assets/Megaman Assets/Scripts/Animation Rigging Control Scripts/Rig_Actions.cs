using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public abstract class Rig_Actions : MonoBehaviour
{
    #region COMPONENT REFERENCES
    public List<Transform> controlBones;
    public Rig lookAtRig;

    private Enemy enemy;
    #endregion

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }


    public virtual IEnumerator ProceduralWalk()
    {
        yield return null;
    }

    public virtual void ToggleLookAtRig()
    {
        StartCoroutine(LerpToggleRig(lookAtRig));
    }
    public virtual void ToggleMove(Transform other, string moveName)
    {
     
    }
    public virtual IEnumerator LerpToggleRig(Rig rig)
    {
        float min = rig.weight;
        float max = 1 - rig.weight;
        float t = 0.0f;
        while (rig.weight != max)
        {
            rig.weight = Mathf.Lerp(min, max, t);
            t += 0.5f * Time.deltaTime;
            yield return null;
        }
    }
    public virtual IEnumerator LerpFloat(TwoBoneIKConstraint twoBone, float lerpTo)
    {
        float timeElapsed = 0;
        float moveDuration = 0.5f;
        float currentWeight = twoBone.weight;
        while (timeElapsed < moveDuration)
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveDuration;

            twoBone.weight = Mathf.Lerp(twoBone.weight, lerpTo, normalizedTime);

            yield return null;
        }
    }
 

}
