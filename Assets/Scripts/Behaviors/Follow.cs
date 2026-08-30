using UnityEngine;

/// <summary>
/// Follow.
/// </summary>
public class Follow : MonoBehaviour
{
    public Vector3 offset;
    public GameObject target;

    /// <summary>
    /// Update.
    /// </summary>
    private void Update()
    {
        if (target)
        {
            gameObject.transform.position = target.transform.position + offset;
        }
    }
}
