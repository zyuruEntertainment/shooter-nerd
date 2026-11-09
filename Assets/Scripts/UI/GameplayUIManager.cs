using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("Gameplay UI")]
    [SerializeField] private List<GameObject> gameplayUI = new List<GameObject>();

    [Header("Shop Scene")]
    [SerializeField] private string shopSceneName = "BuyMenu";
    [SerializeField] private bool pauseGameplay = true;

    private bool shopIsOpen;
    private bool isLoading;
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
        if (shopIsOpen || isLoading)
            return;

        StartCoroutine(OpenShopRoutine());
    }

    private IEnumerator OpenShopRoutine()
    {
        isLoading = true;

        SetGameplayUI(false);

        if (pauseGameplay)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        Scene shopScene = SceneManager.GetSceneByName(shopSceneName);

        if (!shopScene.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                shopSceneName,
                LoadSceneMode.Additive
            );

            if (operation == null)
            {
                Debug.LogError("Gagal memuat scene " + shopSceneName);
                RestoreGameplay();
                isLoading = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        shopIsOpen = true;
        isLoading = false;
    }

    public void CloseShop()
    {
        if (!shopIsOpen || isLoading)
            return;

        StartCoroutine(CloseShopRoutine());
    }

    private IEnumerator CloseShopRoutine()
    {
        isLoading = true;

        Scene shopScene = SceneManager.GetSceneByName(shopSceneName);

        if (shopScene.isLoaded)
        {
            AsyncOperation operation =
                SceneManager.UnloadSceneAsync(shopSceneName);

            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
            }
        }

        RestoreGameplay();
        isLoading = false;
    }

    private void SetGameplayUI(bool state)
    {
        foreach (GameObject ui in gameplayUI)
        {
            if (ui != null)
                ui.SetActive(state);
        }
    }

    private void RestoreGameplay()
    {
        shopIsOpen = false;

        SetGameplayUI(true);

        if (pauseGameplay)
            Time.timeScale = previousTimeScale;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}