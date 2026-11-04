using System;
using UnityEngine;

// Static event hub for sound emission. Sound emitters call BotHearing.EmitSound.
public static class BotSoundEvents
{
    public static event Action<SoundEvent> Emitted;

    public static void EmitSound(Vector3 position, float radius, SoundType type, GameObject source = null)
    {
        SoundEvent e = new SoundEvent { position = position, radius = radius, type = type, source = source };
        Emitted?.Invoke(e);
    }
}

public class BotHearing : MonoBehaviour
{
    [SerializeField] private BotSettings settings;

    private Vector3 lastHeardPosition;
    private float lastHeardTime = -999f;
    private bool hasHeard;

    public Vector3 LastHeardPosition => lastHeardPosition;
    public float LastHeardTime => lastHeardTime;
    public bool HasRecentHearing(float memoryDuration)
    {
        if (!hasHeard) return false;
        return (Time.time - lastHeardTime) <= memoryDuration;
    }

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
    }

    private void OnEnable()
    {
        BotSoundEvents.Emitted += OnSoundEmitted;
    }

    private void OnDisable()
    {
        BotSoundEvents.Emitted -= OnSoundEmitted;
    }

    private void OnSoundEmitted(SoundEvent e)
    {
        if (settings == null) return;
        float maxRange = settings.HearingRange;
        float effectiveRadius = Mathf.Min(e.radius, maxRange);
        float sqrDist = (e.position - transform.position).sqrMagnitude;
        if (sqrDist > effectiveRadius * effectiveRadius) return;

        // Approximate location: add small noise so bot doesn't know exact position.
        Vector3 approx = e.position + new Vector3(
            UnityEngine.Random.Range(-0.5f, 0.5f),
            0f,
            UnityEngine.Random.Range(-0.5f, 0.5f)
        );

        lastHeardPosition = approx;
        lastHeardTime = Time.time;
        hasHeard = true;
    }
}