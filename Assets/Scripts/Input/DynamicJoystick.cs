using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DynamicJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform touchArea;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField, Min(1f)] private float handleRange = 60f;
    [SerializeField, Min(0f)] private float hideDelay = 1f;

    private RectTransform backgroundParent;
    private int activeFingerId = int.MinValue;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (touchArea == null || background == null || handle == null)
        {
            Debug.LogError("DynamicJoystick: Referensi belum lengkap!", this);
            enabled = false;
            return;
        }

        backgroundParent = background.parent as RectTransform;

        if (backgroundParent == null)
        {
            Debug.LogError("DynamicJoystick: Background harus memiliki parent RectTransform!", this);
            enabled = false;
            return;
        }

        background.gameObject.SetActive(false);
        handle.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activeFingerId != int.MinValue)
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(
            touchArea,
            eventData.position,
            eventData.pressEventCamera))
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundParent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        activeFingerId = eventData.pointerId;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        background.anchoredPosition = localPoint;
        background.gameObject.SetActive(true);
        handle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activeFingerId)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        handle.anchoredPosition =
            Vector2.ClampMagnitude(localPoint, handleRange);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activeFingerId)
            return;

        activeFingerId = int.MinValue;
        handle.anchoredPosition = Vector2.zero;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideJoystick());
    }

    private IEnumerator HideJoystick()
    {
        yield return new WaitForSeconds(hideDelay);

        if (activeFingerId == int.MinValue)
            background.gameObject.SetActive(false);

        hideCoroutine = null;
    }

    private void OnDisable()
    {
        activeFingerId = int.MinValue;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (background != null)
            background.gameObject.SetActive(false);

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}