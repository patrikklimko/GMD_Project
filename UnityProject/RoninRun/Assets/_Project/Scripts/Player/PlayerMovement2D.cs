using System.Collections;
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
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float secondJumpMultiplier = 0.9f;

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
    private bool _movementLocked;
    private int _facing = 1;
    private int _jumpsRemaining;

    public float MoveX { get; private set; }
    public int FacingDir { get; private set; } = 1;

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

        if (Mathf.Abs(x) < moveDeadzone)
            x = 0f;

        MoveX = x;

        if (MoveX > 0.01f)
            _facing = 1;
        else if (MoveX < -0.01f)
            _facing = -1;

        FacingDir = _facing;
        _sr.flipX = (_facing == -1);
    }

    private void FixedUpdate()
    {
        // Only apply normal movement if not temporarily locked by knockback
        if (!_movementLocked)
        {
            _rb.linearVelocity = new Vector2(MoveX * moveSpeed, _rb.linearVelocity.y);
        }

        if (IsGrounded())
        {
            _jumpsRemaining = maxJumps;
        }

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

    public void LockMovement(float duration)
    {
        StartCoroutine(LockMovementRoutine(duration));
    }

    private IEnumerator LockMovementRoutine(float duration)
    {
        _movementLocked = true;
        yield return new WaitForSeconds(duration);
        _movementLocked = false;
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