using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager untuk menampilkan sebuah prefab UI di root hierarchy (tanpa parent)
/// sambil menyembunyikan UI lain.
/// Mendukung field "ui" berupa objek scene maupun prefab asset:
/// jika yang dimasukkan adalah prefab, script akan mencari instancenya di scene.
/// </summary>
public class UIPrefabManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Prefab UI yang akan ditampilkan saat dipanggil.")]
    [SerializeField] private GameObject prefabUI;

    [Tooltip("UI yang akan disembunyikan saat prefab UI ditampilkan (bisa scene object atau prefab).")]
    [SerializeField] private GameObject ui;

    private GameObject spawnedInstance;

    // Instance UI yang disembunyikan saat Show() dipanggil, agar Close() hanya
    // mengembalikan yang relevan (mencegah UI lain ikut muncul).
    private readonly List<GameObject> lastHidden = new List<GameObject>();

    // Semua instance UI yang dikelola (termasuk yang dimunculkan UIPrefabOnAwake).
    public static readonly List<GameObject> AllUIInstances = new List<GameObject>();

    public static void RegisterUIInstance(GameObject instance)
    {
        if (instance == null) return;
        if (!AllUIInstances.Contains(instance))
            AllUIInstances.Add(instance);
    }

    // Melacak instance prefab yang pernah ditampilkan agar bisa disembunyikan
    // saat prefab lain dipanggil.
    private static readonly List<GameObject> shownInstances = new List<GameObject>();

    /// <summary>
    /// Memanggil/menampilkan prefab UI di root hierarchy (tanpa parent)
    /// dan menyembunyikan UI yang ditentukan.
    /// </summary>
    public void Show()
    {
        if (prefabUI == null)
        {
            Debug.LogWarning("[UIPrefabManager] prefabUI belum diatur.");
            return;
        }

        lastHidden.Clear();

        // Sembunyikan UI yang ditentukan (scene object atau instance dari prefab).
        GameObject targetUi = ResolveSceneObject(ui);
        if (targetUi != null && targetUi != spawnedInstance && targetUi.activeSelf)
        {
            targetUi.SetActive(false);
            lastHidden.Add(targetUi);
        }

        // Sembunyikan semua instance UI terdaftar yang sedang aktif (kecuali yang akan ditampilkan).
        for (int i = AllUIInstances.Count - 1; i >= 0; i--)
        {
            if (AllUIInstances[i] == null)
            {
                AllUIInstances.RemoveAt(i);
                continue;
            }

            if (AllUIInstances[i] != spawnedInstance && AllUIInstances[i].activeSelf)
            {
                AllUIInstances[i].SetActive(false);
                lastHidden.Add(AllUIInstances[i]);
            }
        }

        for (int i = shownInstances.Count - 1; i >= 0; i--)
        {
            if (shownInstances[i] == null)
            {
                shownInstances.RemoveAt(i);
                continue;
            }

            if (shownInstances[i] != spawnedInstance && shownInstances[i].activeSelf)
            {
                shownInstances[i].SetActive(false);
                lastHidden.Add(shownInstances[i]);
            }
        }

        if (spawnedInstance == null)
        {
            // Instantiate tanpa parent => langsung di root hierarchy.
            spawnedInstance = Instantiate(prefabUI);
            shownInstances.Add(spawnedInstance);
            RegisterUIInstance(spawnedInstance);
        }

        spawnedInstance.SetActive(true);
    }

    /// <summary>
    /// Menutup (menyembunyikan) prefab UI yang ditampilkan dan menampilkan kembali UI yang disembunyikan.
    /// </summary>
    public void Close()
    {
        if (spawnedInstance != null)
            spawnedInstance.SetActive(false);

        // Tampilkan kembali hanya UI yang disembunyikan saat Show().
        foreach (GameObject obj in lastHidden)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        lastHidden.Clear();

        SetUiActive(true);
    }

    private void HideOtherRegisteredInstances()
    {
        for (int i = AllUIInstances.Count - 1; i >= 0; i--)
        {
            if (AllUIInstances[i] == null)
            {
                AllUIInstances.RemoveAt(i);
                continue;
            }

            if (AllUIInstances[i] != spawnedInstance)
                AllUIInstances[i].SetActive(false);
        }

        for (int i = shownInstances.Count - 1; i >= 0; i--)
        {
            if (shownInstances[i] == null)
            {
                shownInstances.RemoveAt(i);
                continue;
            }

            if (shownInstances[i] != spawnedInstance)
                shownInstances[i].SetActive(false);
        }
    }

    private void ShowOtherRegisteredInstances()
    {
        for (int i = AllUIInstances.Count - 1; i >= 0; i--)
        {
            if (AllUIInstances[i] == null)
            {
                AllUIInstances.RemoveAt(i);
                continue;
            }

            if (AllUIInstances[i] != spawnedInstance)
                AllUIInstances[i].SetActive(true);
        }
    }

    private void SetUiActive(bool active)
    {
        GameObject target = ResolveSceneObject(ui);
        if (target != null)
            target.SetActive(active);
    }

    /// <summary>
    /// Jika objek sudah berada di scene, kembalikan langsung.
    /// Jika berupa prefab asset, cari instancenya di scene berdasarkan nama.
    /// </summary>
    private GameObject ResolveSceneObject(GameObject obj)
    {
        if (obj == null)
            return null;

        // Objek scene memiliki scene yang valid.
        if (obj.scene.IsValid())
            return obj;

        // Prefab asset: cari instance di scene dengan nama yang sama.
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            GameObject found = FindByName(root, obj.name);
            if (found != null)
                return found;
        }

        return null;
    }

    private GameObject FindByName(GameObject root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        foreach (Transform child in root.transform)
        {
            GameObject found = FindByName(child.gameObject, name);
            if (found != null)
                return found;
        }

        return null;
    }
}
