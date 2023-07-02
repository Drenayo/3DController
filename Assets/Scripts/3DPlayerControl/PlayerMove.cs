using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public Transform orientation;

    public float groundDrag;
    public LayerMask GroundMask;
    public float playerHeight;
    private bool grounded;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool readyToJump;

    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    private float hor;
    private float ver;
    private Vector3 moveDirection;
    private Rigidbody rig;

    // 最大斜坡角度
    public float maxSlopeAngle;
    // 斜坡射线检测
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public MovementState state;
    public enum MovementState
    {
        Walking,
        Sprinting,
        Crouching,
        Air
    }

    public Vector3 速度;

    void Start()
    {
        rig = GetComponent<Rigidbody>();
        rig.freezeRotation = true;

        ResetJump();

        startYScale = transform.localScale.y;
    }


    void Update()
    {
        速度 = rig.velocity;
        MyInput();
        GroundAddDrag();
        SpeedControl();
        StateHandler();
    }
    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        hor = Input.GetAxisRaw("Horizontal");
        ver = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Space) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // 下蹲
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rig.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // 起身
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            rig.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
    }

    private void StateHandler()
    {
        if (grounded && Input.GetKey(KeyCode.LeftShift))
        {
            state = MovementState.Sprinting;
            moveSpeed = sprintSpeed;
        }
        else if (grounded)
        {
            state = MovementState.Walking;
            moveSpeed = walkSpeed;
        }
        else if (grounded && Input.GetKeyDown(KeyCode.LeftControl))
        {
            state = MovementState.Crouching;
            moveSpeed = crouchSpeed;
        }
        else
        {
            state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * ver + orientation.right * hor;

        if (OnSlope() && !exitingSlope)
        {
            rig.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rig.velocity.y > 0)
            {
                rig.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        if (grounded)
            rig.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if (!grounded)
            rig.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        rig.useGravity = !OnSlope();
    }


    private void GroundAddDrag()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, GroundMask);

        if (grounded)
            rig.drag = groundDrag;
        else
            rig.drag = 0;
    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (rig.velocity.magnitude > moveSpeed)
            {
                rig.velocity = rig.velocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rig.velocity.x, 0f, rig.velocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rig.velocity = new Vector3(limitedVel.x, rig.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        rig.velocity = new Vector3(rig.velocity.x, 0f, rig.velocity.z);
        rig.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        // 将向量投影到由法线定义的平面上（法线与该平面正交）
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
