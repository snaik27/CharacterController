using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


public class LegStepper : MonoBehaviour
{
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Transform constrainedBone;
    [SerializeField]
    public float stepHeight;
    private Vector3 stepPosition;

    public float stepDistance;
    public float moveDuration;

    public bool isMoving;

    private void Awake()
    {
        Physics.IgnoreLayerCollision(10, 10);

        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;

        constrainedBone = GetComponent<DampedTransform>().data.constrainedObject;
        stepPosition = transform.position;
    }

    public void TryTakeStep()
    {
        if (Vector3.Distance(transform.position, constrainedBone.transform.position) > stepDistance && !isMoving)
        {
            StartCoroutine(StepToHome());
        }
    }

    public IEnumerator StepToHome()
    {
        isMoving = true;

        //GetComponent<DampedTransform>().data.dampPosition = 0f;

        float timeElapsed = 0;

        Vector3 centerPoint;
        Vector3 endPoint;

        while (timeElapsed < moveDuration)
        {
            endPoint = new Vector3(transform.position.x, 0f, transform.position.z);
            centerPoint = new Vector3(transform.position.x, stepHeight, transform.position.z);
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveDuration;
            normalizedTime = EaseInOut_Cubic(normalizedTime);

            GetComponent<DampedTransform>().data.dampPosition = Mathf.Lerp(GetComponent<DampedTransform>().data.dampPosition, 0, normalizedTime);

            transform.position = Vector3.Lerp(Vector3.Lerp(transform.position, centerPoint, normalizedTime),
                                                Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                                                normalizedTime);

            yield return null;
        }

        GetComponent<DampedTransform>().data.dampPosition = 1f;

        isMoving = false;        
    }

    public float EaseInOut_Cubic(float value)
    {
        if ((value *= 2f) < 1f) return 0.5f * value * value * value;
        return 0.5f * ((value -= 2f) * value * value + 2f);
    }
}
