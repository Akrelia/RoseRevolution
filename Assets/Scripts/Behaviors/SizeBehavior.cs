using UnityEngine;

/// <summary>
/// Scale behavior.
/// </summary>
public class ScaleBehavior : MonoBehaviour
{
    [Header("Values")]
    public bool randomizeStart;
    public float minScale;
    public float maxScale;
    public float cycleTime;

    private float time;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        time = randomizeStart ? Random.Range(0f, cycleTime) : 0f;
    }

    /// <summary>
    /// Update.
    /// </summary>
    private void Update()
    {
        if (cycleTime <= 0f)
        {
            transform.localScale = Vector3.one * maxScale;

            return;
        }

        time += Time.deltaTime;

        float t = Mathf.PingPong(time / cycleTime, 1f);
        float scale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = Vector3.one * scale;
    }
}
