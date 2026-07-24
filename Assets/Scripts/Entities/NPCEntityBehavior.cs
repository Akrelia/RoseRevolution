using UnityEngine;

/// <summary>
/// NPC Behavior.
/// </summary>
public class NPCEntityBehavior : MonoBehaviour
{
    public NPCEntitySO data;

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