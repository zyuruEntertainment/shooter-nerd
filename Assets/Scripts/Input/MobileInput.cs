
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInput : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Joystick")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Camera Look")]
    [SerializeField] private RectTransform lookArea;
    [SerializeField, Min(0f)] private float lookMultiplier = 1f;

    public Vector2 MoveInput { get; private set; }

    private Vector2 lookInput;

    private int joystickPointerId = int.MinValue;
    private int lookPointerId = int.MinValue;

    private void Awake()
    {
        if (joystickBackground != null &&
            joystickHandle == null)
        {
            Transform handle =
                joystickBackground.Find("Handle");

            if (handle != null)
                joystickHandle = handle as RectTransform;
        }

        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsInside(joystickBackground, eventData.position))
        {
            if (joystickPointerId != int.MinValue)
                return;

            joystickPointerId = eventData.pointerId;
            UpdateJoystick(eventData);
            return;
        }

        if (lookArea != null &&
            IsInside(lookArea, eventData.position) &&
            lookPointerId == int.MinValue)
        {
            lookPointerId = eventData.pointerId;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == joystickPointerId)
        {
            UpdateJoystick(eventData);
            return;
        }

        if (eventData.pointerId == lookPointerId)
        {
            lookInput += eventData.delta * lookMultiplier;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == joystickPointerId)
        {
            joystickPointerId = int.MinValue;
            ResetJoystick();
        }

        if (eventData.pointerId == lookPointerId)
        {
            lookPointerId = int.MinValue;
        }
    }

    public Vector2 ConsumeLookInput()
    {
        Vector2 result = lookInput;
        lookInput = Vector2.zero;
        return result;
    }

    private bool IsInside(
        RectTransform area,
        Vector2 screenPosition)
    {
        return area != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                area,
                screenPosition,
                null
            );
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (joystickBackground == null ||
            joystickHandle == null)
        {
            MoveInput = Vector2.zero;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            return;
        }

        Vector2 offset =
            localPoint - joystickBackground.rect.center;

        float radiusX = joystickBackground.rect.width * 0.5f;
        float radiusY = joystickBackground.rect.height * 0.5f;

        MoveInput = Vector2.ClampMagnitude(
            new Vector2(
                offset.x / Mathf.Max(radiusX, 1f),
                offset.y / Mathf.Max(radiusY, 1f)
            ),
            1f
        );

        joystickHandle.anchoredPosition = new Vector2(
            MoveInput.x * radiusX,
            MoveInput.y * radiusY
        );
    }

    private void ResetJoystick()
    {
        MoveInput = Vector2.zero;

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        joystickPointerId = int.MinValue;
        lookPointerId = int.MinValue;
        lookInput = Vector2.zero;
        ResetJoystick();
    }
}
