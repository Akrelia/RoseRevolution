using UnityEngine;

public class BillboardFacing : MonoBehaviour
{
    public Transform targetCamera;

    void Start()
    {
        if (!targetCamera)
            targetCamera = Camera.main.transform;
    }

    void LateUpdate()
    {
        Vector3 dir = transform.position - targetCamera.position;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
