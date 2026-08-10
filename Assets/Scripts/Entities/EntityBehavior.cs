using RevolutionShared.Data;
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

    bool isWalking;
    Animator animator;

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

    /// <summary>
    /// Set the destination position.
    /// </summary>
    /// <param name="position">Position.</param>
    public void SetDestination(WorldPosition position)
    {
        destinationPosition = position.ToVector3();

        if (model.data.animations[1] != null)
            animator.Play("Base Layer.Animation_1", 0);

        isWalking = true;
    }

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
            animator.Play("Base Layer.Animation_0", 0);
        }
    }
}
