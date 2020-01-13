using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSphere : MonoBehaviour
{
    #region PROPERTIES
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxGroundSnapSpeed = 100f;
    [SerializeField, Range(0f, 100f)]
    float snapDistance = 1.5f;
    [SerializeField]
    LayerMask snapMask = -1; //Conifgure player snap probe to probe all layers specified in the editor. We're mostly trying to disclude props/damage objects/etc that dont make sense to snap to

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 100f)]
    public float FallRateModifier = 1.5f;

    [SerializeField, Range(0f, 100f)] //Note: Running upside down is cool. Other Note: This is only here in case I want to switch to a hard angle limit on running. Atm I think some kind of stamina system is better
    float maxGroundAngle = 45f;

    [SerializeField, Range(0f, 20f)]
    public float m_jumpHeight = 0.5f;

    private float maxSpeedChange;
    private int stepsSinceLastGrounded = 0;
    private int stepsSinceLastJump = 0;

    private Vector3 desiredVelocity;
    private Vector3 velocity;
    #endregion

    #region FROM PLAYER.CS
    Rigidbody m_Rigidbody;

    public float m_GroundCheckDistance; 
    private Vector3 m_GroundNormal;
    public bool m_IsGrounded = true;
    public bool m_IsInAir = false;
    public bool m_canLedgeGrab = false;
    public bool m_IsNearWall = false;
    public int m_JumpCount = 0;
    public int m_DashCount = 0;
    public float m_JumpConstant = 2f;
    


    private Vector3 m_CamForward;
    private Vector3 m_CamRight;
    public Camera Camera;
    #endregion

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }
    void Start()
    {
        Camera = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Update player state
        DebugGroundCheck();
        UpdateState();

        //Called specifically bc camera orientation is not updated unless movement changes
        OnMovement();

    }
    private void Update()
    {    
        //Updating camera transform axes
        m_CamForward = Camera.transform.forward;
        m_CamRight = Camera.transform.right;

    
    }

    //Snaps player to ground to prevent being launched off the top of ramps
    private bool SnapToGround()
    {
        //Don't snap to ground if we've been not-grounded for 2 or more steps  OR if we've jumped within the last 2 steps
        if (stepsSinceLastGrounded >= 2 || stepsSinceLastJump <= 6)
        {
            return false;
        }
        if (m_JumpCount > 0)
        {
            return false;
        }
        float speed = m_Rigidbody.velocity.magnitude;
        if (speed > maxGroundSnapSpeed)
        {
            return false;
        }
        Debug.DrawLine(transform.position, transform.position - (Vector3.up * snapDistance), Color.red);
        if (!Physics.Raycast(m_Rigidbody.position, -transform.up, out RaycastHit hit, snapDistance, snapMask))
        {
            return false;
        }
        if(hit.normal.y < Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad))
        {
            return false;
        }

        m_GroundNormal = hit.normal;
        float dot = Vector3.Dot(m_Rigidbody.velocity, hit.normal);
        if (dot > 0.1f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        return true;
    }
    private void DebugGroundCheck()
    {

#if UNITY_EDITOR
        //visualize ground check ray
        //down
        Vector3 groundCheckOffset = -Vector3.up * 0.5f;
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset * m_GroundCheckDistance));

        //Cardinal Directions
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + (transform.forward *  m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + (-transform.forward * m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset ) + (transform.right * m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + (-transform.right * m_GroundCheckDistance));

        //45* Directions
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + ((transform.forward - transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset ) + ((transform.forward + transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + ((transform.forward + transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + ((-transform.forward + transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position - groundCheckOffset, transform.position - (groundCheckOffset) + ((-transform.forward - transform.right).normalized * m_GroundCheckDistance));

        //Surveyor wheel for running debug                                                                         y-coordinate                                    z-coordinate
        Vector3 verticalOffset = Vector3.Lerp(transform.up * 0.5f, transform.up, m_Rigidbody.velocity.magnitude / maxSpeed);
        Vector3 forwardOffset = Vector3.Lerp(transform.forward * 0.5f, transform.forward, m_Rigidbody.velocity.magnitude / maxSpeed);
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (verticalOffset * Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (forwardOffset * -Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //up
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (-verticalOffset * Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (forwardOffset * Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //down
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (forwardOffset * Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (verticalOffset * Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //foward
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (forwardOffset * -Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (verticalOffset * -Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //

#endif
    }

    /* TO DO
     * 1. Change CheckGroundStatus() to UpdateState and store all state info in there
     * 2. Average the last ground normal with current ground normal to avoid absurd rotation changes. Could also lerp normal from last to current over x time/x translation delta
     */
    private void UpdateState()
    {

        //Counting physics steps since last grounded and last jump
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;


        RaycastHit hitInfo;

        Vector3 raycastOrigin = transform.position + (Vector3.up * 0.5f);

        m_IsNearWall = false; //Set isNearWall to false so it can be set to true if it is true. Otherwise it should default to false

        if (Physics.Raycast(raycastOrigin, transform.forward, out hitInfo,  m_GroundCheckDistance))
        {
            m_GroundNormal = hitInfo.normal;
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, -transform.forward, out hitInfo, m_GroundCheckDistance))
        {
            m_GroundNormal = hitInfo.normal;
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, transform.right, out hitInfo,  m_GroundCheckDistance))
        {
            m_IsNearWall = true;
            m_GroundNormal = hitInfo.normal;

        }
        if (Physics.Raycast(raycastOrigin, -transform.right, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
            m_GroundNormal = hitInfo.normal;

        }
        if (Physics.Raycast(raycastOrigin, (transform.forward + transform.right).normalized, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
            m_GroundNormal = hitInfo.normal;

        }
        if (Physics.Raycast(raycastOrigin, (transform.forward - transform.right).normalized, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
            m_GroundNormal = hitInfo.normal;

        }
        if (Physics.Raycast(raycastOrigin, (-transform.forward + transform.right).normalized, out hitInfo,m_GroundCheckDistance))
        {
            m_IsNearWall = true;
            m_GroundNormal = hitInfo.normal;

        }
        if (Physics.Raycast(raycastOrigin, (-transform.forward - transform.right).normalized, out hitInfo,  m_GroundCheckDistance))
        {
            m_IsNearWall = true;
            m_GroundNormal = hitInfo.normal;

        }
        //Scalars on raycast distance are how far player's legs should be able to reach for a double/triple jump off any rigidbody
        if (Physics.Raycast(raycastOrigin, -transform.up, out hitInfo, m_GroundCheckDistance))
        {
            m_GroundNormal = hitInfo.normal;

            //Only consider grounded when slope is less than max ground angle
            if (m_GroundNormal.y >= Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad))
            {
                m_IsGrounded = true;
                stepsSinceLastGrounded = 0;
            }
            else
            {
                m_IsGrounded = false;
            }
            if (m_Rigidbody.velocity.y < 0.1f && m_Rigidbody.velocity.y > -0.1f)
                m_IsInAir = false;
            m_JumpCount = 0;
            m_DashCount = 0;
            m_canLedgeGrab = false;
        }
        else
        {
            m_IsGrounded = false;
            m_IsInAir = true;
            m_GroundNormal = Vector3.up;
        }


    }

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        //Remove some of the movement in the groundNormal direction by taking the projection of velocity onto ground normal. This is so we don't lose contact too much at higher slope values
        //Why aren't we using Vector3.ProjectOnPlane() here? 
        return vector - m_GroundNormal * Vector3.Dot(vector, m_GroundNormal); 
    }

    //Usees ProjectOnContactPlane to adjust velocity based on ground angle
    private void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(transform.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(transform.forward).normalized;

        float currentXVelocity = Vector3.Dot(velocity, xAxis);
        float currentZVelocity = Vector3.Dot(velocity, zAxis);

        float acceleration = (m_IsGrounded || m_IsNearWall) ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentXVelocity, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZVelocity, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentXVelocity) + zAxis * (newZ - currentZVelocity);
    }

    /// <summary>
    /// Todo:
    /// 1. m_JumpConstant refactored to m_JumpHeight (since we're calculating velocity to reach the constant input)
    /// 2. update jump calculation to this calculation
    /// </summary>
    public void OnJump()
    {
        if (m_IsGrounded || m_IsNearWall)
        {
            stepsSinceLastJump = 0;
            m_JumpCount += 1;
            //Derivation of desired jumpspeed as a fx of jump height can be found on catlikecoding.com/unity/tutorials/movement/physics
            float jumpSpeed = Mathf.Sqrt(-Physics.gravity.y * m_jumpHeight/2);

            float alignedSpeed = Vector3.Dot(velocity, m_GroundNormal);
   
            if(alignedSpeed > 0f)
            {
                //prevents jumps from exceeding max speed
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 1f);
            }

            //If moving slowly, use m_JumpConstant as source of jumpheight
            Vector3 adjustedJumpDirection;

            //When doing grounded jump(incuding walls upto maxGroundAngle), adjust jump towards groundNormal(+Vector3.up upwards jump bias) based on groundNormal slope
            //In the air/on a wall, adjust the jump direction upwards
            if (m_IsGrounded)
            {
                adjustedJumpDirection = Vector3.Lerp(Vector3.up, Vector3.ProjectOnPlane(m_GroundNormal, m_Rigidbody.velocity), 1 - Mathf.Clamp(Vector3.Dot(Vector3.up, m_GroundNormal), 0f, 1f));
            }
            else
            {
                adjustedJumpDirection = Vector3.ProjectOnPlane(transform.up, m_Rigidbody.velocity) + Vector3.up;

            }
            m_Rigidbody.velocity += adjustedJumpDirection * jumpSpeed;
        }
    }

    public void OnMovement()
    {
        desiredVelocity = new Vector3(Gamepad.current.leftStick.ReadValue().x, 0f, Gamepad.current.leftStick.ReadValue().y) * maxSpeed;
        
        //Add in camera orientation
        Vector3 m_CamForward_test = Vector3.Scale(m_CamForward, new Vector3(1, 0, 1)).normalized;
        desiredVelocity = desiredVelocity.x * m_CamRight + desiredVelocity.z * m_CamForward_test;

        velocity = m_Rigidbody.velocity;

        AdjustVelocity(); //Changes air acceleration based on m_IsGrounded and adjusts velocity based on ground slope


        SnapToGround(); //returns a bool but really snaps player to ground to prevent sliding ridiculously off the edge of ramps


        //To make the fall part of the jump faster, affect gravity
        if (m_Rigidbody.velocity.y < 0f)
        {
            velocity += Vector3.up * Physics.gravity.y * FallRateModifier * Time.deltaTime;
        }

        m_Rigidbody.velocity = velocity; 
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Move's body toward velocity when colliding       
        velocity.x = (Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange * 10f));
        velocity.z = (Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange * 10f));
    }
}
