using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DynamicJoystick : MonoBehaviour, IPointerDownHandler,
    IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform touchArea;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 60f;
    [SerializeField] private float hideDelay = 1f;

    private Canvas canvas;
    private Camera uiCamera;

    private Vector2 input;
    private int activeFingerId = -1;

    private Coroutine hideCoroutine;

    public Vector2 Direction => input;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Abaikan input jika sentuhan berada di luar area.
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            touchArea, eventData.position, uiCamera))
            return;

        // Abaikan sentuhan ulang dari jari yang sama.
        if (eventData.pointerId == activeFingerId)
            return;

        // Sentuhan baru mengambil alih posisi joystick.
        activeFingerId = eventData.pointerId;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        MoveJoystick(eventData);

        background.gameObject.SetActive(true);
        handle.anchoredPosition = Vector2.zero;
        input = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activeFingerId)
            return;

        UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activeFingerId)
            return;

        activeFingerId = -1;
        input = Vector2.zero;

        handle.anchoredPosition = Vector2.zero;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideJoystick());
    }

    private void MoveJoystick(PointerEventData eventData)
    {
        RectTransform parent = background.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            background.anchoredPosition = localPoint;
        }
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        input = Vector2.ClampMagnitude(
            localPoint / handleRange, 1f);

        handle.anchoredPosition = input * handleRange;
    }

    private IEnumerator HideJoystick()
    {
        yield return new WaitForSeconds(hideDelay);

        if (activeFingerId == -1)
        {
            background.gameObject.SetActive(false);
            handle.anchoredPosition = Vector2.zero;
            input = Vector2.zero;
        }

        hideCoroutine = null;
    }
}