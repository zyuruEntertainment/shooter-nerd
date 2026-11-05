using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MobileInput : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Movement Joystick")]
    [SerializeField] private RectTransform touchArea;
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Movement Settings")]
    [SerializeField, Min(1f)] private float handleRange = 120f;
    [SerializeField, Min(0f)] private float hideDelay = 1f;

    [Header("Aim Joystick")]
    [SerializeField] private RectTransform lookArea;
    [SerializeField, Min(1f)] private float aimHandleRange = 65f;
    [SerializeField, Min(1f)] private float aimSpeed = 180f;
    [SerializeField, Range(0f, 1f)] private float aimDeadZone = 0.15f;
    [SerializeField] private Color aimBackgroundColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Color aimHandleColor = new Color(1f, 1f, 1f, 0.7f);

    private int joystickPointerId = int.MinValue;
    private int lookPointerId = int.MinValue;

    private Vector2 lookInput;
    private Vector2 aimInput;
    private Coroutine hideCoroutine;
    private RectTransform aimBackground;
    private RectTransform aimHandle;
    private ArcadeWeaponController weapon;

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

    public bool IsAimActive =>
        lookPointerId != int.MinValue &&
        aimInput.sqrMagnitude >= aimDeadZone * aimDeadZone;

    private void Awake()
    {
        if (touchArea == null ||
            joystickBackground == null ||
            joystickHandle == null)
        {
            Debug.LogError("MobileInput: Movement joystick references are missing!", this);
            enabled = false;
            return;
        }

        joystickBackground.gameObject.SetActive(false);
        joystickHandle.anchoredPosition = Vector2.zero;

        if (lookArea != null)
            CreateAimJoystickVisuals();

        weapon = FindAnyObjectByType<ArcadeWeaponController>();
    }

    private void Update()
    {
        if (IsAimActive)
        {
            // Stick displacement controls camera rotation speed.
            lookInput += aimInput * aimSpeed * Time.deltaTime;
        }

        if (weapon != null)
            weapon.SetFireHeld(IsAimActive);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Left-side touch remains movement-only.
        if (joystickPointerId == int.MinValue &&
            IsInside(touchArea, eventData))
        {
            if (PlaceMovementJoystick(eventData))
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

        // A second finger in the right-side area controls aiming.
        if (lookPointerId == int.MinValue &&
            IsInside(lookArea, eventData))
        {
            lookPointerId = eventData.pointerId;
            aimInput = Vector2.zero;

            if (PlaceAimJoystick(eventData) && aimBackground != null)
                aimBackground.gameObject.SetActive(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == joystickPointerId)
        {
            MoveMovementHandle(eventData);
            return;
        }

        if (eventData.pointerId == lookPointerId)
            MoveAimHandle(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == joystickPointerId)
        {
            joystickPointerId = int.MinValue;
            joystickHandle.anchoredPosition = Vector2.zero;

            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            hideCoroutine = StartCoroutine(HideMovementJoystick());
            return;
        }

        if (eventData.pointerId == lookPointerId)
            ResetAimJoystick();
    }

    private bool PlaceMovementJoystick(PointerEventData eventData)
    {
        RectTransform parent = joystickBackground.parent as RectTransform;
        if (parent == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return false;

        joystickBackground.anchoredPosition = localPoint;
        return true;
    }

    private void MoveMovementHandle(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        joystickHandle.anchoredPosition =
            Vector2.ClampMagnitude(localPoint, handleRange);
    }

    private bool PlaceAimJoystick(PointerEventData eventData)
    {
        if (lookArea == null || aimBackground == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lookArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return false;

        aimBackground.anchoredPosition = localPoint;
        aimHandle.anchoredPosition = Vector2.zero;
        return true;
    }

    private void MoveAimHandle(PointerEventData eventData)
    {
        if (aimBackground == null || aimHandle == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            aimBackground, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        aimHandle.anchoredPosition =
            Vector2.ClampMagnitude(localPoint, aimHandleRange);

        aimInput = Vector2.ClampMagnitude(
            aimHandle.anchoredPosition / aimHandleRange,
            1f
        );
    }

    private void CreateAimJoystickVisuals()
    {
        aimBackground = CreateImage(
            "AimJoystickBackground",
            lookArea,
            new Vector2(150f, 150f),
            aimBackgroundColor
        );

        aimHandle = CreateImage(
            "AimJoystickHandle",
            aimBackground,
            new Vector2(64f, 64f),
            aimHandleColor
        );

        aimBackground.gameObject.SetActive(false);
    }

    private RectTransform CreateImage(
        string objectName,
        RectTransform parent,
        Vector2 size,
        Color color)
    {
        GameObject go = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private bool IsInside(RectTransform area, PointerEventData eventData)
    {
        return area != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                area, eventData.position, eventData.pressEventCamera);
    }

    public Vector2 ConsumeLookInput()
    {
        Vector2 result = lookInput;
        lookInput = Vector2.zero;
        return result;
    }

    private IEnumerator HideMovementJoystick()
    {
        yield return new WaitForSeconds(hideDelay);

        if (joystickPointerId == int.MinValue)
            joystickBackground.gameObject.SetActive(false);

        hideCoroutine = null;
    }

    private void ResetAimJoystick()
    {
        lookPointerId = int.MinValue;
        aimInput = Vector2.zero;
        lookInput = Vector2.zero;

        if (aimBackground != null)
            aimBackground.gameObject.SetActive(false);

        if (weapon != null)
            weapon.SetFireHeld(false);
    }

    private void OnDisable()
    {
        joystickPointerId = int.MinValue;
        lookPointerId = int.MinValue;
        aimInput = Vector2.zero;
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

        if (aimBackground != null)
            aimBackground.gameObject.SetActive(false);

        if (weapon != null)
            weapon.SetFireHeld(false);
    }
}
