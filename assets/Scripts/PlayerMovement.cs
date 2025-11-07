using System.Numerics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;

	[Header("Jumping")]
	public float jumpPower = 10f;
	public int maxJumps = 1;
	int jumpsRemaining;

	[Header("GroundCheck")]
	public Transform groundCheckPos;
	public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
	public LayerMask groundLayer;
	
	[Header("WallCheck")]
	public Transform wallCheckPos;
	public Vector2 wallCheckSize = new Vector2(0.5f, 0.05f);
	public LayerMask wallLayer;

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
        rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
		GroundCheck();
		Gravity();
    }

	public void Move(InputAction.CallbackContext context)
	{
		horizontalMovement = context.ReadValue<Vector2>().x;
	}
	
	private void Gravity()
	{
		if (rb.linearVelocity.y < 0)
		{
			rb.gravityScale = baseGravity * fallSpeedMultiplier;
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
		}
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
	}
	
	private void GroundCheck()
	{
		if (Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer))
		{
			jumpsRemaining = maxJumps;
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
