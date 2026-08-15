using UnityEngine;

/// <summary>
/// Camera controller.
/// </summary>
public class CameraController : MonoBehaviour
{
    public GameObject target;
    public float targetHeight = 1.7f;
    public float distance = 12.0f;
    public float offsetFromWall = 2.0f;
    public float maxDistance = 30f;
    public float minDistance = 2.0f;
    public float xSpeed = 200.0f;
    public float ySpeed = 200.0f;
    public float yMinLimit = -80f;
    public float yMaxLimit = 80f;
    public float zoomRate = 40f;
    public float rotationDampening = 3.0f;
    public float zoomDampening = 3.0f;
    public LayerMask collisionLayers = -1;
    public bool lockToRearOfTarget = false;
    public bool allowMouseInputX = true;
    public bool allowMouseInputY = true;

    [Header("Free Look")]
    public KeyCode freeLookKey = KeyCode.F5;
    public float freeLookSpeed = 10f;
    public float freeLookMinSpeed = 1f;
    public float freeLookMaxSpeed = 100f;
    public float freeLookScrollSpeed = 5f;

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
    private float pbuffer = 0.0f;
    private Vector2 lastMousePosition;

    private bool freeLook;
    private Vector3 freeLookPosition;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        targetYaw = currentYaw = angles.y;
        targetPitch = currentPitch = NormalizePitch(angles.x);

        currentDistance = distance;
        desiredDistance = distance;
        correctedDistance = distance;

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody)
            rigidbody.freezeRotation = true;

        if (lockToRearOfTarget)
            rotateBehind = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(freeLookKey))
        {
            freeLook = !freeLook;

            if (freeLook)
            {
                freeLookPosition = transform.position;
            }
            else
            {
                cameraDirty = true;
            }
        }

        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player") as GameObject;
        }
    }

    void LateUpdate()
    {
        if (freeLook)
        {
            HandleFreeLook();
            return;
        }

        if (target == null)
            return;

        if (pbuffer > 0)
            pbuffer -= Time.deltaTime;

        if (pbuffer < 0)
            pbuffer = 0;

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

        if (rotateBehind)
            RotateBehindTarget();
    }

    private void HandleFreeLook()
    {
        HandleFreeLookRotation();
        HandleFreeLookMovement();
    }

    private void HandleFreeLookRotation()
    {
        if (GUIUtility.hotControl == 0 && Input.GetMouseButtonDown(1))
            lastMousePosition = Input.mousePosition;

        if (GUIUtility.hotControl == 0 && Input.GetMouseButton(1))
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 delta = mousePos - lastMousePosition;
            lastMousePosition = mousePos;

            if (allowMouseInputX)
                currentYaw += 480f * delta.x / Mathf.Max(Screen.width, 1f);

            if (allowMouseInputY)
                currentPitch += (-delta.y / Mathf.Max(Screen.height, 1f)) * ySpeed;

            currentPitch = ClampAngle(currentPitch, yMinLimit, yMaxLimit);
        }

        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void HandleFreeLookMovement()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.0001f)
        {
            freeLookSpeed += scroll * freeLookScrollSpeed;
            freeLookSpeed = Mathf.Clamp(freeLookSpeed, freeLookMinSpeed, freeLookMaxSpeed);
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.Q))
            horizontal -= 1f;

        if (Input.GetKey(KeyCode.D))
            horizontal += 1f;

        if (Input.GetKey(KeyCode.Z))
            vertical += 1f;

        if (Input.GetKey(KeyCode.S))
            vertical -= 1f;

        Vector3 movement =
            transform.right * horizontal +
            transform.forward * vertical;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        freeLookPosition += movement * freeLookSpeed * Time.deltaTime;

        transform.position = freeLookPosition;
    }

    void HandleOrbitInput()
    {
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