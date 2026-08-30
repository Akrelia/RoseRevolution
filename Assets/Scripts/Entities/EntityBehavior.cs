using RevolutionShared.Data;
using System.Collections;
using UnityEngine;

/// <summary>
/// Entity behavior.
/// </summary>
public class EntityBehavior : MonoBehaviour
{
    public IEntityMod mod;
    public EntityInfos infos;
    public EntityModelBehavior model;
    public Vector3 destinationPosition;

    public bool isWalking;
    Animator animator;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        destinationPosition = transform.position;

        animator = model.GetComponent<Animator>();
    }

    /// <summary>
    /// Load the infos.
    /// </summary>
    /// <param name="infos">Infos.</param>
    public void LoadBasicInfos(EntityInfos infos)
    {
        this.infos = infos;
    }

    /// <summary>
    /// Update the entity behavior.
    /// </summary>
    public void Update()
    {
        if (isWalking)
        {
            MoveToPosition();
        }
    }

    public void Die()
    {
        var waitTime = 0f;

        isWalking = false;

        if (model.data.animations[4] != null)
        {
            var animation = model.data.animations[4];

            animator.Play("Base Layer.Animation_4", 0);

            waitTime = animation.length;
        }

        StartCoroutine(DestroyAfterDeath(waitTime));
    }

    private IEnumerator DestroyAfterDeath(float duration)
    {
        yield return new WaitForSeconds(duration);

        Destroy(gameObject);
    }

    /// <summary>
    /// Set the destination position.
    /// </summary>
    /// <param name="position">Position.</param>
    public void SetDestination(WorldPosition position)
    {
        destinationPosition = position.ToVector3();

        if (model.data.animations[1] != null)
        {
            if (animator)
            {
                animator.Play("Base Layer.Animation_1", 0); // TODO : whole animation system
            }
        }

        isWalking = true;
    }

    /// <summary>
    /// Move to position.
    /// </summary>
    public void MoveToPosition()
    {
        if (Vector3.Distance(transform.position, destinationPosition) > 0.2f)
        {
            Vector3 playerToMouse = destinationPosition - transform.position;

            playerToMouse.y = 0;

            Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

            transform.rotation = newRotation;

            transform.position += transform.forward * (model.data.monsterData.moveSpeed / 100F) * Time.deltaTime;
        }

        else
        {
            isWalking = false;

            if (model.data.animations[0] != null)
            {
                animator.Play("Base Layer.Animation_0", 0);
            }
        }
    }
}
