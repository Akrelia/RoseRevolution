using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; 
    [SerializeField] private float referenceDistance = 10f; 
    [SerializeField] private Vector3 baseScale = Vector3.one; 
    [SerializeField] private Vector3 baseOffset = Vector3.zero; 

    private Transform parentTransform;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        parentTransform = transform.parent;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || parentTransform == null) return;

        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,targetCamera.transform.rotation * Vector3.up);

        float distance = Vector3.Distance(transform.position, targetCamera.transform.position);

        float scaleFactor = distance / referenceDistance;
        transform.localScale = baseScale * scaleFactor;

        transform.position = parentTransform.position + baseOffset;
    }

    public void SetHeightOffset(float height)
    {
        baseOffset.y += height;
    }
}