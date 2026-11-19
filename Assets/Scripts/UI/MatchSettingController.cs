using UnityEngine;

/// <summary>
/// Penghubung antara SettingMatch (UI) dan MatchManager.
/// </summary>
public class MatchSettingController : MonoBehaviour
{
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private SettingMatch settingMatch;

    private void Awake()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;
    }

    private void OnEnable()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;
    }

    private System.Action<int, int> changeHandler;

    private void Start()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance;
        if (matchManager == null || settingMatch == null)
            return;

        // Sinkronkan UI dengan nilai awal MatchManager.
        settingMatch.SetValues(matchManager.RoundScoreToWin, matchManager.MatchWinsToWin);

        // Terapkan perubahan UI ke MatchManager.
        changeHandler = (kill, win) => matchManager.SetMatchRules(kill, win);
        settingMatch.OnChanged += changeHandler;
    }

    private void OnDestroy()
    {
        if (settingMatch != null && changeHandler != null)
            settingMatch.OnChanged -= changeHandler;
    }
}