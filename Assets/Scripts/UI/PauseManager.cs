using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mengelola pause game: menampilkan canvas pause, menghentikan waktu (Time.timeScale),
/// dan mengembalikan kondisi kursor. Singleton agar mudah diakses dari UI/tombol.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Pause UI - Scene")]
    [Tooltip("Canvas/panel yang sudah ada di scene (opsional).")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("Pause UI - Prefab")]
    [Tooltip("Prefab pause yang akan di-instantiate saat pause (seperti shop).")]
    [SerializeField] private GameObject pausePrefab;
    [SerializeField] private Transform pauseParent;

    [Header("Pause Settings")]
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool hideGameplayUIWhilePaused = false;
    [SerializeField] private GameObject gameplayUIRoot;

    private GameObject pauseInstance;
    private bool isPaused;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

    public bool IsPaused => isPaused;

    public event System.Action OnPause;
    public event System.Action OnResume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Escape: jika shop terbuka, biarkan GameplayUIManager yang menutup shop.
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (GameplayUIManager.Instance != null && GameplayUIManager.Instance.IsShopOpen)
                return;

            TogglePause();
        }
    }

    /// <summary>Toggle status pause.</summary>
    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>Pause game dan tampilkan canvas pause.</summary>
    public void Pause()
    {
        if (isPaused)
            return;

        isPaused = true;

        // Tampilkan pause: gunakan canvas scene jika ada, kalau tidak instantiate prefab.
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(true);
        }
        else if (pausePrefab != null)
        {
            if (pauseInstance == null)
                pauseInstance = Instantiate(pausePrefab, pauseParent, false);
            if (pauseInstance != null)
                pauseInstance.SetActive(true);
        }

        if (hideGameplayUIWhilePaused && gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        // Simpan kondisi sebelumnya.
        previousTimeScale = Time.timeScale;
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // Tampilkan kursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseTimeScale)
            Time.timeScale = 0f;

        OnPause?.Invoke();
    }

    /// <summary>Lanjutkan game dan sembunyikan canvas pause.</summary>
    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }
        else if (pauseInstance != null)
        {
            Destroy(pauseInstance);
            pauseInstance = null;
        }

        if (hideGameplayUIWhilePaused && gameplayUIRoot != null)
            gameplayUIRoot.SetActive(true);

        // Kembalikan kondisi kursor gameplay.
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;

        if (pauseTimeScale)
            Time.timeScale = previousTimeScale;

        OnResume?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (isPaused && pauseTimeScale)
                Time.timeScale = previousTimeScale;

            if (pauseInstance != null)
                Destroy(pauseInstance);

            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (isPaused)
        {
            // Paksa kursor tetap terlihat selama pause.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
