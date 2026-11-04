using UnityEngine;

// Optional component. Attach to the player (or any audible agent) to feed
// the event-based hearing system. Emits footstep sounds while moving.
public class BotFootstepEmitter : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float stepInterval = 0.45f;
    [SerializeField, Min(0f)] private float stepRadius = 12f;
    [SerializeField, Min(0f)] private float moveThreshold = 0.5f;

    private Vector3 lastPosition;
    private float stepTimer;
    private CharacterController cc;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
        float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (speed < moveThreshold)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer += Time.deltaTime;
        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;
            BotSoundEvents.EmitSound(transform.position, stepRadius, SoundType.Footstep, gameObject);
        }
    }
}