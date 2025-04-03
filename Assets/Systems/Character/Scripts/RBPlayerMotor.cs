using Cinemachine;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class RBPlayerMotor : LunarScript
{
    #region Variables

    //Components
    [SerializeField, HideInInspector] protected Rigidbody rb;
    [SerializeField, HideInInspector] protected CapsuleCollider capsule;
    [SerializeField, Header("Components")] protected CinemachineVirtualCamera cineCam;

    //Transforms
    [SerializeField, Header("Transforms")] protected Transform head;
    [SerializeField] protected Transform ikAimTransform;
    [SerializeField] protected Quaternion ikAimOffset;
    [SerializeField] protected Transform crouchTransform;

    //Scriptable References
    [SerializeField, Header("Parameter References")] protected ViewParams viewParams;
    [SerializeField] protected bool canAim;
    [SerializeField] protected AimParams aimParams;
    [SerializeField] protected bool canSlide;
    [SerializeField] protected MoveParams moveParams;
    [SerializeField] Vector3 crouchTransformAxis;
    [SerializeField] protected float crouchTransformStandHeight, crouchTransformCrouchHeight;

    [SerializeField, Header("Stepping"), Tooltip("Can the player step up ledges?")] protected bool canStep;
    [SerializeField, Tooltip("The transform we cast from to check for steps")] protected Transform upperStepTransform, lowerStepTransform;
    [SerializeField] protected StepParams stepParams;

    [SerializeField, Tooltip("Can the player climb up ledges when they are facing and in contact with them in the air?")] protected bool canMantle;
    [SerializeField] protected bool debugMantle;
    [SerializeField] protected MantleParams mantleParams;

    [SerializeField, Header("Dashing"), Tooltip("Can the player dash?")] protected bool canDash;
    [SerializeField] protected DashParams dashParams;

    public float aimAmount;

    //Aiming
    protected float lookPitch;
    protected Vector2 lookDelta;
    protected Vector2 oldLook;

    protected bool aiming;
    protected bool altAiming;
    protected float headTiltAngle;
    protected float currentFOV;

    //Movement Parameters
    protected bool slowWalking;
    protected bool sprinting;
    protected bool isSliding;
    protected bool crouching;
    protected Vector3 uncrouchCheckPosition;
    protected bool canUncrouch;
    protected float currentCrouchLerp;
    protected int multiJumpsRemaining = 1;
    protected float currentAirborneIgnoreDampTime;

    //Ground check
    protected bool isGrounded;
    [SerializeField] protected Vector3 groundCheckOrigin;
    [SerializeField] protected float groundCheckDistance = 1.2f, groundCheckRadius = 0.4f;
    [SerializeField] protected LayerMask groundChecklayermask;
    [SerializeField] protected Vector3 groundNormal;
    [SerializeField] protected bool debugGroundCheck;

    protected float mantleTime, mantleDistance, mantleTimeInc;
    protected Vector3 mantleStart, mantleEnd;
    protected Rigidbody mantleTargetRB;
    protected bool mantling;
    protected Vector3 mantleEndLocalToTarget;
    float mantleCurrentEnableTime;
    Rigidbody connectedBody, lastConnectedBody;
    Vector3 connectionVelocity, connectedWorldPosition, connectedLocalPosition;
    float connectionDeltaYaw, connectionYaw, connectionLastYaw;
    /// <summary>
    /// Are we currently dashing?
    /// </summary>
    protected bool dashing;
    /// <summary>
    /// Have we used a dash? this one persists until we can use a dash again.
    /// </summary>
    [SerializeField] protected bool dashUsed;
    /// <summary>
    /// The camera's current FOV as a result of dashing
    /// </summary>
    protected float dashCurrentFOV;
    /// <summary>
    /// How far through the dash we are
    /// </summary>
    protected float dashCurrentTime;
    /// <summary>
    /// The direction we're dashing in
    /// </summary>
    protected Vector3 dashDirection;
    #endregion
    #region Unity Messages
    private void OnValidate()
    {
        if(rb == null)
            rb = GetComponent<Rigidbody>();
        if(capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        uncrouchCheckPosition = Vector3.up * moveParams.crouchObstructionVerticalOffset;

        if (crouchTransform != null)
        {
            crouchTransformCrouchHeight = crouchTransformStandHeight + moveParams.crouchedCapsuleCentre.y;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        if (debugGroundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheckOrigin, groundCheckRadius);
            Gizmos.DrawWireSphere(groundCheckOrigin + (Vector3.down * groundCheckDistance), groundCheckRadius);
            Gizmos.color = Color.red;
            if (lowerStepTransform != null)
                Gizmos.DrawWireCube(lowerStepTransform.localPosition, stepParams.stepBoxSize * 2);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(uncrouchCheckPosition, groundCheckRadius);
            Gizmos.DrawWireSphere(uncrouchCheckPosition + Vector3.up * moveParams.crouchObstructionDistance, groundCheckRadius);

            Gizmos.DrawRay(lowerStepTransform.localPosition, Vector3.forward * stepParams.stepDistance);
            Gizmos.DrawRay(lowerStepTransform.localPosition + (Vector3.up * stepParams.stepHeight) + (Vector3.forward * stepParams.stepDistance), Vector3.down * (stepParams.stepHeight + 0.03f));

        }
        if (debugMantle && mantleParams != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.cyan;
            Vector3 vec = Vector3.forward * mantleParams.mantleCheckDistance;
            Vector3 vec2 = vec + (Vector3.forward * mantleParams.mantlePointForwardOffset);
            Gizmos.DrawWireCube(mantleParams.mantleCheckOffset, mantleParams.mantleCheckBounds);
            Gizmos.DrawWireCube(mantleParams.mantleCheckOffset + vec, mantleParams.mantleCheckBounds);

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(vec2 + (Vector3.up * mantleParams.mantleMaxHeight) + mantleParams.mantleHeightRayOffset, Vector3.down * mantleParams.mantleMaxHeight);

            Gizmos.DrawRay(vec2 + (Vector3.forward * 0.1f) + mantleParams.mantleHeightRayOffset, Vector3.up * mantleParams.mantleMaxHeight);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
    private void Awake()
    {
        upperStepTransform.localPosition = lowerStepTransform.localPosition + (Vector3.forward * stepParams.stepDistance) + (Vector3.up * stepParams.stepHeight);
    }
    #endregion
    #region LunarScript
    public override void LTimestep()
    {
        CheckGround();
        CheckState();
        TryDash();
        Jump();
        CrouchPlayer();
        MovePlayer();
        if (isGrounded && InputManager.MoveInput.y > 0 && canStep && !mantling)
            ClimbSteps();
        if (!isGrounded && canMantle && !mantling)
        {
            CheckMantle();
        }
        rb.isKinematic = mantling;

        if(mantleTargetRB != null && mantling)
        {
            connectedBody = mantleTargetRB;
        }

        UpdateConnectedBody();

        if (connectedBody != null)
        {
            if (moveParams.followPlatformPosition)
                transform.position += connectionVelocity * Time.fixedDeltaTime;
            
            if(moveParams.followPlatformRotation)
                transform.rotation *= Quaternion.Euler(0, connectionDeltaYaw, 0);
        }
        
        lastConnectedBody = connectedBody;
    }
    void UpdateConnectedBody()
    {
        if (connectedBody == null)
            return;

        connectionYaw = connectedBody.transform.eulerAngles.y;
        if(connectedBody == lastConnectedBody)
        {
            if (moveParams.followPlatformPosition)
            {
                Vector3 connectionMovement = connectedBody.transform.TransformPoint(connectedLocalPosition) - connectedWorldPosition;
                connectionVelocity = connectionMovement / Time.fixedDeltaTime;
                //connectionVelocity = rb.velocity - connectionVelocity;

            }
            if (moveParams.followPlatformRotation)
            {
                connectionDeltaYaw = connectionYaw - connectionLastYaw;
            }
        }

        connectedWorldPosition = rb.position;
        connectedLocalPosition = connectedBody.transform.InverseTransformPoint(connectedWorldPosition);
        connectionLastYaw = connectionYaw;
    }
    public override void LUpdate()
    {
        base.LUpdate();
        CheckAimState();
        Look();
    }
    #endregion
    #region View Update
    void Look()
    {
        if (InputManager.LookInput != Vector2.zero)
        {
            Vector2 lookSpeed = viewParams.lookSpeed * (aiming || altAiming ? viewParams.aimLookModifier : 1);
            lookPitch -= Time.deltaTime * lookSpeed.y * InputManager.LookInput.y;
            lookPitch = Mathf.Clamp(lookPitch, -viewParams.lookPitchClamp, viewParams.lookPitchClamp);
            transform.rotation *= Quaternion.Euler(0, InputManager.LookInput.x * lookSpeed.x * Time.deltaTime, 0);
            oldLook = new(transform.eulerAngles.x, lookPitch);
        }
        if (lookDelta != oldLook)
            lookDelta = new Vector2(transform.eulerAngles.x % 360, lookPitch) - oldLook;

        headTiltAngle = Mathf.Lerp(headTiltAngle, (isSliding ? viewParams.slideHeadTiltAngle : viewParams.strafeHeadTiltAngle) * InputManager.MoveInput.x, viewParams.headTiltSpeed * Time.fixedDeltaTime);
        head.localRotation = Quaternion.Euler(lookPitch, 0, 0);
        cineCam.transform.localEulerAngles = new Vector3(0, 0, headTiltAngle);

        if(ikAimTransform != null)
        {
            ikAimTransform.localRotation = head.localRotation * ikAimOffset;
        }
    }
    void CheckAimState()
    {
        aiming = InputManager.AimInput;
        altAiming = aiming && InputManager.AltAimInput;
        if (aiming)
        {
            sprinting = false;
        }
        aimAmount = Mathf.MoveTowards(aimAmount, aiming ? 1 : 0, aimParams.fovMoveSpeed * Time.deltaTime);

        //The mother of all ternary statements...
        float fov = viewParams.baseFOV +
            //Are we dashing?
            (dashing ? dashCurrentFOV :
            //Are we aiming?
            altAiming ? aimParams.altAimFOV :
            //Are we side aiming?
            aiming ? aimParams.aimFOV :
            //Are we sliding
            isSliding ? viewParams.slideFOV :
            //Are we sprinting or sliding?
            ((sprinting && InputManager.MoveInput != Vector2.zero) || isSliding) ? viewParams.sprintFOV :
            //Are we moving normally or crouching?
            0);

        currentFOV = Mathf.Lerp(currentFOV, fov, Time.deltaTime * aimParams.fovMoveSpeed);
        cineCam.m_Lens.FieldOfView = currentFOV;
    }
    #endregion
    #region Movement
    void CheckGround()
    {
        if (Physics.SphereCast(transform.position + groundCheckOrigin, groundCheckRadius, -transform.up, out RaycastHit hit, groundCheckDistance, groundChecklayermask))
        {
            isGrounded = hit.normal.y > 0.4f;
            if (isGrounded)
            {
                if (moveParams.canMultiJump)
                {
                    multiJumpsRemaining = moveParams.multiJumps;
                }
                if (hit.rigidbody && hit.rigidbody.isKinematic)
                    connectedBody = hit.rigidbody;
                else
                    connectedBody = null;
                groundNormal = hit.normal;
                return;
            }
        }
        isGrounded = false;
        if (connectedBody)
        {
            connectedBody = null;
            rb.AddForce(connectionVelocity, ForceMode.VelocityChange);
        }
        if (moveParams.canMultiJump && moveParams.multiJumps == multiJumpsRemaining)
        {
            multiJumpsRemaining = moveParams.multiJumps - 1;
        }
        return;
    }
    void CheckState()
    {
        if (!isGrounded || dashing)
        {
            rb.drag = dashing ? dashParams.dashForceDamping : currentAirborneIgnoreDampTime <= 0 ? 0 : moveParams.airborneDamping;
        }
        else
        {
            rb.drag = isSliding ? moveParams.slideDamping : moveParams.walkDamping;
        }
        //If we are sliding, there's some state checking we need to do for that.
        if (isSliding)
        {
            UpdateSlide();
        }
        else
        {
            //we'll assign Crouching first, and sprinting at the end of CheckState()
            //This means that sprinting starts the frame AFTER you press it, while crouching should start immediately upon pressing crouch.
            //Doing it this way allows me to detect slides better - if we're already sprinting and THEN we crouch, we'll slide. If we're crouching and then we sprint, we'll get up and sprint.
            if(crouching || isSliding)
            {
                CheckUncrouch();
            }

            if (InputManager.CrouchInput)
            {
                crouching = InputManager.CrouchInput && !isSliding;
            }
            else
            {
                crouching = !canUncrouch && !isSliding;
            }
            if(sprinting || !isGrounded)
            {
                float flatVelSqr = new Vector3(rb.velocity.x, 0, rb.velocity.z).sqrMagnitude;
                //If we try to crouch while sprinting or airborne, we'll slide instead.
                if (canSlide && crouching && flatVelSqr > 4f)
                {
                    StartSlide();
                }
            }
            sprinting = InputManager.SprintInput && !aiming && !isSliding;
            if (crouching)
            {
                if (sprinting)
                {
                    crouching = false;
                    InputManager.CrouchInput = false;
                }
            }
        }
    }
    void CheckUncrouch()
    {
        canUncrouch = !Physics.SphereCast(transform.position + uncrouchCheckPosition, groundCheckRadius, Vector3.up, out RaycastHit hit, moveParams.crouchObstructionDistance, groundChecklayermask, QueryTriggerInteraction.Ignore);
    }
    void StartSlide()
    {
        isSliding = true;
        if(isGrounded)
            rb.AddForce(transform.forward * moveParams.slidePushOffForce, ForceMode.Impulse);
    }
    void UpdateSlide()
    {
        if (!InputManager.CrouchInput || rb.velocity.sqrMagnitude < 4f)
        {
            StopSlide();
        }
    }
    void StopSlide()
    {
        isSliding = false;
        sprinting = false;
        crouching = true;
    }
    void CrouchPlayer()
    {
        currentCrouchLerp = Mathf.MoveTowards(currentCrouchLerp, crouching || isSliding ? 1 : 0, moveParams.crouchHeadSpeed * Time.fixedDeltaTime);
        head.localPosition = Vector3.Lerp(Vector3.up * viewParams.standingHeadHeight, Vector3.up * viewParams.crouchedHeadHeight, currentCrouchLerp);
        capsule.height = Mathf.Lerp(moveParams.standingCapsuleHeight, moveParams.crouchedCapsuleHeight, currentCrouchLerp);
        capsule.center = Vector3.Lerp(moveParams.standingCapsuleCentre, moveParams.crouchedCapsuleCentre, currentCrouchLerp);
        if(crouchTransform != null)
        {
            crouchTransform.localPosition = Vector3.Lerp(crouchTransformAxis * crouchTransformStandHeight, crouchTransformAxis * crouchTransformCrouchHeight, currentCrouchLerp);
        }
    }
    void MovePlayer()
    {
        if (isGrounded && !dashing)
        {
            if (isSliding)
            {
                rb.AddForce(Vector3.ProjectOnPlane(InputManager.MoveInput.x * moveParams.slideSteerForce * transform.right, groundNormal));
            }
            else
            {

                Vector3 right = Vector3.Cross(-transform.forward, groundNormal);
                Vector3 forward = Vector3.Cross(right, groundNormal);

                Vector3 moveForce = (aiming ? moveParams.aimWalkMoveMultiply : altAiming ? moveParams.sideAimWalkMoveMultiply : 1) 
                    * (sprinting ? moveParams.sprintForceMultiply : crouching ? moveParams.crouchWalkForceMultiply :
                    slowWalking ? moveParams.slowWalkForceMultiply : 1)
                    * moveParams.baseMoveForce
                    * (right * InputManager.MoveInput.x + forward * InputManager.MoveInput.y);
                rb.AddForce(moveForce);
                rb.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
            };
            //Add a force to keep the player on the ground. This can be scaled if the player bounces too much
            rb.AddForce(-groundNormal * moveParams.groundPushForce);
        }
        else
        {
            rb.AddForce(transform.rotation * new Vector3(InputManager.MoveInput.x, 0, InputManager.MoveInput.y) * moveParams.airMoveForce);
        }
    }
    void Jump()
    {
        if (InputManager.JumpInput)
        {
            if(isGrounded || moveParams.canMultiJump && multiJumpsRemaining > 0)
            {
                InputManager.JumpInput = false;
                rb.velocity.Scale(new(1, 0, 1));
                rb.AddForce(Vector3.up * moveParams.jumpForce, ForceMode.VelocityChange);
                if (moveParams.canMultiJump)
                {
                    multiJumpsRemaining--;
                }
            }
        }
    }
    void TryDash()
    {
        if (InputManager.DashInput && !dashUsed)
        {
            StartCoroutine(DashCoroutine());
            InputManager.DashInput = false;
        }
    }
    IEnumerator DashCoroutine()
    {
        dashing = true;
        dashUsed = true;
        float timeInc = Time.fixedDeltaTime / dashParams.dashDuration;
        if (InputManager.MoveInput == Vector2.zero)
        {
            dashDirection = Vector3.Lerp(transform.forward, head.forward, dashParams.dashDirectionPitchContribution);
            
        }
        else
        {
            dashDirection = (((transform.forward * (1 - dashParams.dashDirectionPitchContribution)) + (head.forward * dashParams.dashDirectionPitchContribution)) * InputManager.MoveInput.y)
                + (transform.right * InputManager.MoveInput.x);
        }
        dashDirection += Vector3.up * dashParams.dashVerticalAmount;
        dashDirection.Normalize();
        while (dashCurrentTime < 1)
        {
            dashCurrentTime += timeInc;
            dashCurrentFOV = dashParams.dashFOVCurve.Evaluate(dashCurrentTime) * dashParams.dashMaxFOV;
            rb.AddForce((new Vector3(dashDirection.x, 0, dashDirection.z) * dashParams.dashForce.x) + (dashDirection.y * dashParams.dashForce.y * Vector3.up));
            if(dashParams.dashSteerSpeed > 0)
            {
                dashDirection = Vector3.Lerp(dashDirection,
                    Quaternion.Euler(head.eulerAngles.x * dashParams.dashDirectionPitchContribution, head.eulerAngles.y, 0) * Vector3.forward, dashParams.dashSteerSpeed * Time.fixedDeltaTime);
            }
            yield return new WaitForFixedUpdate();
        }
        dashing = false;
        rb.drag = moveParams.airborneDamping;
        yield return new WaitForSeconds(dashParams.dashDelayTime);
        dashCurrentTime = 0;
        dashUsed = false;
        yield return null;
    }
    void ClimbSteps()
    {
        if (Physics.BoxCast(lowerStepTransform.position, stepParams.stepBoxSize, transform.forward, out RaycastHit hit, transform.rotation, stepParams.stepDistance, stepParams.stepLayermask, QueryTriggerInteraction.Ignore))
        {
            if(Physics.BoxCast(upperStepTransform.position, stepParams.stepBoxSize, -transform.up, out RaycastHit hit2, transform.rotation, stepParams.stepDistance + 0.03f, stepParams.stepLayermask, QueryTriggerInteraction.Ignore))
            {

                Debug.DrawRay(hit2.point, hit2.normal);
                Debug.Log($"hit2 normal = {hit2.normal}");
                if (hit2.normal.y < 0.85f)
                    return;
                if(hit2.rigidbody != null)
                {
                    mantleTargetRB = hit2.rigidbody;
                }
                print("climbing steps");
                StartCoroutine(MantleToPoint(hit2.point, stepParams.stepSpeed));
            }
            
        }
    }
    void CheckMantle()
    {
        if (!(InputManager.MoveInput.y > 0 || InputManager.JumpInput))
        {
            return;
        }

        if (Physics.BoxCast(transform.TransformPoint(mantleParams.mantleCheckOffset), mantleParams.mantleCheckBounds / 2, transform.forward,
            out RaycastHit hit, transform.rotation, mantleParams.mantleCheckDistance, groundChecklayermask, QueryTriggerInteraction.Ignore))
        {
            Vector3 rayOrigin = new Vector3(hit.point.x, transform.position.y + mantleParams.mantleMaxHeight, hit.point.z) + (transform.forward * mantleParams.mantlePointForwardOffset) + mantleParams.mantleHeightRayOffset;
            Debug.DrawLine(hit.point, rayOrigin, Color.red, 1f);
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, mantleParams.mantleMaxHeight, groundChecklayermask, QueryTriggerInteraction.Ignore))
            {
                if (!Physics.Raycast(hit.point + (Vector3.down * 0.02f), Vector3.up, mantleParams.mantleMaxHeight + 0.02f, groundChecklayermask, QueryTriggerInteraction.Ignore))
                {
                    if(hit.rigidbody != null)
                        mantleTargetRB = hit.rigidbody;
                    print("mantling");
                    StartCoroutine(MantleToPoint(hit.point, mantleParams.mantleSpeed));
                }
            }
        }
    }
    IEnumerator MantleToPoint(Vector3 point, float speed)
    {
        mantleStart = transform.position;
        InputManager.JumpInput = false;
        mantling = true;
        mantleTime = 0;
        point += (Vector3.up * mantleParams.mantlePointOffset);

        UpdateMantleParams(point, true, speed);
        WaitForFixedUpdate wff = new();
        Vector2 latpos;
        float vertpos;
        while (mantleTime < 1 && mantling)
        {
            mantleTime += mantleTimeInc;
            latpos = Vector2.Lerp(new(mantleStart.x, mantleStart.z), new(mantleEnd.x, mantleEnd.z),
                mantleParams.mantleLateralPath.Evaluate(mantleTime));
            vertpos = Mathf.Lerp(mantleStart.y, mantleEnd.y, mantleParams.mantleVerticalPath.Evaluate(mantleTime));

            transform.position = new(latpos.x, vertpos, latpos.y);

            if (mantleParams.mantleFollowsTransform)
                UpdateMantleParams(point, false, speed);

            yield return wff;
        }
        rb.isKinematic = false;
        //rb.AddForce(((transform.forward * InputManager.MoveInput.y) 
        //    + (transform.right * InputManager.MoveInput.x)) * baseMoveForce);

        if(mantleTime >= 1)
        {
            rb.velocity = transform.rotation * new Vector3(mantleParams.mantleDismountSpeed * InputManager.MoveInput.x, rb.velocity.y, mantleParams.mantleDismountSpeed * InputManager.MoveInput.y);
        }

        mantling = false;
    }
    void UpdateMantleParams(Vector3 point, bool initialise = false, float speed = 2)
    {
        if (mantleTargetRB != null)
        {
            if (initialise)
            {
                mantleEndLocalToTarget = mantleTargetRB.transform.InverseTransformPoint(point);
            }
            else
            {
                point = mantleTargetRB.transform.TransformPoint(mantleEndLocalToTarget);
            }
        }

        mantleEnd = point;
        mantleTargetRB = null;

        Debug.DrawLine(mantleStart, mantleStart, Color.red, 2f);

        mantleDistance = Vector3.Distance(mantleStart, mantleEnd);
        mantleTimeInc = (speed / mantleDistance) * Time.fixedDeltaTime;

    }
    #endregion
}
