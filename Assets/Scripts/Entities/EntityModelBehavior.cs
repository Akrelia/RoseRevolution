using UnityEngine;

/// <summary>
/// Entity Model Behavior.
/// </summary>
public class EntityModelBehavior : MonoBehaviour
{
    public EntitySO data;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        var animator = GetComponent<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        var controller = animator.runtimeAnimatorController;

        if (controller.animationClips.Length > 0)
        {
            animator.Play(0, 0, 0f);
        }
    }
}