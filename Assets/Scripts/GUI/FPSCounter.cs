using UnityEngine;
using TMPro; // Si tu utilises TextMeshPro (recommandé)
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;     // Mise à jour toutes les 0.5s (plus lisible)
    [SerializeField] private int targetFrameRate = 0;         // 0 = unlimited

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textMeshPro;     // Priorité TMP

    private float accum = 0f;
    private int frames = 0;
    private float timeLeft;
    private float currentFPS = 0f;

    private void Start()
    {
        timeLeft = updateInterval;

        // Optionnel : limiter le framerate pour tester
        if (targetFrameRate > 0)
            Application.targetFrameRate = targetFrameRate;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if (timeLeft <= 0f)
        {
            currentFPS = accum / frames;

            // Mise à jour du texte
            string fpsText = Mathf.RoundToInt(currentFPS).ToString();

            if (textMeshPro != null)
                textMeshPro.text = $"FPS : {fpsText}";

            // Reset
            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    // Optionnel : méthode publique pour récupérer le FPS actuel
    public float GetCurrentFPS() => currentFPS;
}