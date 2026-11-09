using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Shooter Nerd/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField, Min(0)] private int price = 0;
    [SerializeField] private Sprite icon;

    [Header("Combat Attributes")]
    [SerializeField, Min(0.01f)] private float damage = 20f;
    [SerializeField, Min(0.01f)] private float fireInterval = 0.15f;
    [SerializeField, Min(1f)] private float range = 100f;
    [SerializeField, Min(1)] private int magazineSize = 6;
    [SerializeField, Min(0)] private int startingReserveAmmo = 114;
    [SerializeField, Min(0)] private int startingMagazineAmmo = 6;
    [SerializeField, Min(0.01f)] private float shootingAnimationDuration = 0.2f;

    public string WeaponName => weaponName;
    public int Price => price;
    public Sprite Icon => icon;
    public float Damage => damage;
    public float FireInterval => fireInterval;
    public float Range => range;
    public int MagazineSize => magazineSize;
    public int StartingReserveAmmo => startingReserveAmmo;
    public int StartingMagazineAmmo => startingMagazineAmmo;
    public float ShootingAnimationDuration => shootingAnimationDuration;
}
