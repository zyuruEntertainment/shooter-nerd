using System;
using UnityEngine;

// All possible AI states for a bot.
public enum BotState
{
    Idle,
    Patrol,
    Investigate,
    Chase,
    Attack,
    Search,
    Recover
}

// What the perception system currently knows about the player.
public enum PerceptionStatus
{
    None,
    Heard,
    LastKnown,
    Visible
}

// Lightweight sound event payload used by the event-based hearing system.
public struct SoundEvent
{
    public Vector3 position;
    public float radius;
    public SoundType type;
    public GameObject source;
}

public enum SoundType
{
    Footstep,
    Gunshot,
    Explosion,
    Other
}