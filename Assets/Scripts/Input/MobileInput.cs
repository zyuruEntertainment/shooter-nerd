using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class MobileInput : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Joystick")]
    [SerializeField] private RectTransform touchArea;
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Joystick Settings")]
    [SerializeField, Min(1f)] private float handleRange = 60f;
    [SerializeField, Min(0f)] private float hideDelay = 1f;

    [Header("Camera Look")]
    [SerializeField] private RectTransform lookArea;
    [SerializeField, Min(0f)] private float lookMultiplier = 1f;

    private int joystickPointerId = int.MinValue;
    private int lookPointerId = int.MinValue;

    private Vector2 lookInput;
    private Coroutine hideCoroutine;

    // Character movement is calculated from the HANDLE position.
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

    private void Awake()
    {
        if (touchArea == null ||
            joystickBackground == null ||
            joystickHandle == null)
        {
            Debug.LogError(
                "MobileInput: Joystick references are missing!",
                this
            );
            enabled = false;
            return;
        }

        joystickBackground.gameObject.SetActive(false);
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Joystick: only the first finger inside Touch Area can activate it.
        if (joystickPointerId == int.MinValue &&
            IsInside(touchArea, eventData))
        {
            if (PlaceJoystick(eventData))
            {
                joystickPointerId = eventData.pointerId;

                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                    hideCoroutine = null;
                }

                joystickHandle.anchoredPosition = Vector2.zero;
                joystickBackground.gameObject.SetActive(true);
            }

            return;
        }

        // Camera: independent pointer in the look area.
        if (lookPointerId == int.MinValue &&
            IsInside(lookArea, eventData))
        {
            lookPointerId = eventData.pointerId;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == joystickPointerId)
        {
            MoveHandle(eventData);
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

            joystickHandle.anchoredPosition = Vector2.zero;

            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            hideCoroutine = StartCoroutine(HideJoystick());
            return;
        }

        if (eventData.pointerId == lookPointerId)
        {
            lookPointerId = int.MinValue;
        }
    }

    private bool PlaceJoystick(PointerEventData eventData)
    {
        RectTransform parent =
            joystickBackground.parent as RectTransform;

        if (parent == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return false;

        joystickBackground.anchoredPosition = localPoint;
        return true;
    }

    private void MoveHandle(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        // Handle position is clamped to the joystick radius.
        joystickHandle.anchoredPosition =
            Vector2.ClampMagnitude(localPoint, handleRange);
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

    public Vector2 ConsumeLookInput()
    {
        Vector2 result = lookInput;
        lookInput = Vector2.zero;
        return result;
    }

    private IEnumerator HideJoystick()
    {
        yield return new WaitForSeconds(hideDelay);

        if (joystickPointerId == int.MinValue)
            joystickBackground.gameObject.SetActive(false);

        hideCoroutine = null;
    }

    private void OnDisable()
    {
        joystickPointerId = int.MinValue;
        lookPointerId = int.MinValue;

        lookInput = Vector2.zero;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;
    }
}