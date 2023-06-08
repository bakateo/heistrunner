using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLedgeGrabbing : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform cam;
    [SerializeField] private Rigidbody rb;

    [Header("Ledge Grabbing")]
    [SerializeField] private float moveToLedgeSpeed;
    [SerializeField] private float maxLedgeGrabDistance;
    
    [SerializeField] private float minTimeOnLedge;
    private float timeOnLedge;

    [SerializeField] public bool holdingLedge;

    [Header("Ledge Jumping")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float ledgeJumpForwardForce;
    [SerializeField] private float ledgeJumpUpwardForce;
    

    [Header("Ledge Detection")]
    [SerializeField] private float ledgeDetectionLength;
    [SerializeField] private float ledgeSphereCastRadius;
    [SerializeField] private LayerMask isLedge;


    private Transform lastLedge;
    private Transform currLedge;

    private RaycastHit ledgeHit;

    [Header("Exiting")]
    [SerializeField] public bool exitingLedge;
    [SerializeField] private float exitLedgeTime;
    private float exitLedgeTimer;

    void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }

    private void SubStateMachine()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        bool anyInputKeyPressed = horizontalInput != 0 || verticalInput != 0;

        if (holdingLedge)
        {
            FreezeRbOnLedge();
            timeOnLedge += Time.deltaTime;

            if (timeOnLedge > minTimeOnLedge && Input.GetKeyDown(jumpKey))
                LedgeJump();
              
        }

        else if (exitingLedge) 
        {
            if (exitLedgeTimer > 0) 
                exitLedgeTimer -= Time.deltaTime;
            else 
                exitingLedge = false;
        }
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, out ledgeHit, ledgeDetectionLength, isLedge);

        if (!ledgeDetected) return;

        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.transform.position);

        if (ledgeHit.transform == lastLedge) return;

        if (distanceToLedge < maxLedgeGrabDistance && !holdingLedge) 
            EnterLedgeHold();

    }

    private void DelayedJumpForce()
    {
        Vector3 forceToAdd = cam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpUpwardForce;
        rb.velocity = Vector3.zero;
        rb.AddForce(forceToAdd, ForceMode.Force);
    }

    private void LedgeJump()
    {
        ExitLedgeHold();
        DelayedJumpForce();
    }

    private void EnterLedgeHold()
    {
        holdingLedge = true;

        pm.unlimited = true;
        pm.restricted = true;

        currLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }
    
    private void FreezeRbOnLedge()
    {
        rb.useGravity = false;
        Vector3 directionToLedge = currLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, currLedge.position);

        if(distanceToLedge > 1f)
        {
            if (rb.velocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);
        }

        else
        {
            if (!pm.freeze) pm.freeze = true;

            if (pm.unlimited) pm.unlimited = false;

            if (distanceToLedge > maxLedgeGrabDistance) 
                ExitLedgeHold();

        }
    }

    private void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        holdingLedge = false;
        timeOnLedge = 0f;

        pm.restricted = false;
        pm.freeze = false;

        rb.useGravity = true;

        Invoke(nameof(ResetLastLedge), 0.75f);

    }

    private void ResetLastLedge()
    {
        lastLedge = null;
    }

}
