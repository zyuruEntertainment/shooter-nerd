using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInput : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Movement Joystick (read-only reference)")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField, Min(1f)] private float handleRange = 120f;

    [Header("Look Area")]
    [SerializeField] private RectTransform lookArea;
    // Pre-multiplier input sentuh (bukan sensitivitas pengguna). Default 1 agar tidak ganda.
    [SerializeField, Min(0.01f)] private float lookMultiplier = 1f;

    private int lookPointerId = int.MinValue;
    private Vector2 lookInput;

    public Vector2 MoveInput
    {
        get
        {
            if (joystickBackground == null ||
                joystickHandle == null ||
                !joystickBackground.gameObject.activeInHierarchy)
                return Vector2.zero;

            return Vector2.ClampMagnitude(
                joystickHandle.anchoredPosition / Mathf.Max(handleRange, 1f),
                1f
            );
        }
    }

    // DynamicJoystick exclusively owns movement touch handling.
    // MobileInput only reads its handle position for PlayerMovement.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (lookPointerId != int.MinValue)
            return;

        // Abaikan pointer yang dimulai pada FireJoystick.
        if (FireJoystick.IsPointerOverFireJoystick(eventData.position, eventData.pressEventCamera))
            return;

        if (IsInside(lookArea, eventData))
            lookPointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (FireJoystick.IsPointerClaimed(eventData.pointerId))
            return;

        if (eventData.pointerId == lookPointerId)
            lookInput += eventData.delta * lookMultiplier;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == lookPointerId)
            lookPointerId = int.MinValue;
    }

    private bool IsInside(RectTransform area, PointerEventData eventData)
    {
        return area != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                area,
                eventData.position,
                eventData.pressEventCamera
            );
    }

    public Vector2 ConsumeLookInput()
    {
        Vector2 result = lookInput;
        lookInput = Vector2.zero;
        return result;
    }

    private void OnDisable()
    {
        lookPointerId = int.MinValue;
        lookInput = Vector2.zero;
    }
}
