using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("Gameplay UI")]
    [SerializeField] private List<GameObject> gameplayUI = new List<GameObject>();
    [SerializeField] private GameObject BackButton;

    [Header("Shop Prefab")]
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private Transform shopParent;

    [Header("Settings")]
    [SerializeField] private bool pauseGameplay = true;

    private GameObject shopInstance;
    private bool shopIsOpen;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private Canvas backButtonCanvas;

    private void Awake()
    {
        SetBackButton(false);

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

        if (keyboard.bKey.wasPressedThisFrame)
            OpenShop();

        if (keyboard.escapeKey.wasPressedThisFrame)
            CloseShop();
    }

    public void OpenShop()
    {
        if (shopIsOpen) return;

        if (shopPrefab == null)
        {
            Debug.LogError("Shop Prefab belum diatur!");
            return;
        }

        shopIsOpen = true;
        SetGameplayUI(false);
        SetBackButton(true);

        // simpan kondisi kursor sebelumnya
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // tampilkan kursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseGameplay)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        shopInstance = Instantiate(shopPrefab, shopParent, false);
        shopInstance.SetActive(true);
    }

    public void CloseShop()
    {
        if (!shopIsOpen) return;

        if (shopInstance != null)
        {
            Destroy(shopInstance);
            shopInstance = null;
        }

        shopIsOpen = false;
        SetGameplayUI(true);
        SetBackButton(false);

        // kembalikan kondisi kursor gameplay
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;

        if (pauseGameplay)
            Time.timeScale = previousTimeScale;
    }

    private void SetGameplayUI(bool state)
    {
        foreach (GameObject ui in gameplayUI)
        {
            if (ui != null)
                ui.SetActive(state);
        }
    }

    private void SetBackButton(bool state)
    {
        if (BackButton == null)
            return;

        // ambil atau buat canvas khusus
        backButtonCanvas = BackButton.GetComponent<Canvas>();

        if (backButtonCanvas == null)
            backButtonCanvas = BackButton.AddComponent<Canvas>();

        // prioritaskan canvas
        backButtonCanvas.overrideSorting = true;
        backButtonCanvas.sortingOrder = 1000;

        // pastikan paling depan dalam parent
        BackButton.transform.SetAsLastSibling();

        BackButton.SetActive(state);
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (pauseGameplay && shopIsOpen)
                Time.timeScale = previousTimeScale;

            Instance = null;
        }
    }
    private void LateUpdate()
    {
        if (shopIsOpen)
        {
            // paksa kursor tetap terlihat selama shop terbuka
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}