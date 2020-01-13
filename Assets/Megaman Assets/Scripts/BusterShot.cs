using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all Buster Shot logic
/// </summary>
public class BusterShot : MonoBehaviour
{
    #region PROPERTIES
    public Rigidbody m_Rigidbody;
    public SphereCollider m_Collider;
    public MeshRenderer m_MeshRenderer;
    private Material m_MAT;
    private static Vector3 m_ShotSize = new Vector3( .003f, 0.003f, 0.003f);
    private static Vector3 m_WaitingSize = new Vector3(0.003f, 0.003f, 0.003f);
    public Transform originalParent;
    private int speed = 1;
    public float m_Lifetime = 5f; // --------------Should be tuned later and then returned to a private variable
    #endregion

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<SphereCollider>();
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_MAT = m_MeshRenderer.sharedMaterial;
    }

    private void Update()
    {
        if (transform.parent != originalParent)
        {
            //transform.Translate(transform.up * Time.deltaTime);
            m_Rigidbody.MovePosition(transform.position + (0.25f * transform.up));
        }
    }

    private IEnumerator LifetimeInSeconds(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        Destroy(gameObject);
    }
    
    public void ShootBuster()
    {
        StartCoroutine(LifetimeInSeconds(m_Lifetime));
        transform.localScale = m_ShotSize;
        m_MeshRenderer.enabled = true;
        m_Rigidbody.isKinematic = false;
        m_Collider.isTrigger = false;
        transform.parent = null;
        //StartCoroutine(ForceReloadShot());
    }
    private IEnumerator ForceReloadShot()
    {
        yield return new WaitForSecondsRealtime(3);
        if (transform.parent != originalParent)
            StartCoroutine(ReloadShot());
    }
    public IEnumerator ReloadShot()
    {
        transform.parent = originalParent;
        yield return new WaitForSecondsRealtime(0.2f);
        m_Rigidbody.velocity = Vector3.zero;
        transform.position = new Vector3(0f, 0.01f, 0f);
        transform.localScale = m_WaitingSize;
        m_MeshRenderer.enabled = false;
        m_Rigidbody.isKinematic = true;
        m_Collider.isTrigger = true;
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
