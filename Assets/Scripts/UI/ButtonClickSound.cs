using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Dipasang pada satu GameObject di scene.
/// Otomatis mencari semua Button di scene (termasuk yang muncul belakangan
/// dari prefab) dan memasang proxy pemutar sound pada setiap button.
/// Proxy terpisah dari Button.onClick, sehingga tidak terhapus oleh
/// RemoveAllListeners() yang dipanggil script lain (mis. WeaponShopItemUI).
/// </summary>
public class ButtonClickSound : MonoBehaviour
{
    public static ButtonClickSound Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Default")]
    [SerializeField] private AudioClip sound;

    [Header("Pengecualian (button + sound sendiri)")]
    [SerializeField] private ButtonSoundException[] exceptions = Array.Empty<ButtonSoundException>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
    }

    private void Update()
    {
        // Pasang proxy pada semua button yang ada (termasuk yang baru di-instantiate).
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            AudioClip clipToPlay = sound;
            bool isException = false;

            foreach (ButtonSoundException exception in exceptions)
            {
                if (exception != null && exception.button == button)
                {
                    isException = true;
                    clipToPlay = exception.sound;
                    break;
                }
            }

            if (isException && clipToPlay == null)
                clipToPlay = null; // pengecualian boleh tanpa suara

            if (!isException && sound == null)
                clipToPlay = null;

            ButtonClickSoundProxy proxy = button.GetComponent<ButtonClickSoundProxy>();
            if (proxy == null)
                proxy = button.gameObject.AddComponent<ButtonClickSoundProxy>();

            proxy.clip = clipToPlay;
        }
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        AudioSource source = GetPlayableAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
            return;
        }

        Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, position);
    }

    private AudioSource GetPlayableAudioSource()
    {
        if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            audioSource.volume = 1f;
            return audioSource;
        }

        AudioSource fallback = GetComponent<AudioSource>();
        if (fallback == null)
            fallback = gameObject.AddComponent<AudioSource>();

        fallback.playOnAwake = false;
        fallback.spatialBlend = 0f;
        fallback.volume = 1f;

        if (!fallback.enabled)
            fallback.enabled = true;

        if (audioSource == null)
            audioSource = fallback;

        return fallback;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void PlaySound(AudioClip clip)
    {
        if (Instance != null)
            Instance.PlayClip(clip);
        else
        {
            Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, position);
        }
    }
}

[Serializable]
public class ButtonSoundException
{
    public Button button;
    public AudioClip sound;
}

/// <summary>
/// Proxy pada button yang memutar sound saat diklik.
/// Terpisah dari Button.onClick agar tidak terhapus RemoveAllListeners().
/// </summary>
public class ButtonClickSoundProxy : MonoBehaviour, IPointerClickHandler
{
    public AudioClip clip;

    public void OnPointerClick(PointerEventData eventData)
    {
        ButtonClickSound.PlaySound(clip);
    }
}