using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("UI References")]
    public RectTransform mainBar;        // Main (actual health) bar
    public RectTransform backgroundBar;  // Delayed trail bar
    public Image backgroundImage;

    [Header("Speeds")]
    public float mainBarSpeed = 10f;
    public float backgroundSpeed = 2f;

    private float currentMainValue;
    private float currentBackgroundValue;

    public Color healcolor;
    public Color damagecolor;

    private bool isHealing = false;


    void Start()
    {
        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
        currentMainValue = healthRatio;
        currentBackgroundValue = healthRatio;

        UpdateBarScales();
        
    }

    void Update()
    {
        float targetHealthRatio = Mathf.Clamp01(currentHealth / maxHealth);

        if (targetHealthRatio < currentMainValue)
        {
            // Damage: main drops fast, red follows
            isHealing = false;

            currentMainValue = Mathf.Lerp(currentMainValue, targetHealthRatio, Time.deltaTime * mainBarSpeed);
            currentBackgroundValue = Mathf.Lerp(currentBackgroundValue, targetHealthRatio, Time.deltaTime * backgroundSpeed);
            backgroundImage.color = damagecolor;
        }
        else if (targetHealthRatio > currentMainValue)
        {
            // Healing: green bar jumps first, main bar catches up
            isHealing = true;

            currentBackgroundValue = Mathf.Lerp(currentBackgroundValue, targetHealthRatio, Time.deltaTime * mainBarSpeed); // fill quickly
            currentMainValue = Mathf.Lerp(currentMainValue, targetHealthRatio, Time.deltaTime * backgroundSpeed); // catch up slowly
            backgroundImage.color = healcolor;
        }

        UpdateBarScales();


        if (Input.GetKeyDown(KeyCode.X)) TakeDamage(5);
        if (Input.GetKeyDown(KeyCode.H) && currentHealth < maxHealth) { 
            Heal(10);
            GetComponent<PlayerSunlightEnergy>().UseSunlight(10);
        }
    }

    void UpdateBarScales()
    {
        mainBar.localScale = new Vector3(currentMainValue, 1f, 1f);
        backgroundBar.localScale = new Vector3(currentBackgroundValue, 1f, 1f);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }
}

