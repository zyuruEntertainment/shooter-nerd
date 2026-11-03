using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInput : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Camera Look")]
    [SerializeField] private RectTransform lookArea;
    [SerializeField, Min(0f)] private float lookMultiplier = 1f;

    private Vector2 lookInput;
    private int lookPointerId = int.MinValue;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (lookPointerId != int.MinValue)
            return;

        if (IsInside(lookArea, eventData))
        {
            lookPointerId = eventData.pointerId;
        }
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

    private bool IsInside(
        RectTransform area,
        PointerEventData eventData)
    {
        return area != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                area,
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