using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class NetPlayerMotor : LunarNetScript
{
    NetBufferManager MyBufferManager => playerEntity.bufferManager;


    internal bool jumpInput, slowWalkInput, slideInput, crouchInput, sprintInput;
    internal Vector2 lookInput, moveInput;

    [SerializeField] internal Rigidbody rigidbody;
    [SerializeField] NetPlayerEntity playerEntity;
    [SerializeField] internal CinemachineVirtualCamera worldCamera, viewCamera;

    [SerializeField] internal Transform head;
    [SerializeField] internal Transform ikAimtransform;
    [SerializeField] internal Quaternion ikAimOffset;
    [SerializeField] internal Transform crouchTransform;
    [SerializeField] internal float crouchTransformStandHeight, crouchTransformCrouchHeight;
    [SerializeField] internal Vector3 crouchTransformAxis;

    [SerializeField] internal ViewParams viewParams;
    [SerializeField] internal AimParams defaultAimParams;
    [SerializeField] internal MoveParams moveParams;
    [SerializeField] internal StepParams stepParams;
    [SerializeField] internal MantleParams mantleParams;

    [SerializeField] internal Transform upperStepTransform, lowerStepTransform;
    [SerializeField] internal bool debugMantle;




    internal float lookPitch;
    internal Vector2 lookDelta, oldLook;

    internal bool slowWalking, sprinting, sliding, crouching, canUncrouch;
    internal float currentCrouchLerp, currentAirborneIgnoreDampTime;
    internal int jumpsRemaining = 1;
    internal Vector3 uncrouchCheckPosition;

    internal bool isGrounded;
    [SerializeField] protected Vector3 groundCheckOrigin;
    [SerializeField] protected float groundCheckDistance = 1.2f, groundCheckRadius = 0.4f;
    [SerializeField] protected LayerMask groundLayermask;
    [SerializeField] protected bool debugGroundCheck;
    protected Vector3 groundNormal;

    internal bool mantling;
    protected float mantleTime, mantleDistance, mantleTimeIncrement;
    protected Vector3 mantleStart, mantleEnd, mantleEndLocalToTarget;
    protected Rigidbody mantleTargetRigidbody;

    Rigidbody connectedBody, lastConnectedBody;
    Vector3 connectionVelocity, connectedWorldPos, connectedLocalPos;
    float connectionDeltaYaw, connectionYaw, connectionLastYaw;



    [SerializeField] bool debugReconciliation;
    [SerializeField] GameObject serverCube, clientCube;

    private void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();

        uncrouchCheckPosition = Vector3.up * moveParams.crouchObstructionVerticalOffset;

        if (crouchTransform != null && moveParams != null)
        {
            crouchTransformCrouchHeight = crouchTransformStandHeight + moveParams.crouchedCapsuleCentre.y;
        }
        if (debugReconciliation)
        {
            if(serverCube == null)
            {
                serverCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                serverCube.transform.SetParent(transform, false);
                DestroyImmediate(serverCube.GetComponent<Collider>());
            }
            if(clientCube == null)
            {
                clientCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clientCube.transform.SetParent(transform, false);
                DestroyImmediate(clientCube.GetComponent<Collider>());
            }
        }
        if(serverCube != null)
        serverCube.SetActive(debugReconciliation);
        if(clientCube != null)
        clientCube.SetActive(debugReconciliation);
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
            if (lowerStepTransform != null && stepParams != null)
            {
                Gizmos.DrawWireCube(lowerStepTransform.localPosition, stepParams.stepBoxSize * 2);
                Gizmos.DrawRay(lowerStepTransform.localPosition, Vector3.forward * stepParams.stepDistance);
                Gizmos.DrawRay(lowerStepTransform.localPosition + (Vector3.up * stepParams.stepHeight) 
                    + (Vector3.forward * stepParams.stepDistance), Vector3.down * (stepParams.stepHeight + 0.03f));
            }
            Gizmos.color = Color.green;
            if(moveParams != null)
            {
                Gizmos.DrawWireSphere(uncrouchCheckPosition, groundCheckRadius);
                Gizmos.DrawWireSphere(uncrouchCheckPosition + Vector3.up * moveParams.crouchObstructionDistance, groundCheckRadius);
            }
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

    private void Start()
    {
        upperStepTransform.localPosition = lowerStepTransform.localPosition + (Vector3.forward * stepParams.stepDistance)
            + (Vector3.up * stepParams.stepHeight);
    }
    #region Update
    public override void LTimestep()
    {
        if (IsOwner)
        {
            if (MyBufferManager.ShouldTick)
            {
                ClientTick();
                ServerTick();   
            }
        }
    }

    void ClientTick()
    {
        if (!IsClient)
            return;

        var currentTick = MyBufferManager.Tick;
        int bufferIndex = currentTick % MyBufferManager.bufferSize;

        if (IsOwner)
        {
            crouchInput = InputManager.CrouchInput;
            moveInput = InputManager.MoveInput;
            sprintInput = InputManager.SprintInput;
            jumpInput = InputManager.JumpInput;
            slowWalkInput = InputManager.SlowWalkInput;
        }

        InputPayload inputpayload = new()
        {
            crouchInput = crouchInput,
            moveInput = moveInput,
            jumpInput = jumpInput,
            sprintInput = sprintInput,
            tick = currentTick,
        };

        MyBufferManager.SendInputPayload_RPC(inputpayload);

        StatePayload statePayload = ProcessMovement(inputpayload);
        if(debugReconciliation && clientCube != null)
        {
            clientCube.transform.position = statePayload.position + Vector3.up * 4;
        }
        MyBufferManager.clientStateBuffer.Add(statePayload, bufferIndex);

        ServerReconciliation();
    }
    void ServerTick()
    {
        var bufferIndex = -1;
        while(MyBufferManager.serverInputQueue.Count > 0)
        {
            InputPayload input = MyBufferManager.serverInputQueue.Dequeue();
            bufferIndex = input.tick % MyBufferManager.bufferSize;
            StatePayload state = ProcessMovement(input);
            MyBufferManager.serverStateBuffer.Add(state, bufferIndex);

            if (debugReconciliation && serverCube != null)
            {
                serverCube.transform.position = state.position + Vector3.up * 6;
            }
        }

        if (bufferIndex == -1) return;
            
        MyBufferManager.SendStateToClient_RPC(MyBufferManager.serverStateBuffer.Get(bufferIndex));
    }
    bool ShouldReconcile()
    {
        bool isNewServerState = !MyBufferManager.lastServerState.Equals(default);
        bool isLastStateUndefinedOrDifferent = MyBufferManager.lastProcessedState.Equals(default) ||
            !MyBufferManager.lastProcessedState.Equals(MyBufferManager.lastServerState);

        return isNewServerState && isLastStateUndefinedOrDifferent;
    }
    void ServerReconciliation()
    {
        bool shouldReconcile = ShouldReconcile();
        if (!shouldReconcile)
            return;

        float positionError = 0;
        int bufferIndex = -1;
        StatePayload rewindState;

        bufferIndex = MyBufferManager.lastServerState.tick % MyBufferManager.bufferSize;
        if(bufferIndex - 1 < 0)
        {
            //Cannot reconcile, not enough information to do so
            return;
        }
        rewindState = IsHost ? 
            (MyBufferManager.serverStateBuffer.Get(bufferIndex - 1)) 
            : (MyBufferManager.lastServerState);

        positionError = Vector3.Distance(rewindState.position, MyBufferManager.clientStateBuffer.Get(bufferIndex).position);
        if(positionError > MyBufferManager.reconciliationPositionThreshold)
        {
            ReconcileState(rewindState);
        }

        MyBufferManager.lastProcessedState = MyBufferManager.lastServerState;


    }
    void ReconcileState(StatePayload rewindState)
    {
        rigidbody.position = rewindState.position;
        rigidbody.velocity = rewindState.velocity;
        sliding = rewindState.sliding;
        crouching = rewindState.crouching;
        sprinting = rewindState.sprinting;

        if (!rewindState.Equals(MyBufferManager.lastServerState)) return;

        Debug.Log($"Reconciling from {MyBufferManager.Tick} to {rewindState.tick}!");

        MyBufferManager.clientStateBuffer.Add(rewindState, rewindState.tick);

        int tickToReplay = MyBufferManager.lastServerState.tick;
        while (tickToReplay < MyBufferManager.Tick)
        {
            int bufferIndex = tickToReplay % MyBufferManager.bufferSize;
            StatePayload payload = ResimulateMovement(MyBufferManager.clientInputBuffer.Get(bufferIndex));

            MyBufferManager.clientStateBuffer.Add(payload, bufferIndex);
            tickToReplay++;
        }

    }

    StatePayload ResimulateMovement(InputPayload inputPayload)
    {
        Physics.simulationMode = SimulationMode.Script;

        StatePayload payload = ProcessMovement(inputPayload);

        Physics.Simulate(Time.fixedDeltaTime);
        Physics.simulationMode = SimulationMode.FixedUpdate;

        return payload;
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        MovementUpdate();




        return new StatePayload()
        {
            tick = input.tick,
            crouching = crouching,
            sprinting = sprinting,
            sliding = sliding,
            position = rigidbody.position,
            velocity = rigidbody.velocity,
        };
    }

    void MovementUpdate()
    {

        if (IsHost)
        {
            if (InputManager.DashInput)
            {
                InputManager.DashInput = false;
                rigidbody.position += head.forward * 10;
            }
        }

        CheckGround();
        CheckMoveState();
        Jump();
        CrouchPlayer();
        MovePlayer();
        if (isGrounded && moveInput.y > 0 && !mantling)
        {
            ClimbSteps();
        }
        if (!isGrounded && !mantling)
        {
            CheckMantle();
        }
        rigidbody.isKinematic = mantling;

        if (mantleTargetRigidbody != null && mantling)
        {
            connectedBody = mantleTargetRigidbody;
        }
        UpdateConnectedMotion();
        if (connectedBody != null)
        {
            if (moveParams.followPlatformPosition)
            {
                rigidbody.position += connectionVelocity * Time.fixedDeltaTime;
            }
            if (moveParams.followPlatformRotation)
            {
                rigidbody.rotation *= Quaternion.Euler(0, connectionDeltaYaw, 0);
            }
        }
        lastConnectedBody = connectedBody;

    }

    public override void LUpdate()
    {
        base.LUpdate();
        if (IsOwner)
        {
            lookInput = InputManager.LookInput;
            CheckAimState();
            RotatePlayer();
        }
    }
    #endregion Update

    #region Movement
    void CheckGround()
    {
        if (Physics.SphereCast(transform.position + groundCheckOrigin, groundCheckRadius, -transform.up,
    out RaycastHit hit, groundCheckDistance, groundLayermask))
        {
            isGrounded = hit.normal.y > 0.4f;
            if (isGrounded)
            {
                if (moveParams.canMultiJump)
                {
                    jumpsRemaining = moveParams.multiJumps;
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
            rigidbody.AddForce(connectionVelocity, ForceMode.VelocityChange);
        }
        if (moveParams.canMultiJump && moveParams.multiJumps == jumpsRemaining)
        {
            jumpsRemaining = moveParams.multiJumps - 1;
        }
        return;
    }
    void CheckMoveState()
    {
        rigidbody.drag = isGrounded ? (sliding ? moveParams.slideDamping : moveParams.walkDamping) 
            : (currentAirborneIgnoreDampTime <= 0 ? 0 : moveParams.airborneDamping);

        if (sliding)
        {
            UpdateSlide();
        }
        else
        {
            if(crouching || sliding)
            {
                CheckUncrouch();
            }
            if (InputManager.CrouchInput)
            {
                crouching = crouchInput && !sliding;
            }
            else
            {
                crouching = !canUncrouch && !sliding;
            }
        }

        if(sprinting || !isGrounded)
        {
            float flatVelSqr = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z).sqrMagnitude;
            if(crouching && flatVelSqr > 4f)
            {
                StartSlide();
            }
        }

        sprinting = sprintInput && !sliding;
        if(crouching && sprinting)
        {
            crouching = false;
            crouchInput = false;
            if (IsOwner)
            {
                InputManager.CrouchInput = false;
            }
        }
    }
    void StartSlide()
    {
        sliding = true;
        if (isGrounded)
            rigidbody.AddForce(transform.forward * moveParams.slidePushOffForce, ForceMode.Impulse);
    }
    void UpdateSlide()
    {
        if (!crouchInput || rigidbody.velocity.sqrMagnitude < 4f)
        {
            StopSlide();
        }
    }
    void StopSlide()
    {
        sliding = false;
        sprinting = false;
        crouching = true;
    }
    void CheckUncrouch()
    {
        canUncrouch = !Physics.SphereCast(transform.position + uncrouchCheckPosition, groundCheckRadius, Vector3.up, out RaycastHit hit, moveParams.crouchObstructionDistance, groundLayermask, QueryTriggerInteraction.Ignore);
    }
    void Jump()
    {
        if (jumpInput)
        {
            if (isGrounded || jumpsRemaining > 0)
            {
                if (IsOwner)
                {
                    InputManager.JumpInput = false;
                }
                jumpInput = false;
                rigidbody.velocity.Scale(new(1, 0, 1));
                rigidbody.AddForce(Vector3.up * moveParams.jumpForce, ForceMode.VelocityChange);
                jumpsRemaining--;
            }
        }
    }
    void CrouchPlayer()
    {
        currentCrouchLerp = Mathf.MoveTowards(currentCrouchLerp, crouching || sliding ? 1 : 0, moveParams.crouchHeadSpeed * Time.fixedDeltaTime);
        head.localPosition = Vector3.Lerp(Vector3.up * viewParams.standingHeadHeight, Vector3.up * viewParams.crouchedHeadHeight, currentCrouchLerp);
        if (crouchTransform != null)
        {
            crouchTransform.localPosition = Vector3.Lerp(crouchTransformAxis * crouchTransformStandHeight, crouchTransformAxis * crouchTransformCrouchHeight, currentCrouchLerp);
        }
    }
    void MovePlayer()
    {
        if (isGrounded)
        {
            if (sliding)
            {
                rigidbody.AddForce(Vector3.ProjectOnPlane(moveInput.x * moveParams.slideSteerForce * transform.right, groundNormal));
            }
            else
            {

                Vector3 right = Vector3.Cross(-transform.forward, groundNormal);
                Vector3 forward = Vector3.Cross(right, groundNormal);

                Vector3 moveForce =  (sprinting ? moveParams.sprintForceMultiply : crouching ? moveParams.crouchWalkForceMultiply :
                    slowWalking ? moveParams.slowWalkForceMultiply : 1)
                    * moveParams.baseMoveForce
                    * (right * moveInput.x + forward * moveInput.y);
                rigidbody.AddForce(moveForce);
                rigidbody.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
            };
            //Add a force to keep the player on the ground. This can be scaled if the player bounces too much
            rigidbody.AddForce(-groundNormal * moveParams.groundPushForce);
        }
        else
        {
            rigidbody.AddForce(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * moveParams.airMoveForce);
        }
    }
    void CheckMantle()
    {
        if (!(moveInput.y > 0 || InputManager.JumpInput))
        {
            return;
        }

        if (Physics.BoxCast(transform.TransformPoint(mantleParams.mantleCheckOffset), mantleParams.mantleCheckBounds / 2, transform.forward,
            out RaycastHit hit, transform.rotation, mantleParams.mantleCheckDistance, groundLayermask, QueryTriggerInteraction.Ignore))
        {
            Vector3 rayOrigin = new Vector3(hit.point.x, transform.position.y + mantleParams.mantleMaxHeight, hit.point.z) + (transform.forward * mantleParams.mantlePointForwardOffset) + mantleParams.mantleHeightRayOffset;
            Debug.DrawLine(hit.point, rayOrigin, Color.red, 1f);
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, mantleParams.mantleMaxHeight, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if (!Physics.Raycast(hit.point + (Vector3.down * 0.02f), Vector3.up, mantleParams.mantleMaxHeight + 0.02f, groundLayermask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.rigidbody != null)
                        mantleTargetRigidbody = hit.rigidbody;
                    print("mantling");
                    StartCoroutine(MantleToPoint(hit.point, mantleParams.mantleSpeed));
                }
            }
        }
    }
    void UpdateConnectedMotion()
    {
        if (connectedBody == null)
            return;

        connectionYaw = connectedBody.rotation.eulerAngles.y;
        if(connectedBody == lastConnectedBody)
        {
            if (moveParams.followPlatformPosition)
            {
                Vector3 connectedDeltaPos = connectedBody.transform.TransformPoint(connectedLocalPos) - connectedWorldPos;
                connectionVelocity = connectedDeltaPos / Time.fixedDeltaTime;
            }
            if (moveParams.followPlatformRotation)
            {
                connectionDeltaYaw = connectionYaw - connectionLastYaw;
            }
        }

        connectedWorldPos = rigidbody.position;
        connectedLocalPos = connectedBody.transform.InverseTransformPoint(connectedWorldPos);

        connectionLastYaw = connectionYaw;
    }
    void ClimbSteps()
    {
        if (Physics.BoxCast(lowerStepTransform.position, stepParams.stepBoxSize, transform.forward, out RaycastHit hit, transform.rotation, stepParams.stepDistance, stepParams.stepLayermask, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Dot(hit.normal, transform.up) > 0.5f)
                return;

            if (Physics.BoxCast(upperStepTransform.position, stepParams.stepBoxSize, -transform.up, out RaycastHit hit2, transform.rotation, stepParams.stepDistance + 0.03f, stepParams.stepLayermask, QueryTriggerInteraction.Ignore))
            {

                Debug.DrawRay(hit2.point, hit2.normal);
                Debug.Log($"hit2 normal = {hit2.normal}");
                if (hit2.normal.y < 0.85f)
                    return;
                if (hit2.rigidbody != null)
                {
                    mantleTargetRigidbody = hit2.rigidbody;
                }
                print("climbing steps");
                StartCoroutine(MantleToPoint(hit2.point, stepParams.stepSpeed));
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
            mantleTime += mantleTimeIncrement;
            latpos = Vector2.Lerp(new(mantleStart.x, mantleStart.z), new(mantleEnd.x, mantleEnd.z),
                mantleParams.mantleLateralPath.Evaluate(mantleTime));
            vertpos = Mathf.Lerp(mantleStart.y, mantleEnd.y, mantleParams.mantleVerticalPath.Evaluate(mantleTime));

            transform.position = new(latpos.x, vertpos, latpos.y);

            if (mantleParams.mantleFollowsTransform)
                UpdateMantleParams(point, false, speed);

            yield return wff;
        }
        rigidbody.isKinematic = false;
        //rb.AddForce(((transform.forward * InputManager.MoveInput.y) 
        //    + (transform.right * InputManager.MoveInput.x)) * baseMoveForce);

        if (mantleTime >= 1)
        {
            rigidbody.velocity = transform.rotation * new Vector3(mantleParams.mantleDismountSpeed * moveInput.x, rigidbody.velocity.y, mantleParams.mantleDismountSpeed * moveInput.y);
        }

        mantling = false;
    }
    void UpdateMantleParams(Vector3 point, bool initialise = false, float speed = 2)
    {
        if (mantleTargetRigidbody != null)
        {
            if (initialise)
            {
                mantleEndLocalToTarget = mantleTargetRigidbody.transform.InverseTransformPoint(point);
            }
            else
            {
                point = mantleTargetRigidbody.transform.TransformPoint(mantleEndLocalToTarget);
            }
        }

        mantleEnd = point;
        mantleTargetRigidbody = null;

        Debug.DrawLine(mantleStart, mantleStart, Color.red, 2f);

        mantleDistance = Vector3.Distance(mantleStart, mantleEnd);
        mantleTimeIncrement = (speed / mantleDistance) * Time.fixedDeltaTime;

    }
    #endregion Movement
    #region Looking
    void CheckAimState()
    {
        //Do more later
    }
    void RotatePlayer()
    {
        if(InputManager.LookInput != Vector2.zero)
        {
            Vector2 lookSpeed = viewParams.lookSpeed;
            lookPitch = Mathf.Clamp(lookPitch - Time.deltaTime * lookSpeed.y * lookInput.y, -viewParams.lookPitchClamp, viewParams.lookPitchClamp);
            head.localRotation = Quaternion.Euler(lookPitch, 0, 0);
            transform.rotation *= Quaternion.Euler(0, lookInput.x * lookSpeed.x * Time.deltaTime, 0);
            oldLook = new(transform.eulerAngles.x, lookPitch);
        }
        if(lookDelta != oldLook)
        {
            lookDelta = new Vector2(transform.eulerAngles.y % 360, lookPitch) - oldLook;
        }

        if(ikAimtransform != null)
        {
            ikAimtransform.localRotation = head.localRotation * ikAimOffset;
        }
    }
    #endregion Looking
}
