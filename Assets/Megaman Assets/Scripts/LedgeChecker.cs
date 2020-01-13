using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeChecker : MonoBehaviour
{

    public Vector3 closestVertex; //world position closest vertex
    public float maxGrabDistance = 0.3f; //max distance allowed to grab from collision point
    public bool canLedgeGrab = false;
    private Rigidbody m_Rigidbody;

    private Transform collisionTransform;
    public Mesh collisionMesh;
    public Collider HitCollider;

    public List<int> grabbableVertexIndices;

    private Vector3 xzProjectedNormal;
    private Vector3 normalsNormal;

    public Vector3 ledge_point;
    private Vector3 normal_point;

    public Mesh lastCollisionMesh;
    private int closest;
    private int lastClosest;
    public List<int> lastGrabbableVertexIndices;



    [SerializeField] CapsuleCollider capsuleCollider;

    // Layer mask for world geometry. Make sure to ignore the player!
    [SerializeField] LayerMask geometryMask = ~0;
    // How high up above root the hands of the character will be
    [SerializeField] float grabHeight = 0.1f;
    // How far our horizontal rays go
    [SerializeField] float horizontalDistanceCheck = 0.25f;
    // How far our vertical rays go
    [SerializeField] float verticalDistanceCheck = 0.25f;
    // How steep the surface above the ledge can be for it to be considered a ledge
    [SerializeField] float maxSlopeAngle = 0;
    // Distance of the final capsule cast
    [SerializeField] float capsuleCastCheckDistance = 0.75f;
    // Buffer for our nonalloc physics checks
    Collider[] collisionResults = new Collider[20];

    /// <summary>
    /// Searches for a ledge in front of this object's transform.
    /// </summary>
    /// <param name="ledgePoint">Point on the ledge we want to grab</param>
    /// <param name="ledgeNormal">The normal of the ledge point</param>
    /// <returns>Returns whether a ledge was found or not</returns>
    bool TryGetLedgeGrabPoint(out Vector3 ledgePoint, out Vector3 ledgeNormal)
    {
        // Set our out variables
        ledgePoint = Vector3.zero;
        ledgeNormal = Vector3.zero;

        // Check if the space over the top of our head is empty
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position + new Vector3(0, grabHeight, 0),
            capsuleCollider.radius,
            collisionResults,
            geometryMask,
            QueryTriggerInteraction.Ignore
        );

        // If we hit anything at all, abort
        if (hitCount != 0)
        {
            Debug.DrawLine(transform.position + Vector3.up * grabHeight, Vector3.up, Color.red);
            return false;
        }

        int verticalHits = 0;

        // Horizontal rays to check above and over the ledge
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origin = transform.position + new Vector3(0, grabHeight, 0) + (transform.right * 0.5f) * capsuleCollider.radius * i;
            Debug.DrawRay(origin, transform.forward * horizontalDistanceCheck, Color.magenta);

            if (Physics.Raycast(origin, transform.forward, horizontalDistanceCheck, geometryMask, QueryTriggerInteraction.Ignore))
            {
                // Exit if it's not clear
                Debug.DrawRay(origin, transform.forward * horizontalDistanceCheck, Color.red);
                return false;
            }
            else
            {
                RaycastHit hit;

                // if no hit, cast down from the ends of our previous rays

                // Move origin to the end of the previous ray
                origin += transform.forward * horizontalDistanceCheck;

                Debug.DrawRay(origin, Vector3.down * verticalDistanceCheck, Color.blue);

                if (Physics.Raycast(origin, Vector3.down, out hit, verticalDistanceCheck, geometryMask, QueryTriggerInteraction.Ignore))
                {
                    // If end hits, check slope.
                    if (Vector3.Angle(hit.normal, Vector3.up) < maxSlopeAngle)
                    {
                        Debug.DrawRay(origin, Vector3.down * verticalDistanceCheck, Color.green);
                        verticalHits++;
                    }
                    else
                    {
                        Debug.DrawRay(origin, Vector3.down * verticalDistanceCheck, Color.red);
                    }
                }
            }
        }

        // We want at least two hits. 
        // This is sort of arbitrary, but one missed hit is usually acceptable.
        if (verticalHits < 2)
        {
            return false;
        }

        if (HitCollider)
        {
            GetClosestVertex(HitCollider);
            ledgePoint = ledge_point;
            ledgeNormal = normal_point;
            return true;
        }

        return false;
        //// Capsule cast to find the ledge point and normal

        //// 1 sphere above the character
        //Vector3 capsuleTop = transform.position + (capsuleCollider.height + capsuleCollider.radius * 2) * Vector3.up;
        //// 1 sphere behind the top sphere on the character capsule
        //Vector3 capsuleBottom = capsuleTop - Vector3.up * capsuleCollider.radius * 2;
        //capsuleBottom -= transform.forward * capsuleCollider.radius * 2;

        //// 45 degrees down from forward
        //Vector3 dir = (Vector3.down + transform.forward) / 2;

        //RaycastHit capsuleHit;

        //if (Physics.CapsuleCast(
        //    capsuleTop,
        //    capsuleBottom,
        //    capsuleCollider.radius,
        //    dir,
        //    out capsuleHit,
        //    capsuleCastCheckDistance,
        //    LayerMask.GetMask("Default", "Player"),
        //    QueryTriggerInteraction.Ignore
        //    ))
        //{
        //    // Success!
        //    ledgePoint = capsuleHit.point;
        //    ledgeNormal = capsuleHit.normal;
        //    return true;
        //}
        //else
        //{
        //    Debug.Log("Capsule hit failed");
        //}

        // No ledge found
        //return false;
    }

    void DoLedgeCheck()
    {
        Vector3 ledgePoint;
        Vector3 ledgeNormal;

        if (TryGetLedgeGrabPoint(out ledgePoint, out ledgeNormal))
        {
            // Assumes the capsule collider's bottom is at the transform's origin

            // Move the capsule down so that it meets the ledge at ledgeHangGrabHeight
            Vector3 targetPoint = ledgePoint - new Vector3(0, grabHeight, 0);
            // Move the target point away from the ledge so our capsule will be flush up against it
            targetPoint -= transform.forward * capsuleCollider.radius;

            //transform.position = targetPoint;
            // Look in the opposite direction to the normal
            // We want to hang down from the ledge so we strip out the Y component
            //transform.root.rotation = Quaternion.LookRotation(new Vector3(-ledgeNormal.x, 0, -ledgeNormal.z));

            // Exercise for the reader: Add some easing to make it nice and smooth!
        }
    }

    private void Awake()
    {
        //Physics.IgnoreLayerCollision(9, 9);
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        DoLedgeCheck();
    }

    /* TO DO:
     * Jobify reading
     * turn off collider when !isInAir
     * make sure min signed angle for mesh normals is a good enough value
     * CAN POTENTIALLY MAKE THIS MUCH MORE EFFICIENT BY DOING THIS AT LOAD TIME INSTEAD OF RUNTIME. Store grabbable vertex indices on each object in a List<int> and grab those when colliding instead!
     */
    private Vector3 GetClosestVertex(Collider other)
    {

        if (!other.GetComponent<MeshFilter>())
            return Vector3.zero;

        collisionMesh = (other.GetComponent<MeshFilter>()) ? other.GetComponent<MeshFilter>().mesh : other.GetComponent<SkinnedMeshRenderer>().sharedMesh;


        //collisionMesh.vertices.Length
        for (int index = 0; index < collisionMesh.vertexCount; index++)
        {
            //Is close enough and normal doesn't point down
            if (Vector3.Distance(transform.position, other.transform.position + collisionMesh.vertices[index]) < maxGrabDistance && collisionMesh.normals[index].y / Mathf.Abs(collisionMesh.normals[index].y) != -1)
            {
                //Has normal which is within feasible range to grab
                xzProjectedNormal = Vector3.ProjectOnPlane(collisionMesh.normals[index], Vector3.up);
                normalsNormal = Vector3.Cross(collisionMesh.normals[index], xzProjectedNormal);

                if (Vector3.SignedAngle(collisionMesh.normals[index], xzProjectedNormal, normalsNormal) > 20 && Vector3.SignedAngle(collisionMesh.normals[index], xzProjectedNormal, normalsNormal) < 179)
                {
                    //store vert index and
                    grabbableVertexIndices.Add(index);
                    Debug.DrawRay(other.transform.position + collisionMesh.vertices[index], collisionMesh.normals[index] * 2, Color.green, 10f);
                }
            }
        }

        //Return zero if there's no grabbable verts
        if (grabbableVertexIndices.Count == 0)
            return closestVertex;


        closest = 0;
        for (int j = 1; j < grabbableVertexIndices.Count - 1; j++)
        {
            if (Vector3.Distance(transform.position, other.transform.position + collisionMesh.vertices[grabbableVertexIndices[closest]]) > Vector3.Distance(transform.position, other.transform.position + collisionMesh.vertices[grabbableVertexIndices[j]]))
            {
                closest = j;
            }
        }


        Debug.DrawRay(other.transform.position + collisionMesh.vertices[grabbableVertexIndices[closest]], collisionMesh.normals[grabbableVertexIndices[closest]] * 2, Color.blue, 10f);

        ledge_point = other.transform.position + collisionMesh.vertices[grabbableVertexIndices[closest]];
        normal_point = collisionMesh.normals[grabbableVertexIndices[closest]];

        //lastCollisionMesh = collisionMesh;
        //lastGrabbableVertexIndices = grabbableVertexIndices;
        //lastClosest = closest;
        //collisionTransform = other.transform;
        //return world position of closest vertex
        return other.transform.position + collisionMesh.vertices[grabbableVertexIndices[closest]];
    }

    public Vector3 GetLedgeVertexPosition()
    {
        return lastCollisionMesh.vertices[lastGrabbableVertexIndices[lastClosest]] + collisionTransform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Don't collide with self
        if (other.transform.name == "MegamanTrigger" || other.transform.root.name == "MegamanTrigger")
            return;
        if (other.gameObject.layer == 9 || other.gameObject.layer == 11)
            return;
        //clear the stores vert index list 
        grabbableVertexIndices.Clear();

        HitCollider = other;

        //Get the closest vertex in world space
        //closestVertex = GetClosestVertex(other);
        //DoLedgeCheck();

        //Don't do anything if there's no grabbable vertices
        if (ledge_point == Vector3.zero)
            canLedgeGrab = false;
        else
            canLedgeGrab = true;

       
    }
}
