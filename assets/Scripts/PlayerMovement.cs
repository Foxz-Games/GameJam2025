using System.Diagnostics;
using System.Dynamic;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
	public Rigidbody2D rb;
	public Animator animator;

	[Header("Movement")]
	public float moveSpeed = 5f;
	float horizontalMovement;
	bool isFacingRight = true;

	[Header("Jumping")]
	public float jumpPower = 10f;
	public int maxJumps = 1;
	int jumpsRemaining;

	[Header("GroundCheck")]
	public Transform groundCheckPos;
	public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
	public LayerMask groundLayer;
	bool isGrounded = false;

	[Header("WallCheck")]
	public Transform wallCheckPos;
	public Vector2 wallCheckSize = new Vector2(0.5f, 0.05f);
	public LayerMask wallLayer;

	[Header("WallMovement")]
	public float wallSlideSpeed = 2f;
	bool isWallSliding = false;

	bool isWallJumping;
	float wallJumpDirection;
	float wallJumpTime = 0.5f;
	float wallJumpTimer;
	public Vector2 wallJumpPower = new Vector2(5f, 10f);

	[Header("Gravity")]
	public float baseGravity = 2f;
	public float maxFallSpeed = 10f;
	public float fallSpeedMultiplier = 2f;

	void Start()
	{
		DoubleJumpItem.OnDoubleJumpCollect += _ => maxJumps = 2;
	}

	// Update is called once per frame
	void Update()
	{
		GroundCheck();
		ProcessGravity();
		ProcessWallSlide();
		ProcessWallJump();

		if (!isWallJumping)
		{
			rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
			Flip();
		}

		animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
	}

	public void Move(InputAction.CallbackContext context)
	{
		horizontalMovement = context.ReadValue<Vector2>().x;
	}

	private void Flip()
	{
		if (isFacingRight && rb.linearVelocity.x < 0 || !isFacingRight && rb.linearVelocity.x > 0)
		{
			_Flip();
		}
	}

	private void _Flip()
	{
		isFacingRight = !isFacingRight;
		Vector3 ls = transform.localScale;
		ls.x *= -1f;
		transform.localScale = ls;
	}

	private void ProcessGravity()
	{
		if (rb.linearVelocity.y < 0)
		{
			rb.gravityScale = baseGravity * fallSpeedMultiplier;
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
		}
		else
		{
			rb.gravityScale = baseGravity;
		}
	}

	private void ProcessWallSlide()
	{
		if (!isGrounded && WallCheck() && horizontalMovement != 0)
		{
			isWallSliding = true;
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
			UnityEngine.Debug.Log(isWallSliding);
		}
		else
		{
			isWallSliding = false;
		}
	}

	private void ProcessWallJump()
	{
		if (isWallSliding)
		{
			isWallJumping = false;
			wallJumpDirection = -transform.localScale.x;
			wallJumpTimer = wallJumpTime;

			CancelInvoke(nameof(CancelWallJump));
		}
		else if (wallJumpTimer > 0f)
		{
			wallJumpTimer += Time.deltaTime;
		}
	}

	private void CancelWallJump()
	{
		isWallJumping = false;
	}

	public void Jump(InputAction.CallbackContext context)
	{
		if (jumpsRemaining > 0)
		{
			if (context.performed)
			{
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
				jumpsRemaining--;
			}
			else if (context.canceled)
			{
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
				jumpsRemaining--;
			}
		}

		// Wall jump
		if (context.performed && wallJumpTimer > 0f)
		{
			isWallJumping = true;
			rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
			wallJumpTimer = 0;

			if (transform.localScale.x != wallJumpDirection)
			{
				_Flip();
			}

			Invoke(nameof(CancelWallJump), wallJumpTime + 0.1f);
		}
	}

	private bool WallCheck()
	{
		return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0, wallLayer);
	}

	private void GroundCheck()
	{
		if (Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer))
		{
			jumpsRemaining = maxJumps;
			isGrounded = true;
		}
		else
		{
			isGrounded = false;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);
	}
}
