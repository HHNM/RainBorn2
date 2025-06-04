using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerSoulCorruption : MonoBehaviour
{
    [Header("Corruption Settings")]
    public float corruptionLevel = 0;
    public float maxCorruption = 100;

    [Header("Corruption Over Time")]
    public float baseCorruptionRate = 1f;
    public float currentCorruptionRate = 1f;

    [Header("Soul Effect")]
    public float soulCorruptionBoost = 5f;
    public float soulRateIncrease = 0.5f;

    private bool isCorruptionActive = false;
    private bool isDead = false;

    public Slider soulCorruptionBar;
    private float displayedCorruption = 0f;
    public float barLerpSpeed = 5f;

    public Image soulCorruptionFill;

    [Header("Beat Effect")]
    public Color baseColor = Color.red;
    public Color pulseColor = Color.white;
    public float beatSpeed = 2f;

    public bool isBeating = false; // New flag to control beat

    private void Start()
    {
        soulCorruptionBar.value = corruptionLevel;
        displayedCorruption = corruptionLevel;
        soulCorruptionBar.maxValue = maxCorruption;
    }

    void Update()
    {
        // Smooth slider
        displayedCorruption = Mathf.Lerp(displayedCorruption, corruptionLevel, Time.deltaTime * barLerpSpeed);
        soulCorruptionBar.value = displayedCorruption;

        // Beat ONLY when isBeating is true
        if (isBeating && soulCorruptionFill != null)
        {
            float t = (Mathf.Sin(Time.time * beatSpeed) + 1f) * 0.5f;
            soulCorruptionFill.color = Color.Lerp(baseColor, pulseColor, t);
        }
        else if (soulCorruptionFill != null)
        {
            if (soulCorruptionFill.color != baseColor)
                soulCorruptionFill.color = baseColor;
        }

        if (isDead || !isCorruptionActive) return;

        corruptionLevel += currentCorruptionRate * Time.deltaTime;
        corruptionLevel = Mathf.Min(corruptionLevel, maxCorruption);

        if (corruptionLevel >= maxCorruption)
        {
            Die();
        }
    }

    public void DropSoul(float soulAmount, bool activated)
    {
        if (activated)
        {
            activated = false;

            if (!isCorruptionActive)
            {
                currentCorruptionRate = baseCorruptionRate;
            }

            StartCoroutine(ReduceCorruptionGradually(soulAmount)); // isBeating is set inside coroutine
            currentCorruptionRate = Mathf.Max(0, currentCorruptionRate - soulRateIncrease);

            Debug.Log("[Corruption System] Soul dropped! Reducing corruption gradually...");
        }
    }

    public void CollectSoul(float soulAmount)
    {
        if (!isCorruptionActive)
        {
            isCorruptionActive = true;
            currentCorruptionRate = baseCorruptionRate;
            corruptionLevel += soulAmount;
            corruptionLevel = Mathf.Min(corruptionLevel, maxCorruption);
            currentCorruptionRate += soulRateIncrease;

            Debug.Log("[Corruption System] Soul absorbed! Corruption has begun...");
        }

        Debug.Log($"[Soul Absorbed] Gained {soulCorruptionBoost} corruption. Rate now: {currentCorruptionRate}/sec");
    }

    public void StopCorruption()
    {
        isCorruptionActive = false;
        corruptionLevel = 0f;

        Debug.Log("[Corruption System] Corruption stopped and reset at resting point.");
    }

    void Die()
    {
        isDead = true;
        Debug.Log("[Soul Corruption] Corruption maxed out! Player has died.");
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("dark"))
        {
            CollectSoul(10);
            isCorruptionActive = false;
            other.gameObject.SetActive(false);
        }
    }

    private IEnumerator ReduceCorruptionGradually(float amount)
    {
        isBeating = true; // START beat effect
        float step = 0.01f;
        float delay = 100f; // shortened delay for real-time feel

        float totalReduced = 0f;

        while (totalReduced < amount && corruptionLevel > 0f)
        {
            float reduceThisTick = Mathf.Min(step, amount - totalReduced);

            corruptionLevel -= reduceThisTick;
            corruptionLevel = Mathf.Max(corruptionLevel, 0f);

            totalReduced += reduceThisTick;

            yield return new WaitForSeconds(delay);
        }

        isBeating = false; // STOP beat effect
        Debug.Log($"[Corruption Reduced] Total reduced: {totalReduced}");
    }
}
