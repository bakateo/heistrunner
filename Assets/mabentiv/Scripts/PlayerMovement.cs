using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float groundDrag;
    [SerializeField] private float wallrunSpeed;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    [Header("Camera")]
    [SerializeField] private Transform orientation;
    [SerializeField] PlayerCamera cam;
    [SerializeField] ParticleSystem speedLines;

    [Header("Sliding")]
    [SerializeField] private float slidingSpeed;
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    [SerializeField] private float slideYScale;
    private float slideTimer;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float coyoteTime;
    private float coyoteTimeCounter;
    private bool readyToJump;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] private LayerMask isGround;

    [Header("Crouching")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private bool underCeiling;
    private bool stillCrouching;
    private RaycastHit ceilingHit;
    private float startYScale;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode slideKey = KeyCode.F;

    [Header("UI Logging")]
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private float moveSpeed;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    [SerializeField] MovementState state;


    private bool sliding;
    public bool grounded;
    public bool wallrunning;
    public bool freeze;
    public bool unlimited;
    public bool restricted;
    public bool activeGrapple;

    private enum MovementState
    //states in klassen umändern
    {
        freeze,
        unlimited,
        walking,
        sprinting,
        wallrunning,
        crouching,
        sliding,
        air
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        grounded = false;
        wallrunning = false;
        stillCrouching = false;
        underCeiling = false;
        startYScale = transform.localScale.y;

    }

    private void Update()
    {
        CheckGround();
        PlayerInput();
        if(stillCrouching)
            CheckCeiling();
        StateHandler();
        SpeedControl();
        HandleDrag();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }


    private void PlayerInput()
    {

        if (!PlayerRespawn.playerDied)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            //Jumping
            if (Input.GetKey(jumpKey) && readyToJump && !CheckCeiling() && coyoteTimeCounter > 0f)
            {
                ResetCrouch();
                coyoteTimeCounter = 0f;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }

            //Crouching
            if (Input.GetKeyDown(crouchKey) && grounded)
                Crouch();
            if (Input.GetKeyUp(crouchKey) && grounded && !CheckCeiling())
                ResetCrouch();

            //Sliding
            if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && !stillCrouching)
                StartSlide();
            if (Input.GetKeyUp(slideKey) && sliding)
                StopSlide();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private void StateHandler()
    {
        if (freeze)
        {
            state = MovementState.freeze;
            rb.velocity = Vector3.zero;

        } else if (unlimited)
        {
            state = MovementState.unlimited;
            moveSpeed = 999f;
            return;
        }

        else if (wallrunning && !grounded)
        {
            state = MovementState.wallrunning;
            desiredMoveSpeed = wallrunSpeed;
        }

        else if (sliding && !stillCrouching && grounded)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                desiredMoveSpeed = slidingSpeed;
            else
                desiredMoveSpeed = sprintSpeed;
        }

        else if (Input.GetKey(crouchKey) && grounded || stillCrouching)
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        else if (Input.GetKey(sprintKey) && grounded && !stillCrouching)
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }

        else if (grounded && !stillCrouching)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;

        } else if(!grounded && !stillCrouching){

            state = MovementState.air;
        }

        moveSpeed = desiredMoveSpeed;

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        if (restricted) return;

        if (activeGrapple) return;
     
        if (sliding)
            SlidingMovement();

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;


        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        else if (grounded) 
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        if(!wallrunning) rb.useGravity = !OnSlope();

    }

    private void CheckGround()
    {
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, isGround);
        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
        } else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleDrag()
    {
        if (grounded && !activeGrapple)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void SpeedControl()
    {
        if (activeGrapple) return;

        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

        }

    }

    public void Jump()
    {
        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        readyToJump = false;
    }

    private void ResetJump()
    {
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        exitingSlope = false;
        readyToJump = true;
    }

    private bool enableMovementOnNextTouch;

    public void JumpToPosition(Vector3 targetPostition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPostition + new Vector3(0f, 2f, 0f), trajectoryHeight);

        Invoke(nameof(SetVelocity), 0.1f);
    }

    private Vector3 velocityToSet;

    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;

        rb.velocity = velocityToSet;
    }

    public void ResetRestriction()
    {
        activeGrapple = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestriction();

            GetComponent<PlayerGrappling>().StopGrapple();
        }
    }

    private bool CheckCeiling()
    {
        if (Physics.Raycast(transform.position + new Vector3(0f, 0.5f, 0f), Vector3.up, out ceilingHit, 0.6f))
        {
            Debug.Log("ceiling hit");
            return true;
        }
        else
        {
            Debug.Log("no ceiling hit");
            if (!Input.GetKey(crouchKey))
                ResetCrouch();
            return false;
        }
    }

    private void Crouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        stillCrouching = true;
    }

    private void ResetCrouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        stillCrouching = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 0.1f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void StartSlide()
    {
        sliding = true;
        transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        cam.DoFov(80f);
        speedLines.Play();

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if (!OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;

        } else {
            rb.AddForce(GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    {
        sliding = false;
        stillCrouching = true;
        CheckCeiling();

        cam.DoFov(75f);
        speedLines.Stop();

    }

    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }
}
