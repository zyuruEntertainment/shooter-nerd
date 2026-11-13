using UnityEngine;

/// <summary>
/// Menangani klik tombol Pause (pada gameplay UI).
/// Memanggil PauseManager.Instance.Pause() dengan pengecekan null.
/// </summary>
public class PauseButtonHandler : MonoBehaviour
{
    public void OnPauseClicked()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.Pause();
    }

    public void OnResumeClicked()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
    }

    public void OnTogglePauseClicked()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.TogglePause();
    }
}
