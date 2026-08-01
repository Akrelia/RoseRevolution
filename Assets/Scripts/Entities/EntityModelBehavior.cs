using UnityEngine;

/// <summary>
/// Entity Model Behavior.
/// </summary>
public class EntityModelBehavior : MonoBehaviour
{
    public EntitySO data;
    public float heightOffset = 0f;
    public LayerMask groundMask;

    private Transform entityTransform;

    private void Start()
    {
        groundMask = LayerMask.GetMask("Floor");
        entityTransform = transform.parent;

        var animator = GetComponent<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        if (animator.runtimeAnimatorController.animationClips.Length > 0)
            animator.Play(0, 0, Random.value);
    }

    private void FixedUpdate()
    {
        if (!Physics.Raycast(transform.position + Vector3.up * 100f, Vector3.down, out var hit, 200f, groundMask))
            return;

        float targetY = hit.point.y + heightOffset;

        if (Mathf.Abs(transform.position.y - targetY) < 0.001f)
            return;

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }
}