using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeathController : MonoBehaviour
{
    public static bool IsPlayerDead { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject deathPanel;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Die";

    [Header("Disable On Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D playerCollider;

    [Header("Restart")]
    [SerializeField] private float showDeathPanelDelay = 0.6f;

    private bool isDead;
    private bool canRestart;
    private RigidbodyConstraints2D originalConstraints;

    private void Awake()
    {
        IsPlayerDead = false;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();

        if (rb != null)
            originalConstraints = rb.constraints;

        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isDead || !canRestart)
            return;

        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
    }

public void Die()
{
    if (isDead)
        return;

    isDead = true;
    IsPlayerDead = true;
    canRestart = false;

    AudioManager.Instance?.PlaySfx(SfxId.PlayerDeath);

    DisableControlScripts();
    StopPlayerPhysics();
    TriggerDeathAnimation();

    Invoke(nameof(ShowDeathPanel), showDeathPanelDelay);
}

    private void DisableControlScripts()
    {
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }
    }

    private void StopPlayerPhysics()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Do NOT change bodyType to Static.
            // That causes "Cannot use linearVelocity on a static body"
            // if another movement script still runs.
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }
    }

    private void TriggerDeathAnimation()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger(deathTriggerName);
        }
    }

private void ShowDeathPanel()
{
    Debug.Log("Trying to show death panel...");

    if (deathPanel == null)
    {
        Debug.LogError("DeathPanel is NOT assigned in PlayerDeathController.");
        canRestart = true;
        return;
    }

    deathPanel.SetActive(true);
    deathPanel.transform.SetAsLastSibling();

    Debug.Log("DeathPanel activeSelf: " + deathPanel.activeSelf);
    Debug.Log("DeathPanel activeInHierarchy: " + deathPanel.activeInHierarchy);

    canRestart = true;
}

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        IsPlayerDead = false;

        if (rb != null)
        {
            rb.constraints = originalConstraints;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}