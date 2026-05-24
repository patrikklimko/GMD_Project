using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement2D))]
[RequireComponent(typeof(GroundDetector2D))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float yVelDeadzone = 0.01f;

    private Animator _anim;
    private Rigidbody2D _rb;
    private PlayerMovement2D _move;
    private GroundDetector2D _groundDetector;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _move = GetComponent<PlayerMovement2D>();
        _groundDetector = GetComponent<GroundDetector2D>();
    }

    private void Update()
    {
        bool grounded = _groundDetector.IsGrounded;

        float yVel = _rb.linearVelocity.y;

        if (Mathf.Abs(yVel) < yVelDeadzone)
        {
            yVel = 0f;
        }

        float speed = Mathf.Abs(_move.MoveX) * speedMultiplier;

        _anim.SetBool("IsGrounded", grounded);
        _anim.SetFloat("YVelocity", yVel);
        _anim.SetFloat("Speed", speed);
    }
}