using UnityEngine;

/// <summary>
/// Entity Model Behavior.
/// </summary>
public class EntityModelBehavior : MonoBehaviour
{
    public EntitySO data;
    public float heightOffset = 0f;
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

        var controller = animator.runtimeAnimatorController;

        if (controller.animationClips.Length > 0)
        {
            animator.Play(0, 0, Random.value);
        }
    }

    private void LateUpdate()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 100f, Vector3.down, out var hit, 200f, groundMask))
        {
            var position = transform.position;
            position.y = hit.point.y + heightOffset;
            transform.position = position;
        }
    }
}