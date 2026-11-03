using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInput : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Joystick Reader")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField, Min(1f)] private float handleRange = 60f;

    [Header("Camera Look")]
    [SerializeField] private RectTransform lookArea;
    [SerializeField, Min(0f)] private float lookMultiplier = 1f;

    private Vector2 lookInput;
    private int lookPointerId = int.MinValue;

    public Vector2 MoveInput
    {
        get
        {
            if (joystickBackground == null ||
                joystickHandle == null ||
                !joystickBackground.gameObject.activeInHierarchy)
                return Vector2.zero;

            return Vector2.ClampMagnitude(
                joystickHandle.anchoredPosition /
                Mathf.Max(handleRange, 1f),
                1f
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (lookPointerId != int.MinValue)
            return;

        if (IsInsideLookArea(eventData))
            lookPointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != lookPointerId)
            return;

        lookInput += eventData.delta * lookMultiplier;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != lookPointerId)
            return;

        lookPointerId = int.MinValue;
    }

    public Vector2 ConsumeLookInput()
    {
        Vector2 result = lookInput;
        lookInput = Vector2.zero;
        return result;
    }

    private bool IsInsideLookArea(PointerEventData eventData)
    {
        return lookArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                lookArea,
                eventData.position,
                eventData.pressEventCamera
            );
    }

    private void OnDisable()
    {
        lookPointerId = int.MinValue;
        lookInput = Vector2.zero;
    }
}