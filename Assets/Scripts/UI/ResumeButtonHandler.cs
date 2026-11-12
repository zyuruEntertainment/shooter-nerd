using UnityEngine;

/// <summary>
/// Menangani klik tombol Resume pada Pause prefab.
/// Memanggil PauseManager.Instance.Resume() dengan pengecekan null.
/// </summary>
public class ResumeButtonHandler : MonoBehaviour
{
    public void OnResumeClicked()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
    }
}
