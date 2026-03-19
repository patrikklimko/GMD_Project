using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D _rb;
    private int _dir = 1; // 1 = right, -1 = left

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (leftPoint == null || rightPoint == null)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        _rb.linearVelocity = new Vector2(_dir * moveSpeed, _rb.linearVelocity.y);

        if (_dir > 0 && transform.position.x >= rightPoint.position.x)
            _dir = -1;
        else if (_dir < 0 && transform.position.x <= leftPoint.position.x)
            _dir = 1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = (_dir < 0);
    }
}