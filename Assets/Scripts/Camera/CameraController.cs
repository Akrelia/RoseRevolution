using UnityEngine;

/// <summary>
/// Camera controller.
/// </summary>
public class CameraController : MonoBehaviour
{
    public GameObject target;                           // Target to follow
    public float targetHeight = 1.7f;                         // Vertical offset adjustment
    public float distance = 12.0f;                            // Default Distance
    public float offsetFromWall = 2.0f;                       // Bring camera away from any colliding objects
    public float maxDistance = 30f;                       // Maximum zoom Distance
    public float minDistance = 2.0f;                      // Minimum zoom Distance
    public float xSpeed = 200.0f;                             // Orbit speed (Left/Right)
    public float ySpeed = 200.0f;                             // Orbit speed (Up/Down)
    public float yMinLimit = -80f;                            // Looking up limit
    public float yMaxLimit = 80f;                             // Looking down limit
    public float zoomRate = 40f;                          // Zoom Speed
    public float rotationDampening = 3.0f;                // Auto Rotation speed (higher = faster)
    public float zoomDampening = 3.0f;                    // Auto Zoom speed (Higher = faster)
    public LayerMask collisionLayers = -1;     // What the camera will collide with
    public bool lockToRearOfTarget = false;             // Lock camera to rear of target
    public bool allowMouseInputX = true;                // Allow player to control camera angle on the X axis (Left/Right)
    public bool allowMouseInputY = true;                // Allow player to control camera angle on the Y axis (Up/Down)

    public float settleTimeMs = 200f;

    private const float EpsilonYaw = 0.05f;
    private const float EpsilonPitch = 0.05f;
    private const float EpsilonDistance = 0.05f;

    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;
    private float currentDistance;
    private float desiredDistance;
    private float correctedDistance;
    private bool rotateBehind = false;
    private bool cameraDirty = true;
    private float pbuffer = 0.0f;       //Cooldownpuffer for SideButtons
    private float coolDown = 0.5f;      //Cooldowntime for SideButtons 
    private Vector2 lastMousePosition;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        targetYaw = currentYaw = angles.y;
        targetPitch = currentPitch = NormalizePitch(angles.x);

        currentDistance = distance;
        desiredDistance = distance;
        correctedDistance = distance;

