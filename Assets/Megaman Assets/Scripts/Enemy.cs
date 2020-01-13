using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public abstract class Enemy : MonoBehaviour
{
    #region COMPONENT REFS 
    [SerializeField]
    public Transform Player;
    public Rigidbody m_Rigidbody;
    public Animator m_Animator;
    [SerializeField]
    public List<Transform> ControlBones;
    #endregion

    #region STATIC PROPERTIES
    [SerializeField]
    public float m_speed = 1f;
    #endregion

    #region VARIABLE PROPERTIES
    [SerializeField]
    public bool hasTarget = false;
    #endregion


    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();
        Player = Transform.FindObjectOfType<Player>().transform;
    }

    private void Update()
    {
        if (hasTarget)
        {
            DoMovement();
        }

    }

    public virtual void DoMovement()
    {
        Vector3 myPos = transform.position;
        //while(transform.position != Player.transform.position)
        //{
        Vector3 slerp = Vector3.Lerp(myPos, new Vector3(Player.transform.position.x, transform.position.y, Player.transform.position.z), m_speed * Time.deltaTime);
        m_Rigidbody.MovePosition(slerp);
            //yield return null;
        //}
 
    }

    public virtual void ChangeSpeed(string attackName)
    {
        switch (attackName)
        {
            case "AlternateHammers":
                m_speed = 0.3f;
                ChangeLegStepSpeed(1f);
                break;
            case "SpinMove":
                m_speed = 0.1f;
                ChangeLegStepSpeed(1f);
                break;
        }

    }

    public abstract void ChangeLegStepSpeed(float stepDistance);

}