using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGrappling : MonoBehaviour
{
    [Header("References")]
    private PlayerMovement pm;
    [SerializeField] private Transform cam;
    [SerializeField] private Image crosshair;
    [SerializeField] private Transform gunTip;
    [SerializeField] private LayerMask isGrappleable;
    [SerializeField] LineRenderer lr;

    [Header("Grappling")]
    [SerializeField] private float maxGrappleDistance;
    [SerializeField] private float grappleDelayTime;
    [SerializeField] private float overshootYAxis;

    protected Vector3 grapplePoint;

    [Header("Animation")]
    [SerializeField] private float animationDuration;

    [Header("Cooldown")]
    [SerializeField] float grapplingCoolDown;
    private float grapplingCDTimer;

    [Header("Input")]
    [SerializeField] KeyCode grappleKey = KeyCode.Mouse1;

    protected bool grappling;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
        lr.useWorldSpace = true;
    }

    private void Update()
    {

        RaycastHit hit;

        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, isGrappleable)) crosshair.color = Color.green;
        else crosshair.color = Color.white;

        if (Input.GetKeyDown(grappleKey)) StartGrapple();

        if (grapplingCDTimer > 0)
            grapplingCDTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (grappling)
            lr.SetPosition(0, gunTip.position);
    }


    private void StartGrapple()
    {
        if (grapplingCDTimer > 0) return;

        grappling = true;

        pm.freeze = true;

        RaycastHit hit;

        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, isGrappleable))
        {
            grapplePoint = hit.transform.gameObject.GetComponent<Renderer>().bounds.center;

            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        } else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;

            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lr.enabled = true;
        /*
        lr.SetPosition(1, grapplePoint);
        */
        StartCoroutine(AnimateLine(grapplePoint));
    }

    private void ExecuteGrapple()
    {
        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 0.75f);

    }

    public void StopGrapple()
    {
        pm.freeze = false;

        grappling = false;


        grapplingCDTimer = grapplingCoolDown;

        lr.enabled = false;
    }

    private IEnumerator AnimateLine (Vector3 grapplePoint)
    {
        float startTime = Time.time;
        Vector3 startPosition = gunTip.position;
        Vector3 endPosition = grapplePoint;

        Vector3 pos = startPosition;
        while (pos != endPosition)
        {
            float t = (Time.time - startTime) / animationDuration;
            pos = Vector3.Lerp(startPosition, endPosition, t);
            lr.SetPosition(1, pos);
            yield return null;
        }


    }
}
