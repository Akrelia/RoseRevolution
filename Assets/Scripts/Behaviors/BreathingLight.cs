using UnityEngine;

/// <summary>
/// Makes a light smoothly breathe by varying its intensity.
/// </summary>
public class BreathingLight : MonoBehaviour
{
    [SerializeField] private Light targetLight;
    [SerializeField] private float minIntensity = 0.8f;
    [SerializeField] private float maxIntensity = 1.2f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float noiseStrength = 0f;
    [SerializeField] private float noiseSpeed = 0.5f;

    private float timeOffset;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        timeOffset = Random.value * 100f;
    }

    private void Update()
    {
        if (targetLight == null)
        {
            return;
        }

        float wave = (Mathf.Sin((Time.time + timeOffset) * speed) + 1f) * 0.5f;

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);

        if (noiseStrength > 0f)
        {
            float noise = Mathf.PerlinNoise(
                timeOffset,
                Time.time * noiseSpeed);

            noise = (noise - 0.5f) * 2f;

            intensity += noise * noiseStrength;
        }

        targetLight.intensity = Mathf.Max(0f, intensity);
    }
}