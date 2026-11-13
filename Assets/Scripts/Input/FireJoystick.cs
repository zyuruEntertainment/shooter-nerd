using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FireJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Inspector References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private ArcadeWeaponController weapon;

    [Header("Joystick Settings")]
    [SerializeField, Min(1f)] private float handleRange = 78f;
    [SerializeField] private bool keepBackgroundVisible = true;

    private int activePointerId = int.MinValue;
    private Vector2 lastPointerPosition;
    private Vector2 fireLookDelta;

    public Vector2 FireLookDelta => fireLookDelta;

    private static readonly List<FireJoystick> instances = new List<FireJoystick>();

    public static bool IsPointerOverFireJoystick(Vector2 screenPosition, Camera cam)
    {
        foreach (var fj in instances)
        {
            if (fj == null || fj.background == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(fj.background, screenPosition, cam))
                return true;
        }
        return false;
    }

    public static bool IsPointerClaimed(int pointerId)
    {
        // Kepemilikan dilacak per instance melalui activePointerId.
        foreach (var fj in instances)
        {
            if (fj != null && fj.activePointerId == pointerId)
                return true;
        }
        return false;
    }

    private void Awake()
    {
        if (background == null || handle == null)
        {
            Debug.LogError("FireJoystick: Assign Background and Handle in the Inspector.", this);
            enabled = false;
            return;
        }

        if (weapon == null)
            weapon = FindAnyObjectByType<ArcadeWeaponController>();

        handle.anchoredPosition = Vector2.zero;

        if (keepBackgroundVisible)
            background.gameObject.SetActive(true);

        if (!instances.Contains(this))
            instances.Add(this);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(background, eventData.position, eventData.pressEventCamera))
            return;

        activePointerId = eventData.pointerId;
        lastPointerPosition = eventData.position;

        background.gameObject.SetActive(true);
        UpdateHandle(eventData);

        if (weapon != null)
            weapon.SetFireHeld(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        fireLookDelta += eventData.position - lastPointerPosition;
        lastPointerPosition = eventData.position;

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

        handle.anchoredPosition = Vector2.ClampMagnitude(localPoint, handleRange);
    }

    private void ReleaseFire()
    {
        activePointerId = int.MinValue;
        lastPointerPosition = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (weapon != null)
            weapon.SetFireHeld(false);

        if (!keepBackgroundVisible && background != null)
            background.gameObject.SetActive(false);
    }

    public Vector2 ConsumeFireLookInput()
    {
        Vector2 result = fireLookDelta;
        fireLookDelta = Vector2.zero;
        return result;
    }

    private void OnDisable()
    {
        if (activePointerId != int.MinValue)
            ReleaseFire();
    }
}
