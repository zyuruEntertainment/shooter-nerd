using System;

/// <summary>
/// Identitas tim untuk friendly fire / targeting.
/// None = tidak tergabung tim (damage normal? eksplisit: damage ditolak).
/// </summary>
public enum Team
{
    None = 0,
    TeamA = 1,
    TeamB = 2
}

/// <summary>
/// Resolusi identitas tim yang konsisten dari tag GameObject.
/// </summary>
public static class TeamResolver
{
    /// <summary>Petakan tag yang dikenal ke tim. Tag tidak dikenal -> None (fallback ke serialized).</summary>
    public static Team FromTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return Team.None;
        switch (tag.ToLowerInvariant())
        {
            case "player":
            case "ally":
            case "teama":
                return Team.TeamA;
            case "enemy":
            case "teamb":
                return Team.TeamB;
            default:
                return Team.None;
        }
    }

    public static bool IsRecognizedTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        switch (tag.ToLowerInvariant())
        {
            case "player":
            case "ally":
            case "teama":
            case "enemy":
            case "teamb":
                return true;
            default:
                return false;
        }
    }
}