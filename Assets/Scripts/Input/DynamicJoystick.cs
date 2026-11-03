using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DynamicJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField, Min(1f)] private float handleRange = 60f;
    [SerializeField, Min(0f)] private float hideDelay = 1f;

    private RectTransform backgroundParent;
    private int activeFingerId = int.MinValue;
    private Coroutine hideCoroutine;

    private Vector2 input;

    public Vector2 Direction => input;

    private void Awake()
    {
        if (background == null || handle == null)
        {
            Debug.LogError("DynamicJoystick: Background atau Handle belum diatur!", this);
            enabled = false;
            return;
        }

        backgroundParent = background.parent as RectTransform;

        if (backgroundParent == null)
        {
            Debug.LogError("DynamicJoystick: Background harus memiliki RectTransform parent!", this);
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

        activeFingerId = eventData.pointerId;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundParent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            activeFingerId = int.MinValue;
            return;
        }

        background.anchoredPosition = localPoint;
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

        activeFingerId = int.MinValue;

        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideJoystick());
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
            localPoint / handleRange,
            1f
        );

        handle.anchoredPosition = input * handleRange;
    }

    private IEnumerator HideJoystick()
    {
        yield return new WaitForSeconds(hideDelay);

        if (activeFingerId == int.MinValue)
        {
            background.gameObject.SetActive(false);
            handle.anchoredPosition = Vector2.zero;
            input = Vector2.zero;
        }

        hideCoroutine = null;
    }

    private void OnDisable()
    {
        activeFingerId = int.MinValue;
        input = Vector2.zero;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (background != null)
            background.gameObject.SetActive(false);
    }
}
