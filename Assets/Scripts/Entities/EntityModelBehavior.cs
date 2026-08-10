using UnityEngine;

/// <summary>
/// Entity Model Behavior.
/// </summary>
public class EntityModelBehavior : MonoBehaviour
{
    public float heightOffset = 0f;
    public EntitySO data;
    public LayerMask groundMask;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        groundMask = LayerMask.GetMask("Floor");

        var animator = GetComponent<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (animator.runtimeAnimatorController.animationClips.Length > 0)
        {
            animator.Play(0, 0, Random.value);
        }
    }

    /// <summary>
    /// Fixed update.
    /// </summary>
    private void FixedUpdate()
    {
        if (!Physics.Raycast(transform.position + Vector3.up * 100f, Vector3.down, out var hit, 200f, groundMask))
        {
            return;
        }

        float targetY = hit.point.y + heightOffset;

        if (Mathf.Abs(transform.position.y - targetY) < 0.001f)
        {
            return;
        }

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }
}