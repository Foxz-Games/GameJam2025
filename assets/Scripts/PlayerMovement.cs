using System.Collections;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
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

	[Header("Dashing")]
	public float dashSpeed = 20f;
	public float dashDuration = 0.2f;
	public float dashCooldown = 0.1f;
	bool isDashing;
	bool canDash = true;
	TrailRenderer trailRenderer;

	[Header("Jumping")]
	public float jumpPower = 10f;
	public int maxJumps = 1;
	int jumpsRemaining;
	// private float jumpBuffer = 0.2f;
	// private float jumpBufferTimer = 0;
	// private float coyoteTime = 0.2f;
	// private float coyoteTimeTimer = 0;

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
	public float wallSlideAcceleration = 35f;
	bool isWallSliding = false;

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
		trailRenderer = GetComponent<TrailRenderer>();
	}

	// Update is called once per frame
	void Update()
	{
		animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));

		if (isDashing)
		{
			return;
		}
		GroundCheck();
		ProcessGravity();
		ProcessWallSlide();
		ProcessWallJump();

		if (!isWallJumping)
		{
			rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
			Flip();
		}
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
		if (isWallSliding)
		{
			rb.gravityScale = 0f;
			return;
		}

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
		bool touchingWall = !isGrounded && WallCheck() && horizontalMovement != 0;

		if (touchingWall)
		{
			if (!isWallSliding)
			{
				isWallSliding = true;
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
			}

			float newY = Mathf.MoveTowards(rb.linearVelocity.y, -maxFallSpeed, wallSlideAcceleration * Time.deltaTime);
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
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

	public void Move(InputAction.CallbackContext context)
	{
		horizontalMovement = context.ReadValue<Vector2>().x;
	}

	public void Dash(InputAction.CallbackContext ctx)
	{
		bool buttonPressed = ctx.performed;
		if (buttonPressed && canDash)
		{
			StartCoroutine(DashCoroutine());
		}
	}
	
	private IEnumerator DashCoroutine()
	{
		canDash = false;
		isDashing = true;

		trailRenderer.emitting = true;
		float dashDirection = isFacingRight ? 1f : -1f;
		rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);

		yield return new WaitForSeconds(dashDuration);

		rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

		isDashing = false;
		trailRenderer.emitting = false;

		yield return new WaitForSeconds(dashCooldown);
		canDash = true;
	}

	public void Jump(InputAction.CallbackContext ctx)
	{
		bool buttonPressed = ctx.performed;
		bool buttonReleased = ctx.canceled;
		if (jumpsRemaining > 0)
		{
			if (buttonPressed)
			{
				float jp = jumpPower;
				if (jumpsRemaining != maxJumps)
				{
					// make double jump shorter
					jp *= .85f;
				}
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, jp);
				jumpsRemaining--;
				JumpFX();
			}
			else if (buttonReleased)
			{
				rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.15f);
				jumpsRemaining--;
				JumpFX();
			}
		}

		// Wall jump
		if (buttonPressed && wallJumpTimer > 0f)
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
		//smokeFX.Play();
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
