using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityStandardAssets.Cameras;
using UnityStandardAssets.Utility;
using UnityEngine.Animations.Rigging;
using System;


public class Player : MonoBehaviour
{
    

    #region STATIC PROPERTIES
    private float handAboveHeadHeight = 0.3f;
    private float megamanHeight = 1.63f;

    private const float m_StationaryTurnSpeed = 5;
    private const float m_MovingTurnSpeed = 10;
    private int m_JumpCount = 0;
    private int m_DashCount = 0;  
    #endregion

    #region CONTROLLABLE VARIABLE PROPERTIES
    [Header("Controllable Properties")]
    [SerializeField, Range(0f, 100f)]
    public float m_MoveMultiplier = 6f;
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxGroundSnapSpeed = 100f;
    [SerializeField, Range(0f, 100f)]
    float snapDistance = 1.5f;
    [SerializeField, Range(0f, 100f)]
    public float m_GroundCheckDistance = 0.1f;
    [SerializeField]
    LayerMask snapMask = -1; //configure player snap-probe to probe all layers specified in the editor. We're mostly trying to disclude props/damage objects/etc that dont make sense to snap to

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 100f)]
    public float FallRateModifier = 1.5f;

    [SerializeField, Range(0f, 100f)] //Note: Running upside down is cool. Other Note: This is only here in case I want to switch to a hard angle limit on running. Atm I think some kind of stamina system is better
    float maxGroundAngle = 45f;


    [SerializeField, Range(0f,100f)]
    public float wallRunMaxDistance = 10f;
    [SerializeField, Range(0f, 100f)]
    public float wallRunMaxHeight = 6f;
    [SerializeField, Range(0f, 100f)]
    public float wallRunSlowSpeed = 2f;
    [SerializeField, Range(0f, 100f)]
    public float wallRunForce;

    [SerializeField, Range(0f, 100f)]
    public float m_DashMultiplier = 5f;


    [SerializeField, Range(0f, 20f)]
    public float m_jumpHeight = 0.5f;
    [SerializeField, Range(0, 20)]
    public int m_MaxJumpCount = 3;

    [SerializeField, Range(0f, 100f)]
    public float maxLockOnDistance = 70f;

    #endregion

    #region NON-CONTROL VARIABLE PROPERTIES
    [Header("Visual Debug Values (READ ONLY AT EDITOR LEVEL)")]
    public bool m_IsNearWall;
    public bool m_IsGrounded;
    public bool m_IsWallRunning;
    public bool m_IsInAir = false;
    public bool m_canLedgeGrab;
    public string movementType = "grounded";
    [SerializeField] private int lockedIndex = -1;
    public List<Transform> lockableEnemies;
    public Transform lockedEnemy;
    public bool lockedOn;
    public float lastLockTime;

    private float maxSpeedChange;
    private int stepsSinceLastGrounded = 0;
    private int stepsSinceLastJump = 0;

    private Vector3 desiredVelocity;
    private Vector3 velocity;

    private int busterShotIndex = -1;
    [SerializeField]private Vector3 m_GroundNormal;

    private int wallSide;
    private bool isVerticalWallRun;
    private float onWallRotation;
    private float turnSmoothVelocity;
    private Vector3 wallBitangent;
    private Vector3 m_CamForward;
    private Vector3 m_CamRight;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 Last_moveDirection;
    private Vector3 newMoveDirection;
    private Quaternion tiltTarget;
    private Vector3 tiltDir;
    #endregion

    #region COMPONENT REFS
    private PlayerInput playerInput;
    private Rigidbody m_Rigidbody;
    private Animator m_Animator;
    private Transform m_Armature;
    private Transform grabbedTransform;
    public List<Transform> Buster_Shots;
    public LedgeChecker m_LedgeChecker;
    private Vector3 m_closestVertex;
    private AudioSource audioSource;
    [Header("Necessary Component Refs")]
    [SerializeField] public Camera Camera;
    [SerializeField] private List<MultiAimConstraint> lookAtBones;
    [SerializeField] public Transform pointOfInterest;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private List<AudioClip> SFX;
    [SerializeField] public List<Enemy> enemies; //This should be set dynamically if we have more than 1 enemy. Can get enemies[i].subMeshes to attack 
    #endregion
    
    #region EVENTS
    public delegate void LockOnEvent(object sender, LockOnEventArgs e);
    public event LockOnEvent LockOnChanged;
    public LockOnEventArgs eventArgs;

    protected virtual void OnLockOnChanged(LockOnEventArgs e)
    {
        e.lockedOn = lockedOn;
        e.lockedEnemy = lockedEnemy;
        LockOnChanged?.Invoke(this, e);

    }
    #endregion

    #region CHARACTER LOGIC

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInput>();
        Camera = Camera.main;
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();
        m_Armature = transform.Find("Megaman_Armature");
        eventArgs = new LockOnEventArgs();
        //m_LedgeChecker = GetComponentInChildren<LedgeChecker>();  
    }

    /// <summary>
    /// TODO:
    /// 1. Validate order of execution here later
    /// </summary>
    private void FixedUpdate()
    {
        //Update player state
        DebugGroundCheck();
        UpdateState();

        ValidateLockOnDistance();

        //if (m_Rigidbody.velocity.y < 0)
        //    DoLedgeGrab();

        m_CamForward = Camera.transform.forward;
        m_CamRight = Camera.transform.right;

        switch (movementType)
        {
            case "grounded":
                OnMovement();
                break;
            case "ledge":
                LedgeGrabMovement();
                break;
        }

    }

    private void Update()
    {
        m_CamForward = Camera.transform.forward;
        m_CamRight = Camera.transform.right;
    }

    //Music triggers live here
    private void OnCollisionEnter(Collision collision)
    {
        
        //Move's body toward velocity when colliding       
        velocity.x = (Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange * 10f));
        velocity.z = (Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange * 10f));

        if (collision.collider.gameObject.name == "Bossroom_Entrance" && musicSource.clip != musicSource.GetComponent<AudioManager>().musicList[1])
        {
            musicSource.clip = musicSource.GetComponent<AudioManager>().musicList[1];
            musicSource.Play(); 
        }
        if(collision.collider.gameObject.name == "Altar_Base" && musicSource.clip != musicSource.GetComponent<AudioManager>().musicList[2])
        {
            musicSource.clip = musicSource.GetComponent<AudioManager>().musicList[2];
            musicSource.Play();
            enemies[0].GetComponent<Enemy>().hasTarget = true;
            enemies[0].GetComponent<Enemy>().ControlBones[2].GetComponent<DampedTransform>().data.dampRotation = 0.9f;
        }
    }

    private void DebugGroundCheck()
    {

#if UNITY_EDITOR
        //visualize ground check ray
        //down
        Vector3 groundCheckOffset = Vector3.up * 0.5f;
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset * m_GroundCheckDistance));
                                         
        //Cardinal Directions            
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + (transform.forward * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + (-transform.forward * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + (transform.right * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + (-transform.right * m_GroundCheckDistance));
                                         
        //45* Directions              
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + ((transform.forward - transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + ((transform.forward + transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + ((transform.forward + transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + ((-transform.forward + transform.right).normalized * m_GroundCheckDistance));
        Debug.DrawLine(transform.position + groundCheckOffset, transform.position + (groundCheckOffset) + ((-transform.forward - transform.right).normalized * m_GroundCheckDistance));

        //Surveyor wheel for running debug                                                                         y-coordinate                                    z-coordinate
        Vector3 verticalOffset = Vector3.Lerp(transform.up * 0.5f, transform.up, m_Rigidbody.velocity.magnitude / maxSpeed);
        Vector3 forwardOffset = Vector3.Lerp(transform.forward * 0.5f, transform.forward, m_Rigidbody.velocity.magnitude / maxSpeed);
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (verticalOffset * Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (forwardOffset * -Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //up
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (-verticalOffset * Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (forwardOffset * Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //down
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (forwardOffset * Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (verticalOffset * Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //foward
        Debug.DrawLine(transform.position + verticalOffset, transform.position + transform.up + (forwardOffset * -Mathf.Sin(Time.time * m_Rigidbody.velocity.magnitude)) + (verticalOffset * -Mathf.Cos(Time.time * m_Rigidbody.velocity.magnitude))); //

#endif
    }

    /// <summary>
    /// TODO:
    /// 1. Average the last ground normal with current ground normal to avoid absurd rotation changes. Could also lerp normal from last to current over x time/x translation delta
    /// </summary>
    private void UpdateState()
    {

        //Counting physics steps since last grounded and last jump
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;


        RaycastHit hitInfo;

        Vector3 raycastOrigin = transform.position + (Vector3.up * 0.5f);

        m_IsNearWall = false; //Set isNearWall to false so it can be set to true if it is true. Otherwise it should default to false

        if (Physics.Raycast(raycastOrigin, transform.forward, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, -transform.forward, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, transform.right, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, -transform.right, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, (transform.forward + transform.right).normalized, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, (transform.forward - transform.right).normalized, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, (-transform.forward + transform.right).normalized, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        if (Physics.Raycast(raycastOrigin, (-transform.forward - transform.right).normalized, out hitInfo, m_GroundCheckDistance))
        {
            m_IsNearWall = true;
        }
        m_GroundNormal = hitInfo.normal;

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

    /* TO DO: 
     * When we get hit while ledgegrabbing we have to handle the physics of that ourselves
     */
    private void DoLedgeGrab()
    {
        if (m_LedgeChecker.ledge_point != Vector3.zero)
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.isKinematic = true;
            m_Animator.SetBool("isFalling", false);
            m_Animator.SetBool("isJumping", false);
            m_Animator.SetTrigger("canLedgeGrab");
            movementType = "ledge";
            transform.position = new Vector3(m_LedgeChecker.closestVertex.x, m_LedgeChecker.closestVertex.y - megamanHeight, m_LedgeChecker.closestVertex.z);
            m_LedgeChecker.closestVertex = Vector3.zero;
            
        }
    }
    /* TO DO:
     * Rotate character's body(probably the pelvis) within some range
     */
    private void LedgeGrabMovement()
    {
        if (m_LedgeChecker.canLedgeGrab)
            transform.Translate(m_LedgeChecker.GetLedgeVertexPosition() - new Vector3(m_Armature.transform.position.x, m_LedgeChecker.GetLedgeVertexPosition().y, m_Armature.transform.position.z), Space.World);

        m_Animator.SetBool("canGetUp", false);

        //Raw axis input
        moveDirection = new Vector3(Gamepad.current.leftStick.ReadValue().x, 0f, Gamepad.current.leftStick.ReadValue().y);

        //Add in camera orientation
        Vector3 m_CamForward_test = Vector3.Scale(m_CamForward, new Vector3(1, 0, 1)).normalized;
        moveDirection = moveDirection.x * m_CamRight + moveDirection.z * m_CamForward_test;

        moveDirection = Vector3.ProjectOnPlane(moveDirection, m_GroundNormal);

        //Rotate the character's body 
        //DoRotation();

        //Multiply by moveMultiplier 
        moveDirection = new Vector3(moveDirection.x * m_MoveMultiplier, 0f, moveDirection.z * m_MoveMultiplier);

        //When trying to move backwards, switch to falling anim
        if (Vector3.Angle(moveDirection, transform.forward) > 50)
        {
            m_Rigidbody.isKinematic = false;
            m_Animator.SetFloat("moveSpeed", 0.01f);
            m_Animator.SetBool("canLedgeGrab", false);
            m_Animator.SetBool("canGetUp", false);
            movementType = "grounded";
        }
        else if (Gamepad.current.buttonEast.IsActuated(0.1f)) //Quick and dirty, move to OnJump later
        {
            m_Animator.SetBool("canGetUp", true);
            StartCoroutine(LedgeGetupClean());
            movementType = "grounded";
        }

        //To make the fall part of the jump faster, affect gravity
        if (m_Rigidbody.velocity.y < -0.1f)
        {
            m_Rigidbody.velocity += Vector3.up * Physics.gravity.y * 1.5f * Time.deltaTime;
        }
        else if (m_Rigidbody.velocity.y > 0.1f && !Gamepad.current.buttonSouth.isPressed)
        {
            m_Rigidbody.velocity += Vector3.up * Physics.gravity.y * 2f * Time.deltaTime;
        }
        
    }

    private IEnumerator LedgeGetupClean()
    {
        yield return new WaitUntil(() => m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Megaman_LedgeGetUp") == true);
        yield return new WaitForSecondsRealtime(0.3f);
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.useGravity = true;
        Vector3 transformPosition = transform.position;
        transform.position = m_Armature.Find("Root").position;
        m_Armature.Find("Root").position = transformPosition;
    }

    /// <summary>
    /// TODO:
    /// 1. Once you understand quaternions properly, fix the m_RigidBody.MoveRotation() line
    /// 1a. The reason shit gets fucked is bc Quaternion.LookRotation * tiltTarget evaluates to Quaternion.identity when
    /// 1b. lookDir is too far away from transform.forward. Too far being maybe >20 degrees
    /// </summary>
    private void DoRotation()
    {
        //convert to local-relative for tiltTarget
        Vector3 moveDirectionLocal = transform.InverseTransformDirection(desiredVelocity);
        Vector3 lookDir;
        float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, Vector3.Magnitude(desiredVelocity));

        //Let player move backwards if moveDirection is pointed backwards
        if (Vector3.Dot(transform.forward, desiredVelocity.normalized) > -0.85f)
        {
            lookDir = Vector3.RotateTowards(transform.forward, desiredVelocity.normalized, turnSpeed * Time.deltaTime, 0.0f);
        }
        else
        {
            lookDir = transform.forward;
        }
        //For regular movemnt
        if (velocity.magnitude > 0.2f)
        {
            tiltDir = new Vector3(8f * moveDirectionLocal.normalized.z, 0f, 8f * -moveDirectionLocal.normalized.x);
            tiltDir.x = Mathf.Clamp(tiltDir.x, 0f, 10f);
            tiltDir.z = Mathf.Clamp(tiltDir.z, -10f, 10f);
            tiltDir.y = transform.rotation.y;
            tiltTarget = Quaternion.Euler(tiltDir);
        }
        //When we've fallen
        else if (velocity.magnitude <= 0.2f && Vector3.Dot(Vector3.up, transform.up) < 0.966f)
        {
            tiltTarget = Quaternion.Euler(-transform.rotation.x * 20f, 0f, -transform.rotation.z * 20f);
        }
        //When we're still, don't add any rotation to the lookdir, which will be close to the current transform.forward if the code enters here
        else
        {
            tiltTarget = Quaternion.Euler(0f,0f,0f);
        }


        Debug.DrawRay(transform.position + Vector3.up, moveDirection, Color.green);
        Debug.DrawRay(transform.position + Vector3.up, lookDir, Color.blue);
        Vector3 lookDirAdusted = Quaternion.LookRotation(lookDir, transform.up).eulerAngles + tiltDir;

        //m_Rigidbody.MoveRotation(Quaternion.LookRotation(lookDir, transform.up) * tiltTarget);
        m_Rigidbody.MoveRotation(Quaternion.LookRotation(lookDir, transform.up));
    }

    #endregion

    #region INPUT ACTION EVENTS
    public void OnJump()
    {
        if ((m_IsGrounded || m_IsNearWall) & m_JumpCount <= m_MaxJumpCount)
        {
            stepsSinceLastJump = 0;
            m_JumpCount += 1;
            //Derivation of desired jumpspeed as a fx of jump height can be found on catlikecoding.com/unity/tutorials/movement/physics
            float jumpSpeed = Mathf.Sqrt(-Physics.gravity.y * m_jumpHeight / 2);

            float alignedSpeed = Vector3.Dot(velocity, m_GroundNormal);

            if (alignedSpeed > 0f)
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

            m_Animator.SetBool("isJumping", true);

            StartCoroutine(PlayJumpSounds());
           
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

    private IEnumerator PlayJumpSounds()
    {
        audioSource.PlayOneShot(SFX[1], 0.5f);
        yield return new WaitUntil(() => m_JumpCount == 0);
        audioSource.PlayOneShot(SFX[2], 0.5f);
    }
    /* TO DO:
     * 1. Enemy is hardcoded into this method. If we do more enemies ever, we'll need a systematic way to do this
     */

    public void OnWallRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            RaycastHit hitInfo;

            Vector3 raycastOrigin = transform.position + (Vector3.up * m_GroundCheckDistance);
            //Check if there's a wall near us
            if (Physics.Raycast(raycastOrigin, transform.forward, out hitInfo, 9 * m_GroundCheckDistance))
            {
                newMoveDirection = Vector3.Cross(hitInfo.normal, transform.up);
            }
            else if (Physics.Raycast(raycastOrigin, transform.right, out hitInfo, 9 * m_GroundCheckDistance))
            {
                newMoveDirection = Vector3.Cross(hitInfo.normal, transform.up);
            }
            else if (Physics.Raycast(raycastOrigin, -transform.right, out hitInfo, 9 * m_GroundCheckDistance))
            {
                newMoveDirection = Vector3.Cross(hitInfo.normal, transform.up);
            }
            if (hitInfo.point != Vector3.zero && Vector3.Dot(Vector3.up, hitInfo.normal) < 0.5f)
            {
                moveDirection = newMoveDirection;
                m_IsWallRunning = true;
            }
            else
            {
                m_IsWallRunning = false;
                moveDirection = Last_moveDirection;
            }
            Vector3 rayDir = hitInfo.normal;
            wallBitangent = Vector3.Cross(transform.forward, rayDir);

            float angle = Vector3.Angle(rayDir, transform.forward);
            float yRot = angle - 90;
            if (angle > 145)
            {
                isVerticalWallRun = true;
                wallSide = 0;
                onWallRotation = Quaternion.LookRotation(-hitInfo.normal).eulerAngles.y;
            }
            else
            {
                isVerticalWallRun = false;
                wallSide = 1; //Left
                if (wallBitangent.y < 0)
                {
                    moveDirection.x *= -1f;
                    moveDirection.z *= -1f;
                    yRot *= -1;
                    wallSide = -1; //Right
                }
                Debug.DrawRay(transform.position + Vector3.up, wallSide * moveDirection.magnitude * moveDirection, Color.green, 2f);
                onWallRotation = transform.localEulerAngles.y + yRot;
            }

            wallBitangent = hitInfo.point;

        }
        else if (context.performed)
        {
            m_IsWallRunning = false;
        }
    
    }
    private void CalculateWallRunForce()
    {
        wallRunForce = Mathf.Sqrt(-2 * m_Rigidbody.velocity.y * wallRunMaxHeight);
    }

    private void DoWallRunRotation()
    {
        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, onWallRotation, ref turnSmoothVelocity, 3);
    }

    public void OnLock(InputAction.CallbackContext context)
    {
        List<Transform> temps = new List<Transform>();
        foreach(Transform t in lockableEnemies)
        {
            if (t == null)
            {
                temps.Add(t);
            }
        }
        foreach(Transform t in temps)
        {
            lockableEnemies.Remove(t);
        }

        if (context.performed)
        {
            if (Time.time - lastLockTime < 0.3f)
            {
                GetComponent<Megaman_Rig>().ToggleMove(null, "AimBuster");
                lockedEnemy = null;
                lockedIndex = -1;
                lockedOn = false;
                eventArgs.lockedEnemy = lockedEnemy;
                eventArgs.lockedOn = lockedOn;
                LockOnChanged(this, eventArgs);

            }
            else if (lockableEnemies.Count > 0)
            {
                var newEnemy = (lockableEnemies.Count == lockedIndex + 1) ? lockableEnemies[lockedIndex] : lockableEnemies[lockedIndex + 1];
                Vector3 enemyDir;
                float distance;
                enemyDir = newEnemy.transform.position - transform.position;
                distance = enemyDir.magnitude;

                if (Vector3.Angle(transform.forward, enemyDir) < 120 && distance < maxLockOnDistance)
                {
                    if (lockedIndex + 1 == lockableEnemies.Count)
                        lockedIndex = -1;

                    GetComponent<Megaman_Rig>().ToggleMove(lockableEnemies[lockedIndex + 1], "AimBuster");
                    lockedEnemy = lockableEnemies[lockedIndex + 1];
                    lockedIndex++;
                    lockedOn = true;
                    eventArgs.lockedEnemy = lockedEnemy;
                    eventArgs.lockedOn = lockedOn;
                    LockOnChanged(this, eventArgs);
                }
                else
                {
                    lockedOn = false;
                    lockedEnemy = null;
                    eventArgs.lockedEnemy = lockedEnemy;
                    eventArgs.lockedOn = lockedOn;
                    LockOnChanged(this, eventArgs);
                }

            }
            else
            {
                GetComponent<Megaman_Rig>().ToggleMove(null, "AimBuster");
                lockedEnemy = null;
                lockedIndex = -1;
                lockedOn = false;
                eventArgs.lockedEnemy = lockedEnemy;
                eventArgs.lockedOn = lockedOn;
                LockOnChanged(this, eventArgs);
            }

            lastLockTime = Time.time;
        }


        //Animation Rigging MultiAim logic
        foreach (MultiAimConstraint lookAt in lookAtBones)
        {
            if (lockedOn)
            {
                lookAt.data.sourceObjects.Clear();
                WeightedTransform newTarget = new WeightedTransform(lockedEnemy, 1f);
                lookAt.data.sourceObjects.Add(newTarget);
            }
            else
            {
                lookAt.data.sourceObjects.Clear();
                WeightedTransform newTarget = new WeightedTransform(pointOfInterest, 1f);
                lookAt.data.sourceObjects.Add(newTarget);
            }
        }
    }
  
    // If locked enemy is too far, will auto unlock
    private void ValidateLockOnDistance()
    {
        
        if (lockedOn)
        {
            try
            {
                if (Vector3.Distance(transform.position, lockedEnemy.position) > maxLockOnDistance)
                {
                    GetComponent<Megaman_Rig>().ToggleMove(null, "AimBuster");
                    lockedEnemy = null;
                    lockedIndex = -1;
                    lockedOn = false;
                    eventArgs.lockedEnemy = lockedEnemy;
                    eventArgs.lockedOn = lockedOn;
                    LockOnChanged(this, eventArgs);
                }
            }
            catch (MissingReferenceException)
            {
                GetComponent<Megaman_Rig>().ToggleMove(null, "AimBuster");
                lockedEnemy = null;
                lockedIndex = -1;
                lockedOn = false;
                eventArgs.lockedEnemy = lockedEnemy;
                eventArgs.lockedOn = lockedOn;
                LockOnChanged(this, eventArgs);
            }
        }
    }
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            StopCoroutine(ContinueShooting());
            StartCoroutine(ContinueShooting());
        }
    }
    private IEnumerator ContinueShooting()
    {

        yield return new WaitForSecondsRealtime(0.1f);

        while (Gamepad.current.buttonWest.IsActuated())
        {
            if (busterShotIndex + 1 >= Buster_Shots.Count)
            {
                busterShotIndex = -1;
            }
            var newBuster = Instantiate(Buster_Shots[0], Buster_Shots[0].transform.position, Buster_Shots[0].transform.rotation, Buster_Shots[0].transform.parent);
            newBuster.GetComponent<BusterShot>().ShootBuster();
            busterShotIndex++;
            audioSource.PlayOneShot(SFX[7], 0.5f);

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (m_DashCount == 0 && m_Rigidbody.velocity.magnitude < 10f)
            {
                //var dashDirection = new Vector3(moveDirection.normalized.x * m_DashConstant,0, moveDirection.normalized.z * m_DashConstant);
                //m_Rigidbody.AddForce(dashDirection, ForceMode.Impulse);
                StopCoroutine(Dash());
                StartCoroutine(Dash());
                m_DashCount += 1;
                audioSource.PlayOneShot(SFX[1], 0.5f);
            }
        }
    }
    public IEnumerator Dash()
    {
        //var dashDirection = new Vector3(transform.forward.x, 0.1f, transform.forward.z) * m_DashConstant;
        var dashDirection = new Vector3(moveDirection.normalized.x, moveDirection.normalized.y * 0.5f, moveDirection.normalized.z) * m_DashMultiplier;
        Vector3 newPos = transform.position + dashDirection;
        while (Vector3.Magnitude(transform.position - newPos) > 2f)
        {
            //transform.Translate(dashDirection * 10 * Time.deltaTime, Space.World);
            RaycastHit hit;
            Debug.DrawRay(transform.position + Vector3.up , dashDirection.normalized * 1f, Color.grey, 1f);
            if (Physics.Raycast(transform.position + Vector3.up , dashDirection.normalized, out hit, dashDirection.normalized.magnitude * 1f)){
                break;
            }
            m_Rigidbody.MovePosition(transform.position + 0.3f * dashDirection);
            yield return null;
        }
        yield return null;
    }
    public void OnMovement()
    {
        //Control loop speed for run as a function of movedirection.magnitude
        if ((!m_IsGrounded && m_IsInAir && m_Rigidbody.velocity.y < -0.1f) && !m_IsWallRunning)
        {
            m_Animator.SetBool("isJumping", false);
            m_Animator.SetBool("isFalling", true);
        }
        if ((m_IsGrounded || m_IsNearWall) && !m_IsWallRunning)
        {
            desiredVelocity = new Vector3(Gamepad.current.leftStick.ReadValue().x, 0f, Gamepad.current.leftStick.ReadValue().y) * maxSpeed;

            //Add in camera orientation
            Vector3 m_CamForward_test = Vector3.Scale(m_CamForward, new Vector3(1, 0, 1)).normalized;
            desiredVelocity = desiredVelocity.x * m_CamRight + desiredVelocity.z * m_CamForward_test;

            velocity = m_Rigidbody.velocity;

            AdjustVelocity(); //Changes air acceleration based on m_IsGrounded and adjusts velocity based on ground slope

            //SnapToGround(); //returns a bool but really snaps player to ground to prevent sliding ridiculously off the edge of ramps

            //To make the fall part of the jump faster, affect gravity
            if (m_Rigidbody.velocity.y < 0f)
            {
                velocity += Vector3.up * Physics.gravity.y * FallRateModifier * Time.deltaTime;
            }

            //Do movement and rotation
            m_Rigidbody.velocity = velocity;
            m_Animator.SetFloat("movespeed", m_Rigidbody.velocity.magnitude);

            DoRotation();

        }
        if (m_IsWallRunning)
        {
            m_Animator.SetBool("isFalling", false);
            m_Animator.SetBool("isJumping", false);

            CalculateWallRunForce();
            DoWallRunRotation();

            desiredVelocity = new Vector3(Gamepad.current.leftStick.ReadValue().x, 0f, Gamepad.current.leftStick.ReadValue().y) * maxSpeed;
            //Add in camera orientation
            Vector3 m_CamForward_test = Vector3.Scale(m_CamForward, new Vector3(1, 0, 1)).normalized;
            desiredVelocity = desiredVelocity.x * m_CamRight + desiredVelocity.z * m_CamForward_test;
            desiredVelocity = desiredVelocity * maxSpeed;


            velocity = m_Rigidbody.velocity;

            if (isVerticalWallRun)
            {
                transform.forward = transform.up;
                //transform.up = -transform.forward;
                desiredVelocity.y = 0;
            }
            if (m_IsGrounded || m_IsNearWall)
            {
                desiredVelocity.y = wallRunForce;
            }
            float wallRunMultiplier = m_MoveMultiplier * 1.5f;
            Vector3 wallRunDirection = new Vector3(desiredVelocity.x * wallRunMultiplier, desiredVelocity.normalized.y, desiredVelocity.z * wallRunMultiplier);
            m_Animator.SetFloat("movespeed", desiredVelocity.magnitude * 0.05f);
            m_Rigidbody.MovePosition(transform.position + wallRunDirection * Time.deltaTime);
        }
    }

    /*TO DO:
     * 1. Move DoRotation() from here to Update(). It'll be smooth in update.
     */
    //We can't use OnMovement() event because camera transform isn't updated constantly
    public void OldMovementCode()
    {
        //Control loop speed for run as a function of movedirection.magnitude
        if ((!m_IsGrounded && m_IsInAir && m_Rigidbody.velocity.y < -0.1f) && !m_IsWallRunning)
        {
            m_Animator.SetBool("isJumping", false);
            m_Animator.SetBool("isFalling", true);
        }
        if ((m_IsGrounded || m_IsInAir) && !m_IsWallRunning)
        {
            if (Vector3.Angle(Vector3.up, transform.up) > 10) transform.up = Vector3.up;

            //Raw axis input
            moveDirection = new Vector3(Gamepad.current.leftStick.ReadValue().x, 0f, Gamepad.current.leftStick.ReadValue().y);

            //Add in camera orientation
            Vector3 m_CamForward_test = Vector3.Scale(m_CamForward, new Vector3(1, 0, 1)).normalized;
            moveDirection = moveDirection.x * m_CamRight + moveDirection.z * m_CamForward_test;

            //moveDirection = Vector3.ProjectOnPlane(moveDirection, m_GroundNormal);

            if (m_IsGrounded && m_Rigidbody.velocity.y < 0.01f && m_Rigidbody.velocity.y > -0.01f)
            {
                m_Animator.SetBool("isFalling", false);
                m_Animator.SetBool("isJumping", false);
            }

            //Multiply by moveMultiplier 
            moveDirection = new Vector3(moveDirection.x * m_MoveMultiplier, 0f, moveDirection.z * m_MoveMultiplier);
            Last_moveDirection = moveDirection;


            m_Animator.SetFloat("moveSpeed", moveDirection.magnitude * 0.2f);

            //Move the character
            m_Rigidbody.MovePosition(transform.position + moveDirection * Time.deltaTime);
            DoRotation();


            //To make the fall part of the jump faster, affect gravity
            if (m_Rigidbody.velocity.y < -0.1f)
            {
                m_Rigidbody.velocity += Vector3.up * Physics.gravity.y * 1.5f * Time.deltaTime;
            }
            else if (m_Rigidbody.velocity.y > 0.1f && !Gamepad.current.buttonSouth.isPressed)
            {
                m_Rigidbody.velocity += Vector3.up * Physics.gravity.y * 2f * Time.deltaTime;
            }

            //Release player from ledge grab
            if (m_Rigidbody.isKinematic && moveDirection.magnitude > 0.1)
            {
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.useGravity = true;
            }
            
        }
        if (m_IsWallRunning)
        {
            m_Animator.SetBool("isFalling", false);
            m_Animator.SetBool("isJumping", false);
            CalculateWallRunForce();
            DoWallRunRotation();
            if (isVerticalWallRun)
            {
                transform.forward = transform.up;
                //transform.up = -transform.forward;
                moveDirection.y = 0;
            }
            if (m_IsGrounded || m_IsNearWall)
            {
                moveDirection.y = wallRunForce;
            }
            float wallRunMultiplier = m_MoveMultiplier * 1.5f;
            Vector3 wallRunDirection = new Vector3(moveDirection.x * wallRunMultiplier, moveDirection.normalized.y, moveDirection.z * wallRunMultiplier);
            m_Animator.SetFloat("movespeed", moveDirection.magnitude * 0.05f);
            m_Rigidbody.MovePosition(transform.position + wallRunDirection * Time.deltaTime);
        }
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
        Debug.DrawLine(transform.position + Vector3.up * 0.5f, transform.position - (Vector3.up * snapDistance), Color.red);
        if (!Physics.Raycast(m_Rigidbody.position, -transform.up, out RaycastHit hit, snapDistance, snapMask))
        {
            return false;
        }
        if (hit.normal.y < Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad))
        {
            return false;
        }

        if (hit.normal != null)
        {
            m_GroundNormal = hit.normal;
        }
        float dot = Vector3.Dot(m_Rigidbody.velocity, hit.normal);
        if (dot > 0.1f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        return true;
    }
    #endregion
}

public class LockOnEventArgs : EventArgs
{
    public bool lockedOn { get; set; }
    public Transform lockedEnemy { get; set; }
}