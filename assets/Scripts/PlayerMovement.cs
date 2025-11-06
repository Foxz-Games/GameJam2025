using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

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

	void Start()
	{
		DoubleJumpItem.OnDoubleJumpCollect += _ => maxJumps = 2;
	}

	// Update is called once per frame
	void Update()
    {
        rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
		GroundCheck();
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
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
	}
}
