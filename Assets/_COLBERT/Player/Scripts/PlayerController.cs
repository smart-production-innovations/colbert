using UnityEngine;
using UnityEngine.InputSystem;

//control non-xr player movement
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputActionProperty moveAction;
    [SerializeField]
    private InputActionProperty lookAction;
    [SerializeField]
    private InputActionProperty heightAction;
    [SerializeField]
    private InputActionProperty runAction;

    [SerializeField]
    private float lookAngleRange = 70;
    [SerializeField]
    private Vector2 moveInputMultiplier = new Vector2(1.0f, 1.0f);
    [SerializeField]
    private Vector2 lookMouseInputMultiplier = new Vector2(0.1f, 0.1f);
    [SerializeField]
    private Vector2 lookStickInputMultiplier = new Vector2(150.0f, 75.0f);
    [SerializeField]
    private float lookAcceleration = 8;
    [SerializeField]
    private float moveAcceleration = 3;
    [SerializeField]
    private float runMultiplier = 5f;
    [SerializeField]
    private float heightSpeed = 10f;
    [SerializeField]
    private float heightStep = 0.1f;
    [SerializeField]
    private Transform playerHead;

    private Vector2 moveInput;
    private Vector2 lookMouseInput;
    private Vector2 lookStickInput;
    private float heightInput;

    private Vector2 moveSpeed;
    private Vector2 lookSpeed;
    private float camRotation;
    private float targetHeight;


    private static int lockstate = 0;
    public static void Lock()
    {
        lockstate++;
    }
    public static void Unlock()
    {
        lockstate--;
    }


    private void OnEnable()
    {
        targetHeight = playerHead.localPosition.y;
        moveSpeed = Vector2.zero;
        lookSpeed = Vector2.zero;
    }

    private void Update()
    {
        UpdatePlayer();
    }

    private void UpdatePlayer()
    {
        if (lockstate == 0)
        {
            moveInput = moveAction.action.ReadValue<Vector2>() * moveInputMultiplier;
            if (runAction.action.ReadValue<float>() > 0f)
                moveInput *= runMultiplier;

            heightInput = heightAction.action.ReadValue<float>();

            if (GetCurrentDevice.CurrentInputDevice == GetCurrentDevice.InputDevice.KeyboardAndMouse && !Cursor.visible)
            {
                lookMouseInput = lookAction.action.ReadValue<Vector2>() * lookMouseInputMultiplier;
            }
            else
            {
                lookMouseInput = Vector2.zero;
            }
            if (GetCurrentDevice.CurrentInputDevice == GetCurrentDevice.InputDevice.Gamepad || GetCurrentDevice.CurrentInputDevice == GetCurrentDevice.InputDevice.Touchscreen)
            {
                lookStickInput = lookAction.action.ReadValue<Vector2>() * lookStickInputMultiplier;
            }
            else
            {
                lookStickInput = Vector2.zero;
            }
        }
        else
        {
            moveInput = Vector2.zero;
            heightInput = 0;
            lookMouseInput = Vector2.zero;
            lookStickInput = Vector3.zero;
        }

        UpdateMove(moveInput);
        UpdateHeight(heightInput);

        Vector2 lookDelta = lookMouseInput;
        Vector2 lookSpeed = lookMouseInput / Time.deltaTime + lookStickInput;
        UpdateLook(lookDelta, lookSpeed);
    }

    private void UpdateMove(Vector2 targetSpeed)
    {
        if (moveSpeed != targetSpeed)
        {
            moveSpeed = SmoothSpeed(moveSpeed, targetSpeed, moveAcceleration);
        }
        if (moveSpeed != Vector2.zero)
        {
            Vector3 pos = transform.localPosition;
            pos += transform.localRotation * new Vector3(moveSpeed.x * Time.deltaTime, 0, moveSpeed.y * Time.deltaTime);
            transform.localPosition = pos;
        }
    }

    private void UpdateHeight(float heightDeltaDirection)
    {
        float oldTargetHeight = targetHeight;
        if (heightDeltaDirection != 0)
        {
            targetHeight += Mathf.Sign(heightDeltaDirection) * heightStep;
        }
        float height = playerHead.localPosition.y;
        if (height != targetHeight)
        {
            float newHeight = SmoothHeight(height, targetHeight, oldTargetHeight, heightSpeed);
            playerHead.localPosition = new Vector3(0, newHeight, 0);
        }
    }

    private void UpdateLook(Vector2 lookDelta, Vector2 targetSpeed)
    {
        if (lookSpeed != targetSpeed) //smooth look, use speed
        {
            lookSpeed = SmoothSpeed(lookSpeed, targetSpeed, lookAcceleration);
            lookDelta = lookSpeed * Time.deltaTime;
        }
        else
        {
            lookDelta = targetSpeed * Time.deltaTime;
        }
        if (lookDelta != Vector2.zero && !float.IsNaN(lookDelta.x) && !float.IsNaN(lookDelta.y)) //lookdelta is NaN after playmode pause?
        {
            transform.Rotate(new Vector3(0, lookDelta.x), Space.Self);
            camRotation += lookDelta.y;
            camRotation = Mathf.Clamp(camRotation, -lookAngleRange, lookAngleRange);
            playerHead.localRotation = Quaternion.Euler(-camRotation, 0, 0);
        }
    }

    private static Vector2 SmoothSpeed(Vector2 value, Vector2 target, float speed)
    {
        Vector2 difference = target - value;
        Vector2 direction = difference.normalized;
        float distance = difference.magnitude;
        float step = speed * Time.deltaTime * (1 + distance);
        Vector2 newValue = target;
        if (step < distance)
        {
            newValue = value + direction * step;
        }
        return newValue;
    }

    private static float SmoothHeight(float value, float target, float oldTarget, float speed)
    {
        float t = Time.deltaTime * speed;
        float v = (target - oldTarget) / t;
        float f = value - oldTarget + v;
        return target - v + f * Mathf.Exp(-t);
    }
}