using UnityEngine;

/// <summary>
/// Mengatur cursor mouse agar terlihat dan tidak terkunci di scene Result,
/// sehingga tombol UI (Retry, Main Menu, dll.) dapat diklik.
///
/// Script ini hanya mengatur status Cursor pada saat OnEnable,
/// tidak mengubah status Pause Editor, dan tidak mengubah pengaturan
/// cursor di scene Gameplay secara permanen.
/// </summary>
[DisallowMultipleComponent]
public class ResultCursor : MonoBehaviour
{
    private void OnEnable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
