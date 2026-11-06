using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    public Controller controller;
    private float maxHealth;
    private Camera cam;

    public void Start()
    {
        cam = Camera.main;
        if (controller != null)
        {
            maxHealth = controller.entity.stats.MaxHealth;
            controller.entity.OnHealthChanged += UpdateHealthBar;
        }
        else
        {
            
        }
    }

    public void UpdateHealthBar(float currentHealth)
    {
        foregroundImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.entity.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void LateUpdate()
    {
        transform.rotation = cam.transform.rotation;
    }
}
