using UnityEngine;

/// <summary>
/// Billboard effect + scale that preserves vertical offset
/// </summary>
public class BillboardScaler : MonoBehaviour
{
    public Transform scaler;

    void LateUpdate()
    {
        Vector3 lossyscale = scaler.lossyScale;
        transform.localPosition = new Vector3(0, 2f / lossyscale.y, 0); // "inverse scale"
    }
}
