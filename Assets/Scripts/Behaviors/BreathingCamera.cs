using UnityEngine;

/// <summary>
/// Subtle idle camera movement.
/// </summary>
public class BreathingCamera : MonoBehaviour
{
    [SerializeField] private float positionAmplitude = 0.03f;
    [SerializeField] private float positionSpeed = 0.3f;
    [SerializeField] private float rotationAmplitude = 0.5f;
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float fovAmplitude = 0.3f;
    [SerializeField] private float fovSpeed = 0.15f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float initialFov;
    private float timeOffset;

    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        if (targetCamera != null)
        {
            initialFov = targetCamera.fieldOfView;
        }

        timeOffset = Random.value * 100f;
    }

    private void Update()
    {
        float time = Time.time + timeOffset;

        float x = Mathf.Sin(time * positionSpeed) * positionAmplitude;
        float y = Mathf.Sin(time * positionSpeed * 0.7f) * positionAmplitude;
        float z = Mathf.Sin(time * positionSpeed * 0.5f) * positionAmplitude * 0.5f;

        transform.localPosition = initialPosition + new Vector3(x, y, z);

        float pitch = Mathf.Sin(time * rotationSpeed) * rotationAmplitude;
        float yaw = Mathf.Sin(time * rotationSpeed * 0.7f) * rotationAmplitude;

        transform.localRotation =
            initialRotation *
            Quaternion.Euler(pitch, yaw, 0f);

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = initialFov + Mathf.Sin(time * fovSpeed) * fovAmplitude;
        }
    }
}