        // Make the rigid body not change rotation
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody)
            rigidbody.freezeRotation = true;

        if (lockToRearOfTarget)
            rotateBehind = true;
    }

    void Update()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player") as GameObject;
            // Debug.Log("Looking for Player");
        }

    }

    //Only Move camera after everything else has been updated
    void LateUpdate()
    {
        // Don't do anything if target is not defined
        if (target == null)
            return;
        //pushbuffer
        if (pbuffer > 0)
            pbuffer -= Time.deltaTime;
        if (pbuffer < 0) pbuffer = 0;

        HandleOrbitInput();

        targetPitch = ClampAngle(targetPitch, yMinLimit, yMaxLimit);

        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        correctedDistance = desiredDistance;

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 targetOffset = new Vector3(0f, -targetHeight, 0f);
        Vector3 trueTargetPosition = new Vector3(
            target.transform.position.x,
            target.transform.position.y + targetHeight,
            target.transform.position.z);

        Vector3 desiredPosition = target.transform.position - (rotation * Vector3.forward * correctedDistance + targetOffset);

        bool isCorrected = false;
        if (Physics.Linecast(trueTargetPosition, desiredPosition, out RaycastHit collisionHit, collisionLayers))
        {
            correctedDistance = Vector3.Distance(trueTargetPosition, collisionHit.point) - offsetFromWall;
            isCorrected = true;
        }

        correctedDistance = Mathf.Clamp(correctedDistance, minDistance, maxDistance);

        if (cameraDirty || !IsSettled(isCorrected))
        {
            float timeWeight = GetRoseTimeWeight();

            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, timeWeight);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, timeWeight);

            // snap distance on collision; otherwise eases like yaw/pitch
            if (isCorrected)
                currentDistance = correctedDistance;
            else
                currentDistance += timeWeight * (correctedDistance - currentDistance);

            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            if (IsSettled(isCorrected))
                cameraDirty = false;
        }
        else if (isCorrected)
        {
            currentDistance = correctedDistance;
        }

        rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 position = target.transform.position - (rotation * Vector3.forward * currentDistance + targetOffset);

        transform.rotation = rotation;
        transform.position = position;
    }

    void HandleOrbitInput()
    {
        // Calculate the desired distance
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    Touch touch = Input.GetTouch(0);
                    ApplyYawDelta(touch.deltaPosition.x);
                    ApplyPitchDelta(-touch.deltaPosition.y);
                }

                if ((Input.touchCount == 2) && ((Input.GetTouch(0).phase == TouchPhase.Moved) || (Input.GetTouch(1).phase == TouchPhase.Moved)))
                {
                    // Store both touches.
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    // Find the position in the previous frame of each touch.
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    // Find the magnitude of the vector (the distance) between the touches in each frame.
                    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    // Find the difference in the distances between each frame.
                    float dist = prevTouchDeltaMag - touchDeltaMag;

                    //float dist = Vector2.Distance( Input.touches[0].deltaPosition, Input.touches[1].deltaPosition);
                    desiredDistance += dist * Time.deltaTime * (zoomRate / 50.0f) * Mathf.Abs(desiredDistance);
                    cameraDirty = true;
                }
                break;

            default:
                if (GUIUtility.hotControl == 0 && Input.GetMouseButtonDown(1))
                    lastMousePosition = Input.mousePosition;

                if (GUIUtility.hotControl == 0 && Input.GetMouseButton(1))
                {
                    Vector2 mousePos = Input.mousePosition;
                    Vector2 delta = mousePos - lastMousePosition;
                    lastMousePosition = mousePos;

                    if (allowMouseInputX)
                        ApplyYawDelta(delta.x);
                    else
                        RotateBehindTarget();

                    if (allowMouseInputY)
                        ApplyPitchDelta(-delta.y);

                    if (!lockToRearOfTarget)
                        rotateBehind = false;
                }

                if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.0001f)
                {
                    desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
                    cameraDirty = true;
                }
                break;
        }
    }

    void ApplyYawDelta(float pixelOrAxisDelta)
    {
        targetYaw += 480f * pixelOrAxisDelta / Mathf.Max(Screen.width, 1f);
        cameraDirty = true;
    }

    void ApplyPitchDelta(float pixelOrAxisDelta)
    {
        targetPitch += (pixelOrAxisDelta / Mathf.Max(Screen.height, 1f)) * ySpeed;
        cameraDirty = true;
    }

    float GetRoseTimeWeight()
    {
        float ms = settleTimeMs > 1f ? settleTimeMs : 200f;
        return Mathf.Clamp01(Time.deltaTime * (1000f / ms));
    }

    bool IsSettled(bool isCorrected)
    {
        if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw)) > EpsilonYaw)
            return false;
        if (Mathf.Abs(currentPitch - targetPitch) > EpsilonPitch)
            return false;
        if (!isCorrected && Mathf.Abs(currentDistance - correctedDistance) > EpsilonDistance)
            return false;
        return true;
    }

    private void RotateBehindTarget()
    {
        float targetRotationAngle = target.transform.eulerAngles.y;
        targetYaw = Mathf.LerpAngle(currentYaw, targetRotationAngle, rotationDampening * Time.deltaTime);
        cameraDirty = true;

        if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetRotationAngle)) < EpsilonYaw)
        {
            if (!lockToRearOfTarget)
                rotateBehind = false;
        }
        else
        {
            rotateBehind = true;
        }
    }

    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f)
            angle += 360f;
        if (angle > 360f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
