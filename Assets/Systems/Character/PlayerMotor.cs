using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMotor : LunarScript
{
    public float moveSpeed = 5f, moveDampTime = 0.2f, airDrag = 0.05f, jumpVelocity = 10f, groundFriction = 5;
    public Vector3 moveVelocity, airVelocity;
    Vector3 moveDampVelocity;
    public float coyoteTime;
    float currentCoyoteTime;
    public float airMoveForce;
    public bool grounded;
    Vector2 lastLookAngle;
    Vector2 lookAngle;
    CharacterController cc;
    public float gravity = -9.81f;


    public Transform head;
    public float lookSpeed;
    private void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    public override void LTimestep()
    {
        if (cc == null)
            return;

        if (cc.isGrounded)
        {
            currentCoyoteTime = 0;
        }
        else
        {
            currentCoyoteTime += Time.fixedDeltaTime;
        }
        grounded = cc.isGrounded || currentCoyoteTime < coyoteTime;

        if (grounded)
        {
            if (InputManager.JumpInput)
            {
                Jump();
            }
        }
        Move();
        Physics();
    }
    public override void LUpdate()
    {
        base.LUpdate();
        Look();
    }
    void Jump()
    {
        InputManager.JumpInput = false;
        airVelocity += Vector3.up * jumpVelocity + InputManager.MoveInput.y * moveSpeed * transform.forward + InputManager.MoveInput.x * moveSpeed * transform.right;
        grounded = false;
        currentCoyoteTime = coyoteTime;
    }
    void Look()
    {
        lookAngle += InputManager.LookInput * Time.deltaTime * lookSpeed;
        lookAngle.y = Mathf.Clamp(lookAngle.y, -89f, 89f);
        if(lastLookAngle != lookAngle)
        {
            transform.rotation = Quaternion.Euler(0, lookAngle.x, 0);
            head.localRotation = Quaternion.Euler(-lookAngle.y, 0, 0);   
        }
    }
    void Move()
    {
        moveVelocity = Vector3.SmoothDamp(moveVelocity, new Vector3(InputManager.MoveInput.x, 0, InputManager.MoveInput.y), ref moveDampVelocity, moveDampTime);
        if (grounded)
        {
            cc.Move(transform.rotation * moveVelocity * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            airVelocity += transform.rotation * moveVelocity * airMoveForce * Time.fixedDeltaTime;
        }
    }
    void Physics()
    {
        if (grounded)
        {
            airVelocity.y = -0.1f;
            airVelocity /= 1 + (groundFriction * Time.fixedDeltaTime);
        }
        else
        {
            airVelocity.y += gravity * Time.fixedDeltaTime;
            airVelocity /= 1 + (airDrag * Time.fixedDeltaTime);
        }
        cc.Move(airVelocity * Time.fixedDeltaTime);
    }
}
