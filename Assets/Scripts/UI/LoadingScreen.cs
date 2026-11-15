using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Layar loading sederhana: hanya memiliki field Slider.
/// Scene tujuan ditentukan lewat <see cref="TargetScene"/> sebelum memuat scene "Loading".
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Slider slider;

    /// <summary>Nama scene tujuan yang akan dimuat setelah loading.</summary>
    public static string TargetScene { get; set; }

    /// <summary>Helper untuk berpindah scene melalui scene Loading.</summary>
    public static void Load(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName))
            return;

        TargetScene = targetSceneName;
        SceneManager.LoadScene("Loading");
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(TargetScene))
            TargetScene = "Gameplay";

        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(TargetScene);
        if (operation == null)
            yield break;

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            // progress AsyncOperation berhenti di 0.9 sampai scene diaktifkan.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (slider != null)
                slider.value = progress;

            yield return null;
        }

        if (slider != null)
            slider.value = 1f;
    }
}