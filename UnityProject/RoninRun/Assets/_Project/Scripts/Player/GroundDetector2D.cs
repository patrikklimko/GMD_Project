using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GroundDetector2D : MonoBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.08f;

    private Collider2D playerCollider;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[4];

    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
    }

    private void CheckGrounded()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        int hitCount = playerCollider.Cast(Vector2.down, filter, hits, groundCheckDistance);

        IsGrounded = hitCount > 0;
    }
}