using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu untuk membuat file .asset (ScriptableObject) tanpa harus lewat Project window.
/// Akses: Assets > Shooter Nerd > Create ...
/// </summary>
public static class AssetCreator
{
    [MenuItem("Assets/Shooter Nerd/Create WeaponData")]
    public static void CreateWeaponData() => CreateAsset<WeaponData>("NewWeaponData");

    [MenuItem("Assets/Shooter Nerd/Create AmmoData")]
    public static void CreateAmmoData() => CreateAsset<AmmoData>("NewAmmoData");

    [MenuItem("Assets/Shooter Nerd/Create GrenadeData")]
    public static void CreateGrenadeData() => CreateAsset<GrenadeData>("NewGrenadeData");

    private static void CreateAsset<T>(string defaultName) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + defaultName + ".asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}