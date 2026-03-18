// PlayerAnimatorController.cs
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Tuning")]
    [SerializeField] private float speedMultiplier = 1f;   // keep 1 unless you want to exaggerate
    [SerializeField] private float yVelDeadzone = 0.01f;   // helps avoid jitter around 0

    private Animator _anim;
    private Rigidbody2D _rb;
    private PlayerMovement2D _move;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _move = GetComponent<PlayerMovement2D>();
    }

    private void Update()
    {
        if (groundCheck == null)
        {
            Debug.LogError("PlayerAnimatorController: GroundCheck not assigned.");
            enabled = false;
            return;
        }

        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        float yVel = _rb.linearVelocity.y;
        if (Mathf.Abs(yVel) < yVelDeadzone) yVel = 0f;

        // Use INPUT-based speed to avoid physics drift causing Run while idle
        float speed = Mathf.Abs(_move.MoveX) * speedMultiplier;

        _anim.SetBool("IsGrounded", grounded);
        _anim.SetFloat("YVelocity", yVel);
        _anim.SetFloat("Speed", speed);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}