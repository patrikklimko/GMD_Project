using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Health targetHealth;
    [SerializeField] private Image fillImage;

    private void Update()
    {
        if (targetHealth == null || fillImage == null) return;

        float ratio = (float)targetHealth.CurrentHp / targetHealth.MaxHp;
        fillImage.fillAmount = ratio;
    }
}