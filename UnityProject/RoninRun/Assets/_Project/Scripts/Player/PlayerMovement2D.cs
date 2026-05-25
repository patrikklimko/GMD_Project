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

    [Header("Jump Feel")]
    [Tooltip("Seconds after leaving the ground where you can still trigger a normal jump.")]
    [SerializeField] private float coyoteTime = 0.10f;
    [Tooltip("Seconds before landing where a jump press still counts after touchdown.")]
    [SerializeField] private float jumpBufferTime = 0.10f;

    // Cached reference to the more accurate ground detector. If present, it
    // overrides the OverlapCircle fallback so movement and animator agree.
    private GroundDetector2D _groundDetector;
    private float _lastGroundedTime = -999f;
    private float _lastJumpPressedTime = -999f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Facing / Deadzone")]
    [SerializeField] private float moveDeadzone = 0.20f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private bool _movementLocked;
    private int _facing = 1;
    private int _jumpsRemaining;

    public float MoveX { get; private set; }
    public int FacingDir { get; private set; } = 1;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _groundDetector = GetComponent<GroundDetector2D>();
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
            jumpAction.action.Disable();
        }
    }

    private void Update()
    {
        HandleJumpInput();
    }

    private void FixedUpdate()
    {
        if (!_movementLocked)
        {
            _rb.linearVelocity = new Vector2(MoveX * moveSpeed, _rb.linearVelocity.y);
        }

        if (IsGrounded())
        {
            _jumpsRemaining = maxJumps;
            _lastGroundedTime = Time.time;
        }
    }

    private void HandleJumpInput()
    {
        if (jumpAction == null || jumpAction.action == null)
            return;

        // Record presses into a small buffer so a tap right BEFORE landing
        // still counts a frame or two after touchdown.
        if (jumpAction.action.WasPressedThisFrame())
        {
            _lastJumpPressedTime = Time.time;
        }

        bool jumpQueued = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;
        if (!jumpQueued)
            return;

        bool grounded = IsGrounded();
        bool inCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;

        // Ensure jump counter is correct in two cases the old code missed:
        //  - we just landed THIS frame and Update beat FixedUpdate to it
        //  - the OverlapCircle fallback flickers false even though
        //    GroundDetector2D agrees we're on ground (now also queried).
        if (grounded || inCoyote)
        {
            _jumpsRemaining = maxJumps;
        }

        if (_jumpsRemaining <= 0)
            return;

        // Consume the buffered press so we don't double-fire.
        _lastJumpPressedTime = -999f;
        PerformJump();
    }

    private void PerformJump()
    {
        float multiplier = (_jumpsRemaining == 1) ? secondJumpMultiplier : 1f;

        // Play sound immediately on accepted jump input.
        AudioManager.Instance?.PlaySfx(SfxId.Jump);

        // Apply jump immediately in the same frame.
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpForce * multiplier, ForceMode2D.Impulse);

        _jumpsRemaining--;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        float x = v.x;

        if (Mathf.Abs(x) < moveDeadzone)
        {
            x = 0f;
        }

        MoveX = x;

        if (MoveX > 0.01f)
        {
            _facing = 1;
        }
        else if (MoveX < -0.01f)
        {
            _facing = -1;
        }

        FacingDir = _facing;
        _sr.flipX = (_facing == -1);
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
        // Prefer the dedicated GroundDetector2D (collider-cast based) when it
        // exists — it agrees with the animator and doesn't suffer from the
        // OverlapCircle-misses-gap-between-tiles failure mode.
        if (_groundDetector != null && _groundDetector.IsGrounded)
            return true;

        // Fallback: the original OverlapCircle check, in case GroundDetector2D
        // is missing for some reason.
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}