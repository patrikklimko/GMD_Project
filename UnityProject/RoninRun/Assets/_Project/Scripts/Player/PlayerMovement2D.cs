// PlayerMovement2D.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private int maxJumps = 2;                 // 2 = double jump
    [SerializeField] private float secondJumpMultiplier = 0.9f; // optional: make 2nd jump slightly weaker

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Facing / Deadzone")]
    [SerializeField] private float moveDeadzone = 0.20f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private bool _jumpPressed;
    private int _facing = 1; // 1 = right, -1 = left
    private int _jumpsRemaining;

    // Exposed for Animator (prevents drift triggering Run)
    public float MoveX { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _jumpsRemaining = maxJumps;
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
            moveAction.action.performed += OnMove;
            moveAction.action.canceled += OnMove;
        }

        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= OnMove;
            moveAction.action.canceled -= OnMove;
            moveAction.action.Disable();
        }

        if (jumpAction != null)
        {
            jumpAction.action.performed -= OnJump;
            jumpAction.action.Disable();
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        float x = v.x;

        // Deadzone to kill drift
        if (Mathf.Abs(x) < moveDeadzone) x = 0f;

        MoveX = x;

        // Facing
        if (MoveX > 0.01f) _facing = 1;
        else if (MoveX < -0.01f) _facing = -1;

        _sr.flipX = (_facing == -1);
    }

    private void FixedUpdate()
    {
        // Move
        _rb.linearVelocity = new Vector2(MoveX * moveSpeed, _rb.linearVelocity.y);

        // Reset jumps when grounded
        if (IsGrounded())
        {
            _jumpsRemaining = maxJumps;
        }

        // Jump (supports double jump)
        if (_jumpPressed && _jumpsRemaining > 0)
        {
            float multiplier = (_jumpsRemaining == 1) ? secondJumpMultiplier : 1f;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            _rb.AddForce(Vector2.up * jumpForce * multiplier, ForceMode2D.Impulse);

            _jumpsRemaining--;
        }

        _jumpPressed = false;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        _jumpPressed = true;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}