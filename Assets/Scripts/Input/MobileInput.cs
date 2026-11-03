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
    [SerializeField] private float handleRange = 60f;
    [SerializeField] private float hideDelay = 1f;

    [Header("Camera Look")]
    [SerializeField] private RectTransform lookArea;
    [SerializeField] private float lookMultiplier = 1f;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private int joystickPointerId = int.MinValue;
    private int lookPointerId = int.MinValue;

    private Coroutine hideCoroutine;

    public Vector2 MoveInput => moveInput;

    private void Awake()
    {
        joystickBackground.gameObject.SetActive(false);
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsInside(touchArea, eventData) &&
            joystickPointerId == int.MinValue)
        {
            joystickPointerId = eventData.pointerId;

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            RectTransform parent =
                joystickBackground.parent as RectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                joystickBackground.anchoredPosition = localPoint;
                joystickBackground.gameObject.SetActive(true);
            }

            joystickHandle.anchoredPosition = Vector2.zero;
            moveInput = Vector2.zero;
        }

        if (IsInside(lookArea, eventData) &&
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

            moveInput = Vector2.zero;
            joystickHandle.anchoredPosition = Vector2.zero;

            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            hideCoroutine = StartCoroutine(HideJoystick());
        }

        if (eventData.pointerId == lookPointerId)
        {
            lookPointerId = int.MinValue;
        }
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        moveInput = Vector2.ClampMagnitude(
            localPoint / handleRange,
            1f
        );

        joystickHandle.anchoredPosition =
            moveInput * handleRange;
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
        joystickPointerId = int.MinValue;
        lookPointerId = int.MinValue;

        moveInput = Vector2.zero;
        lookInput = Vector2.zero;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);
    }
}