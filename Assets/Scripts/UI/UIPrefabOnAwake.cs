using UnityEngine;

/// <summary>
/// Memunculkan (instantiate) sebuah prefab UI saat Awake.
/// Prefab diletakkan langsung di root hierarchy (tanpa parent).
/// </summary>
public class UIPrefabOnAwake : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Prefab UI yang akan dimunculkan saat Awake.")]
    [SerializeField] private GameObject ui;

    private void Awake()
    {
        if (ui == null)
        {
            Debug.LogWarning("[UIPrefabOnAwake] Field 'ui' belum diatur.");
            return;
        }

        // Instantiate tanpa parent => langsung di root hierarchy.
        GameObject instance = Instantiate(ui);
        instance.SetActive(true);
    }
}
