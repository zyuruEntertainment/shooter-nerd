using UnityEngine;

/// <summary>
/// Data granat yang bisa dibeli/dipakai.
/// </summary>
[CreateAssetMenu(fileName = "GrenadeData", menuName = "Shooter Nerd/Grenade Data")]
public class GrenadeData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string grenadeName = "Grenade";
    [SerializeField] private Sprite icon;
    [SerializeField, Min(0)] private int price = 100;

    [Header("Explosion")]
    [SerializeField, Min(0.01f)] private float fuseTime = 2f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 5f;
    [SerializeField, Min(0f)] private float explosionDamage = 80f;
    [SerializeField] private GameObject grenadePrefab;

    public string GrenadeName => grenadeName;
    public Sprite Icon => icon;
    public int Price => price;
    public float FuseTime => fuseTime;
    public float ExplosionRadius => explosionRadius;
    public float ExplosionDamage => explosionDamage;
    public GameObject GrenadePrefab => grenadePrefab;
}