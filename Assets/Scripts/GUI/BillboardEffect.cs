using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float referenceDistance = 10f;
    private float fadeStartDistance = 0f;
    private float fadeEndDistance = 1f;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private Vector3 baseOffset = Vector3.zero;

    private Transform parentTransform;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        fadeStartDistance = 30f;
        fadeEndDistance = 100f;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        parentTransform = transform.parent;
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || parentTransform == null)
        {
            return;
        }

        transform.position = parentTransform.position + baseOffset;

        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward, targetCamera.transform.rotation * Vector3.up);

        float depth = Vector3.Dot(transform.position - targetCamera.transform.position, targetCamera.transform.forward);
        float scaleFactor = depth / referenceDistance;

        transform.localScale = baseScale * scaleFactor;

        if (canvasGroup != null)
        {
            float distance = Vector3.Distance(targetCamera.transform.position, parentTransform.position);
            float alpha = 1f - Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, distance);

            canvasGroup.alpha = alpha;
        }
    }

    public void SetHeightOffset(float height)
    {
        baseOffset.y += height;
    }
}