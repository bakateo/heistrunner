using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSliding : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Transform orientation;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float slideTimer;

    [SerializeField] private float slideYScale;
    private float startYScale;

    [Header("Input")]
    [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        PlayerInput();
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
            SlidingMovement();
    }


    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0)) 
            StartSlide();
        if (Input.GetKeyUp(slideKey) && pm.sliding)
            StopSlide();
    }

    private void StartSlide()
    {
        pm.sliding = true;
        transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if(!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;

        } else {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
            StopSlide();
    }


    private void StopSlide()
    {
        pm.sliding = false;

        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);


    }
}
