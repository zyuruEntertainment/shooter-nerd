using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mengelola pause game: menampilkan pause UI (prefab), menghentikan waktu (Time.timeScale),
/// dan mengembalikan kondisi kursor. Singleton agar mudah diakses dari UI/tombol.
/// Pola instantiation mirip dengan GameplayUIManager.OpenShop().
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

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
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            // Jika shop terbuka, tutup shop terlebih dahulu dan hentikan pemrosesan ESC pada frame ini.
            if (GameplayUIManager.Instance != null && GameplayUIManager.Instance.IsShopOpen)
            {
                GameplayUIManager.Instance.CloseShop();
                return;
            }

            if (isPaused)
                Resume();
            else
                Pause();
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

    /// <summary>Pause game dan tampilkan pause UI.</summary>
    public void Pause()
    {
        if (isPaused)
            return;

        if (pausePrefab == null)
        {
            Debug.LogError("Pause Prefab belum diatur!");
            return;
        }

        isPaused = true;

        // Simpan kondisi cursor sebelumnya.
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // Tampilkan kursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Simpan kondisi waktu sebelumnya dan hentikan waktu.
        previousTimeScale = Time.timeScale;
        if (pauseTimeScale)
            Time.timeScale = 0f;

        // Sembunyikan gameplay UI jika konfigurasi mengharuskannya.
        if (hideGameplayUIWhilePaused && gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        // Instantiate pause prefab ke parent (mirip OpenShop).
        pauseInstance = Instantiate(pausePrefab, pauseParent, false);
        if (pauseInstance != null)
            pauseInstance.SetActive(true);

        OnPause?.Invoke();
    }

    /// <summary>Lanjutkan game dan sembunyikan pause UI.</summary>
    public void Resume()
    {
        if (!isPaused)
            return;

        // Hancurkan instance pause UI (mencegah instance ganda).
        if (pauseInstance != null)
        {
            Destroy(pauseInstance);
            pauseInstance = null;
        }

        isPaused = false;

        // Pulihkan gameplay UI.
        if (hideGameplayUIWhilePaused && gameplayUIRoot != null)
            gameplayUIRoot.SetActive(true);

        // Kembalikan kondisi kursor gameplay.
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;

        // Kembalikan Time.timeScale.
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
