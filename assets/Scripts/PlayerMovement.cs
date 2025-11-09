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
	public ParticleSystem smokeFX;

	[Header("Movement")]
	public float moveSpeed = 8f;
	float horizontalMovement;
	bool isFacingRight = true;

	[Header("Jumping")]
	public float jumpPower = 10f;
	public int maxJumps = 1;
	int jumpsRemaining;
	public float jumpBuffer = 0.25f;

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
	float wallSlideDuration = 2f; //TODO: implement

	bool isWallJumping;
	float wallJumpDirection;
	float wallJumpDuration = 0.1f;
	float wallJumpCoyote = 0.25f;
	float wallJumpTimer;
	public Vector2 wallJumpPower = new Vector2(8f, 10f);

	[Header("Gravity")]
	public float baseGravity = 1.5f;
	public float maxFallSpeed = 12f;
	public float fallSpeedMultiplier = 2.5f;

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

		if (rb.linearVelocity.y == 0)
		{
			smokeFX.Play();
		}
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
			wallJumpTimer = wallJumpCoyote;

			CancelInvoke(nameof(CancelWallJump));
		}
		else if (wallJumpTimer > 0f)
		{
			wallJumpTimer = Mathf.Max(0f, wallJumpTimer - Time.deltaTime);	
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
				float jp = jumpPower;
				if (jumpsRemaining != maxJumps)
				{
					jp *= .85f;
				}
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, jp);
				jumpsRemaining--;
				JumpFX();
			}
			else if (context.canceled)
			{
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
				jumpsRemaining--;
				JumpFX();
			}
		}

		// Wall jump
		if (context.performed && wallJumpTimer > 0f)
		{
			isWallJumping = true;
			rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);

			wallJumpTimer = 0f;
			JumpFX();

			if (transform.localScale.x != wallJumpDirection)
			{
				_Flip();
			}

			// cancel wallJump state after set time
			Invoke(nameof(CancelWallJump), wallJumpDuration + 0.1f);
		}
	}
	
	private void JumpFX()
	{
		smokeFX.Play();
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
