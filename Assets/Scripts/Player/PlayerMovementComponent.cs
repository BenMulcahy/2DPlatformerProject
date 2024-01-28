using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementComponent : MonoBehaviour
{
    public Rigidbody2D RB { get; private set; }
    public bool bIsFacingRight { get; private set;}

    [Header("Movement | Walk/Run")]
    [SerializeField] float _walkSpeed = 15.0f;
    [SerializeField] float _sprintSpeed = 20.0f;
    public bool bIsSprinting { get; private set; }


    [Header("Movement | Jumping")]
    [SerializeField] float _jumpForce = 5.0f;
    [SerializeField] float _coyoteTime = 0.25f;
    public bool bCanJump { get; private set; }
    public bool bIsOnFloor { get; private set; }
    public int maxJumps = 1;
    int JumpCounter;

    [Header("GroundCheck")]
    [SerializeField] LayerMask _groundCheckMask;

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        bCanJump = true;
    }

    private void Start()
    {
        StartCoroutine(GroundCheck());
    }

    private void FixedUpdate()
    {
        RB.velocity = new Vector2(Player.Instance.InputActions.Gameplay.Movement.ReadValue<float>() * (bIsSprinting ? _sprintSpeed : _walkSpeed),RB.velocityY);
        if (RB.velocity.x < 0) bIsFacingRight = false;
        else if(RB.velocity.x > 0) bIsFacingRight = true; //Use elif to prevent setting bool when vel = 0
    }

    public void DoJump()
    {
        //TODO Held jump duration logic

        JumpCounter++;
        if (JumpCounter >= maxJumps) bCanJump = false;
        RB.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        //Debug.Log("Do Jump!");
    }

    public void SetSprinting(bool bSprinting)
    {
        bIsSprinting = bSprinting;
    }

    private void OnLand()
    {
        Debug.Log("Player Landed!");

        //Reset Values on landing
        bIsOnFloor = true;
        JumpCounter = 0;
        bCanJump = true;
    }

    private bool RaycastToGround(bool castLeftEdge)
    {
        Bounds playerBounds = Player.Instance.GetComponent<Collider2D>().bounds;
        Vector2 rPos = new Vector2(playerBounds.max.x, playerBounds.min.y);
        Vector2 lPos = playerBounds.min;

        //Debug.DrawLine(castLeftEdge? lPos : rPos, castLeftEdge ? lPos : rPos + Vector2.down * 0.3f, Color.blue);
        if (Physics2D.Linecast(castLeftEdge? lPos : rPos, castLeftEdge ? lPos : rPos + Vector2.down * 0.2f, _groundCheckMask))
        {
            if(!bIsOnFloor) OnLand();
            return true;
        }
        else return false;
    }
    
    public IEnumerator GroundCheck()
    {
        while (true)
        {
            //Edge Detect L
            if (RaycastToGround(true))
            {
                yield return new WaitForFixedUpdate();
            }
            //Edge detect R
            else if (RaycastToGround(false))
            {
                yield return new WaitForFixedUpdate();
            }
            else
            {
                //if not on floor
                yield return new WaitForSeconds(_coyoteTime);
                bIsOnFloor = false;
            }
        }
    }
}
