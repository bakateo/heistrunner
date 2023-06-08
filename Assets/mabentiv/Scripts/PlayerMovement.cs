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

    [SerializeField] private Transform orientation;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;

    [Header("UI Logging")]
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private float moveSpeed;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    [SerializeField] MovementState state;
    private bool sliding;
    
    private bool grounded;
    
    public bool wallrunning;

    public bool freeze;
    public bool unlimited;
    public bool restricted;

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
        moveSpeedText.text = desiredMoveSpeed.ToString();
        HandleDrag();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }


    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //Jumping
        if (Input.GetKey(jumpKey) && readyToJump && !CheckCeiling())
        {
            readyToJump = false;
            ResetCrouch();
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

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private void StateHandler()
    {
        if (freeze && !grounded)
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

    //TO-DO: change to Mathf.SmoothDamp()

    private void MovePlayer()
    {
        if (restricted) return;

        if (sliding)
            SlidingMovement();

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;


        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        else if
            (grounded) rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        if(!wallrunning) rb.useGravity = !OnSlope();

    }

    private void CheckGround()
    {
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, isGround);
    }

    private void HandleDrag()
    {
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void SpeedControl()
    {

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

    private void Jump()
    {
        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        exitingSlope = false;
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
    }
}
