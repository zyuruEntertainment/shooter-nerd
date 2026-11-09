using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("Gameplay UI")]
    [SerializeField] private List<GameObject> gameplayUI = new List<GameObject>();

    [Header("Shop Prefab")]
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private Transform shopParent;

    [Header("Settings")]
    [SerializeField] private bool pauseGameplay = true;

    private GameObject shopInstance;
    private bool shopIsOpen;
    private float previousTimeScale = 1f;

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

        if (keyboard.bKey.wasPressedThisFrame)
            OpenShop();

        if (keyboard.escapeKey.wasPressedThisFrame)
            CloseShop();
    }

    public void OpenShop()
    {
        if (shopIsOpen)
            return;

        if (shopPrefab == null)
        {
            Debug.LogError("Shop Prefab belum diatur!");
            return;
        }

        shopIsOpen = true;

        SetGameplayUI(false);

        if (pauseGameplay)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        shopInstance = Instantiate(
            shopPrefab,
            shopParent,
            false
        );

        shopInstance.SetActive(true);

        Debug.Log("Shop UI berhasil dibuka!");
    }

    public void CloseShop()
    {
        if (!shopIsOpen)
            return;

        if (shopInstance != null)
        {
            Destroy(shopInstance);
            shopInstance = null;
        }

        shopIsOpen = false;

        SetGameplayUI(true);

        if (pauseGameplay)
            Time.timeScale = previousTimeScale;

        Debug.Log("Shop UI berhasil ditutup!");
    }

    private void SetGameplayUI(bool state)
    {
        foreach (GameObject ui in gameplayUI)
        {
            if (ui != null)
                ui.SetActive(state);
        }
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
}