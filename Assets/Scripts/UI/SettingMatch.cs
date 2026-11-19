using System;
using TMPro;
using UnityEngine;

/// <summary>
/// UI pengaturan match: kill to win dan win to win (input integer).
/// </summary>
public class SettingMatch : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField killToWinInput;
    [SerializeField] private TMP_InputField winToWinInput;

    public int KillToWin { get; private set; } = 10;
    public int WinToWin { get; private set; } = 2;

    public event Action<int, int> OnChanged;

    private void Start()
    {
        if (MatchManager.Instance != null && (KillToWin == 10 && WinToWin == 2))
            SetValues(MatchManager.Instance.RoundScoreToWin, MatchManager.Instance.MatchWinsToWin);
    }

    private const string PrefKillKey = "shooter_nerd_match_kill_to_win";
    private const string PrefWinKey = "shooter_nerd_match_win_to_win";

    private void Awake()
    {
        // Jika input field belum di-assign (mis. karena prefab), coba cari otomatis di scene.
        if (killToWinInput == null || winToWinInput == null)
        {
            var inputs = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
            if (inputs != null && inputs.Length > 0)
            {
                if (killToWinInput == null) killToWinInput = inputs[0];
                if (winToWinInput == null && inputs.Length > 1) winToWinInput = inputs[1];
            }
        }

        if (killToWinInput != null)
            killToWinInput.onValueChanged.AddListener(OnKillEdited);
        if (winToWinInput != null)
            winToWinInput.onValueChanged.AddListener(OnWinEdited);

        // Muat nilai tersimpan (atau default) agar konsisten antar scene.
        int savedKill = PlayerPrefs.GetInt(PrefKillKey, MatchManager.Instance != null ? MatchManager.Instance.RoundScoreToWin : 10);
        int savedWin = PlayerPrefs.GetInt(PrefWinKey, MatchManager.Instance != null ? MatchManager.Instance.MatchWinsToWin : 2);
        SetValues(savedKill, savedWin);
    }

    private void OnDestroy()
    {
        if (killToWinInput != null)
            killToWinInput.onEndEdit.RemoveListener(OnKillEdited);
        if (winToWinInput != null)
            winToWinInput.onEndEdit.RemoveListener(OnWinEdited);
    }

    public void SetValues(int killToWin, int winToWin)
    {
        KillToWin = Mathf.Max(1, killToWin);
        WinToWin = Mathf.Max(1, winToWin);

        if (killToWinInput != null)
            killToWinInput.text = KillToWin.ToString();
        if (winToWinInput != null)
            winToWinInput.text = WinToWin.ToString();
    }

    private void OnKillEdited(string value)
    {
        if (int.TryParse(value, out int parsed))
        {
            KillToWin = Mathf.Max(1, parsed);
            if (killToWinInput != null)
                killToWinInput.text = KillToWin.ToString();
            PlayerPrefs.SetInt(PrefKillKey, KillToWin);
            PlayerPrefs.Save();
            if (MatchManager.Instance != null)
                MatchManager.Instance.SetMatchRules(KillToWin, WinToWin);
            OnChanged?.Invoke(KillToWin, WinToWin);
        }
        else
        {
            if (killToWinInput != null)
                killToWinInput.text = KillToWin.ToString();
        }
    }

    private void OnWinEdited(string value)
    {
        if (int.TryParse(value, out int parsed))
        {
            WinToWin = Mathf.Max(1, parsed);
            if (winToWinInput != null)
                winToWinInput.text = WinToWin.ToString();
            PlayerPrefs.SetInt(PrefWinKey, WinToWin);
            PlayerPrefs.Save();
            if (MatchManager.Instance != null)
                MatchManager.Instance.SetMatchRules(KillToWin, WinToWin);
            OnChanged?.Invoke(KillToWin, WinToWin);
        }
        else
        {
            if (winToWinInput != null)
                winToWinInput.text = WinToWin.ToString();
        }
    }
}