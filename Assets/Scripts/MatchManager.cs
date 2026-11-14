using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengatur match: round (10 poin), match (2 poin kemenangan), banner, reset round, dan pindah ke scene result.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static bool ShootingEnabled = true;

    public static MatchManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PointManager pointManager;

    [Header("Rules")]
    [SerializeField, Min(1)] private int roundScoreToWin = 10;
    [SerializeField, Min(1)] private int matchWinsToWin = 2;
    [SerializeField, Min(0.1f)] private float roundEndDelay = 3f;
    [SerializeField, Min(0.1f)] private float bannerAnimDuration = 0.4f;
    [SerializeField, Min(0.1f)] private float nextRoundBannerDuration = 2f;

    [Header("Banners")]
    [SerializeField] private RectTransform roundBannerRect;
    [SerializeField] private TMP_Text roundBannerText;
    [SerializeField] private RectTransform nextRoundBannerRect;
    [SerializeField] private TMP_Text nextRoundBannerText;

    [Header("Result")]
    [SerializeField] private string resultSceneName = "Result";

    private int teamAWins;
    private int teamBWins;
    private bool roundEnding;
    private bool matchEnding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ShootingEnabled = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (roundEnding || matchEnding) return;
        if (pointManager == null) pointManager = PointManager.Instance;
        if (pointManager == null) return;

        if (pointManager.TeamAScore >= roundScoreToWin)
            TriggerRoundWin(Team.TeamA);
        else if (pointManager.TeamBScore >= roundScoreToWin)
            TriggerRoundWin(Team.TeamB);
    }

    private void TriggerRoundWin(Team winner)
    {
        roundEnding = true;
        ShootingEnabled = false;

        if (winner == Team.TeamA) teamAWins++;
        else if (winner == Team.TeamB) teamBWins++;

        bool matchWin = (winner == Team.TeamA && teamAWins >= matchWinsToWin) ||
                        (winner == Team.TeamB && teamBWins >= matchWinsToWin);

        string msg = (winner == Team.TeamA) ? "VICTORY" : "DEFEAT";

        StartCoroutine(RoundEndRoutine(winner, matchWin, msg));
    }

    private IEnumerator RoundEndRoutine(Team winner, bool matchWin, string msg)
    {
        // Banner hasil round (smooth dari atas ke tengah).
        yield return ShowBanner(roundBannerRect, roundBannerText, msg, roundEndDelay);

        if (matchWin)
        {
            matchEnding = true;
            // Banner kemenangan final (reuse round banner).
            string finalMsg = (winner == Team.TeamA) ? "VICTORY" : "DEFEAT";
            yield return ShowBanner(roundBannerRect, roundBannerText, finalMsg, roundEndDelay);

            if (!string.IsNullOrEmpty(resultSceneName))
                SceneManager.LoadScene(resultSceneName);
        }
        else
        {
            ResetRound();

            // Banner next round.
            RectTransform rect = nextRoundBannerRect != null ? nextRoundBannerRect : roundBannerRect;
            TMP_Text txt = nextRoundBannerText != null ? nextRoundBannerText : roundBannerText;
            yield return ShowBanner(rect, txt, "NEXT ROUND", nextRoundBannerDuration);

            roundEnding = false;
            ShootingEnabled = true;
        }
    }

    private void ResetRound()
    {
        // Teleport semua combatant ke spawn + reset health.
        foreach (CombatHealth health in FindObjectsByType<CombatHealth>())
        {
            if (RespawnManager.Instance != null)
                RespawnManager.Instance.Respawn(health.gameObject);
        }

        // Reset senjata & uang.
        if (WeaponManager.Instance != null)
            WeaponManager.Instance.ResetInventory();
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.ResetCredits();
        foreach (BotBuyWeapon bot in FindObjectsByType<BotBuyWeapon>())
            bot.ResetInventory();

        // Reset poin round.
        if (pointManager != null)
            pointManager.ResetScores();
        else if (PointManager.Instance != null)
            PointManager.Instance.ResetScores();
    }

    private IEnumerator ShowBanner(RectTransform rect, TMP_Text text, string message, float hold)
    {
        if (rect == null) yield break;

        if (text != null) text.text = message;
        rect.gameObject.SetActive(true);

        Vector2 center = Vector2.zero;
        Vector2 top = new Vector2(0f, Screen.height);

        rect.anchoredPosition = top;

        float t = 0f;
        while (t < bannerAnimDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / bannerAnimDuration);
            rect.anchoredPosition = Vector2.Lerp(top, center, k);
            yield return null;
        }
        rect.anchoredPosition = center;

        yield return new WaitForSeconds(hold);

        // Keluar ke atas.
        t = 0f;
        while (t < bannerAnimDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / bannerAnimDuration);
            rect.anchoredPosition = Vector2.Lerp(center, top, k);
            yield return null;
        }
        rect.anchoredPosition = top;
        rect.gameObject.SetActive(false);
    }
}