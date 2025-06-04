using UnityEngine;
using UnityEngine.UI;

public class PlayerSunlightEnergy : MonoBehaviour
{
    [Header("Sunlight Settings")]
    public float currentSunlight = 100f;
    public float maxSunlight = 100f;

    [Header("Sunlight Regen")]
    public float regenRate = 5f;
    public bool isInSunlight = false;

    [Header("UI")]
    public Slider sunlightSlider;         // Main bar
    public Slider trailSlider;            // Trail bar
    public float barLerpSpeed = 5f;
    public float trailLerpSpeed = 2f;

    private float displayedSunlight = 0f;
    private float displayedTrail = 0f;

    [Header("Beat Effect")]
    public Image sunlightFill;            // Assign this in the Inspector (Fill image of sunlightSlider)
    public Color baseColor = Color.yellow;
    public Color pulseColor = Color.white;
    public float beatSpeed = 2f;

    [Header("Sun Icon Rotation Pulse")]
    public RectTransform sunIcon; // Assign in Inspector
    public float rotationAmplitude = 15f; // Max angle (e.g. 15 degrees left/right)
    public float pulseSpeed = 2f;         // How fast it swings (Hz)
    private float baseRotationZ = 0f;


    private void Start()
    {
        sunlightSlider.maxValue = maxSunlight;
        trailSlider.maxValue = maxSunlight;

        displayedSunlight = currentSunlight;
        displayedTrail = currentSunlight;

        sunlightSlider.value = displayedSunlight;
        trailSlider.value = displayedTrail;

        if (sunIcon != null)
            baseRotationZ = sunIcon.localEulerAngles.z;

    }

    private void Update()
    {
        // Regenerate if in sunlight
        if (isInSunlight)
        {
            currentSunlight += regenRate * Time.deltaTime;
            currentSunlight = Mathf.Clamp(currentSunlight, 0, maxSunlight);
            GetComponent<PlayerSoulCorruption>().DropSoul(1, isInSunlight);

            // Beat effect while in sunlight
            if (sunlightFill != null)
            {
                float t = (Mathf.Sin(Time.time * beatSpeed) + 1f) * 0.5f;
                sunlightFill.color = Color.Lerp(baseColor, pulseColor, t);
            }

            // Oscillate sun icon like a heartbeat pulse
            if (sunIcon != null)
            {
                float angle = Mathf.Sin(Time.time * pulseSpeed) * rotationAmplitude;
                sunIcon.localEulerAngles = new Vector3(0f, 0f, baseRotationZ + angle);
            }

        }
        else if (sunlightFill != null && sunlightFill.color != baseColor)
        {
            // Reset the fill color when not in sunlight
            sunlightFill.color = baseColor;
        }

        // Update main bar
        displayedSunlight = Mathf.Lerp(displayedSunlight, currentSunlight, Time.deltaTime * barLerpSpeed);
        sunlightSlider.value = displayedSunlight;

        // Trail logic
        if (displayedTrail > currentSunlight)
        {
            displayedTrail = Mathf.Lerp(displayedTrail, currentSunlight, Time.deltaTime * trailLerpSpeed);
        }
        else if (displayedTrail < currentSunlight)
        {
            displayedTrail = Mathf.Lerp(displayedTrail, currentSunlight, Time.deltaTime * barLerpSpeed * 1.2f);
        }

        trailSlider.value = displayedTrail;



    }

    public void UseSunlight(float amount)
    {
        currentSunlight -= amount;
        currentSunlight = Mathf.Clamp(currentSunlight, 0, maxSunlight);
        Debug.Log($"[Sunlight Used] -{amount}. Current: {currentSunlight:F2}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("sunarea"))
        {
            isInSunlight = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("sunarea"))
        {
            isInSunlight = false;
            GetComponent<PlayerSoulCorruption>().isBeating = false;

            if (sunIcon != null)
            {
                sunIcon.localEulerAngles = new Vector3(0f, 0f, baseRotationZ);
            }

        }
    }
}
