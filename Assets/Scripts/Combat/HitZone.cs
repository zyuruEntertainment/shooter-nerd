using UnityEngine;

public enum HitZoneType
{
    Body,
    Head
}

[DisallowMultipleComponent]
public class HitZone : MonoBehaviour
{
    [Header("Hit Zone")]
    [SerializeField] private HitZoneType zoneType;

    [SerializeField, Min(0.1f)]
    private float damageMultiplier = 1f;

    private CombatHealth owner;

    public HitZoneType ZoneType => zoneType;

    private void Awake()
    {
        owner = GetComponentInParent<CombatHealth>();

        if (owner == null)
        {
            Debug.LogError(
                $"HitZone on {name} membutuhkan CombatHealth pada parent.",
                this
            );
        }
    }

    // Kompatibilitas dengan pemanggilan damage lama.
    public void ApplyDamage(float baseDamage)
    {
        ApplyDamage(baseDamage, null);
    }

    // Damage dengan identitas attacker.
    public void ApplyDamage(
        float baseDamage,
        CombatHealth attacker
    )
    {
        if (owner == null || owner.IsDead)
            return;

        // Validasi tim sebelum menerapkan damage (termasuk headshot multiplier).
        if (attacker != null && attacker != owner)
        {
            if (attacker.Team == Team.None || owner.Team == Team.None)
            {
                Debug.LogWarning($"[HitZone] Damage diblokir karena tim tidak valid: {attacker.gameObject.name} ({attacker.Team}) -> {owner.gameObject.name} ({owner.Team}).", this);
                return;
            }
            if (attacker.Team == owner.Team)
            {
                Debug.LogWarning($"[HitZone] Friendly fire diblokir: {attacker.gameObject.name} ({attacker.Team}) -> {owner.gameObject.name} ({owner.Team}).", this);
                return;
            }
        }

        float finalDamage =
            baseDamage * damageMultiplier;

        owner.TakeDamage(finalDamage, attacker);
    }
}