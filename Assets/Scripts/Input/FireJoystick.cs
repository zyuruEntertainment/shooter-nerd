using UnityEngine;
using UnityEngine.EventSystems;

public class FireJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Inspector References")]
    [SerializeField] private RectTransform touchArea;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private ArcadeWeaponController weapon;

    [Header("Joystick Settings")]
    [SerializeField, Min(1f)] private float handleRange = 78f;
    [SerializeField] private bool keepBackgroundVisible = true;

    private int activePointerId = int.MinValue;

    private void Awake()
    {
        if (touchArea == null || background == null || handle == null)
        {
            Debug.LogError(
                "FireJoystick: Assign Touch Area, Background, and Handle in the Inspector.",
                this
            );
            enabled = false;
            return;
        }

        if (weapon == null)
            weapon = FindAnyObjectByType<ArcadeWeaponController>();

        handle.anchoredPosition = Vector2.zero;

        if (keepBackgroundVisible)
            background.gameObject.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue || !IsInside(touchArea, eventData))
            return;

        activePointerId = eventData.pointerId;
        background.gameObject.SetActive(true);
        UpdateHandle(eventData);

        if (weapon != null)
            weapon.SetFireHeld(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        // Handle tetap memberi feedback sentuhan, tetapi tidak menggeser crosshair.
        UpdateHandle(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ReleaseFire();
    }

    private void UpdateHandle(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        handle.anchoredPosition =
            Vector2.ClampMagnitude(localPoint, handleRange);
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

    private void ReleaseFire()
    {
        activePointerId = int.MinValue;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (weapon != null)
            weapon.SetFireHeld(false);

        if (!keepBackgroundVisible && background != null)
            background.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReleaseFire();
    }
}
