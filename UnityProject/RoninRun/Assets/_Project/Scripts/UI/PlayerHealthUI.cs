using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    private void Update()
    {
        if (playerHealth != null && healthText != null)
        {
            healthText.text = "HP: " + playerHealth.GetHp() + "/" + playerHealth.GetMaxHp();
        }
    }
}