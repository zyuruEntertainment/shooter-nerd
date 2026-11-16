using System;
using System.Collections;
using System.Linq;
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
    [SerializeField] private RectTransform winBannerRect;
    [SerializeField] private RectTransform loseBannerRect;
    [SerializeField] private RectTransform nextRoundBannerRect;
    [SerializeField] private TMP_Text roundText;

    [Header("Result")]
    [SerializeField] private string resultSceneName = "Result";

    private int teamAWins;
    private int teamBWins;
    private bool roundEnding;
    private bool matchEnding;
    private int currentRound = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ShootingEnabled = true;
        CombatStats.ResetPlayerStats();

        // Sembunyikan banner di awal.
        if (winBannerRect != null) winBannerRect.gameObject.SetActive(false);
        if (loseBannerRect != null) loseBannerRect.gameObject.SetActive(false);
        if (nextRoundBannerRect != null) nextRoundBannerRect.gameObject.SetActive(false);
    }

    private void Start()
    {
        UpdateRoundText();
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
        RectTransform resultBanner = (winner == Team.TeamA) ? winBannerRect : loseBannerRect;
        string bannerMsg = (winner == Team.TeamA) ? "VICTORY" : "DEFEAT";

        // Banner win/lose selama 3 detik.
        yield return ShowBanner(resultBanner, bannerMsg, roundEndDelay);

        if (matchWin)
        {
            matchEnding = true;
            // Banner final (win/lose) lalu pindah scene result.
            yield return ShowBanner(resultBanner, bannerMsg, roundEndDelay);

            if (!string.IsNullOrEmpty(resultSceneName))
            {
                // Pastikan semua sistem pause/shop sudah dalam kondisi normal sebelum pindah scene.
                if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                {
                    Debug.Log("[MatchManager] PauseManager sedang pause, memanggil Resume() sebelum pindah scene.");
                    PauseManager.Instance.Resume();
                }

                if (GameplayUIManager.Instance != null && GameplayUIManager.Instance.IsShopOpen)
                {
                    Debug.Log("[MatchManager] GameplayUIManager shop terbuka, memanggil CloseShop() sebelum pindah scene.");
                    GameplayUIManager.Instance.CloseShop();
                }

                // Pastikan waktu tidak dalam kondisi freeze saat pindah scene.
                float timeScaleBefore = Time.timeScale;
                Time.timeScale = 1f;
                Debug.Log($"[MatchManager] timeScale sebelum pindah Result: {timeScaleBefore}, diatur ke 1f.");

                SimpanHasilAkhir();
                Debug.Log($"[MatchManager] Memuat scene Result via Loading: {resultSceneName}");
                LoadingScreen.Load(resultSceneName);
            }
            else
            {
                Debug.LogWarning("[MatchManager] resultSceneName kosong, tidak dapat memuat scene Result.");
            }
        }
        else
        {
            ResetRound();
            currentRound++;
            UpdateRoundText();

            // Banner next round.
            yield return ShowBanner(nextRoundBannerRect, "NEXT ROUND", nextRoundBannerDuration);

            roundEnding = false;
            ShootingEnabled = true;
        }
    }

    private void UpdateRoundText()
    {
        if (roundText != null)
            roundText.text = $"Round {currentRound}";
    }

    /// <summary>
    /// Ambil statistik akhir pemain (berdasarkan tag "player") dan simpan ke ResultData
    /// agar dapat ditampilkan oleh ResultStatsUI di scene Result.
    /// </summary>
    private void SimpanHasilAkhir()
    {
        Debug.Log("[MatchManager] SimpanHasilAkhir dipanggil.");

        CombatHealth playerHealth = null;

        // Cari CombatHealth dengan tag "player" (case-insensitive karena Unity tag case-sensitive).
        var allHealth = FindObjectsByType<CombatHealth>();
        Debug.Log($"[MatchManager] Ditemukan {allHealth.Length} CombatHealth di scene.");

        // 1) Utamakan CombatHealth yang memiliki ArcadeWeaponController dengan autoFireFromInput (player) — paling andal.
        foreach (CombatHealth h in allHealth)
        {
            var awc = h.GetComponent<ArcadeWeaponController>() ?? h.GetComponentInParent<ArcadeWeaponController>() ?? h.GetComponentInChildren<ArcadeWeaponController>();
            if (awc != null && awc.AutoFireFromInput)
            {
                playerHealth = h;
                Debug.Log($"[MatchManager] Pemain ditemukan via ArcadeWeaponController.AutoFireFromInput pada {h.gameObject.name} (tag={h.tag})");
                break;
            }
        }

        // 2) Fallback: tag Player (case-insensitive).
        if (playerHealth == null)
        {
            foreach (CombatHealth h in allHealth)
            {
                if (string.Equals(h.tag, "player", StringComparison.OrdinalIgnoreCase))
                {
                    playerHealth = h;
                    Debug.Log($"[MatchManager] Ditemukan pemain dengan tag '{h.tag}' pada {h.gameObject.name}");
                    break;
                }
            }
        }

        // 3) Fallback: CombatHealth yang memiliki CombatStats dengan tag Player.
        if (playerHealth == null)
        {
            foreach (CombatHealth h in allHealth)
            {
                CombatStats s = CombatStats.FindFor(h);
                if (s != null && string.Equals(s.gameObject.tag, "player", StringComparison.OrdinalIgnoreCase))
                {
                    playerHealth = h;
                    Debug.Log($"[MatchManager] Fallback player via CombatStats tag Player pada {h.gameObject.name}");
                    break;
                }
            }
        }

        // 4) Fallback terakhir: CombatHealth pertama yang memiliki CombatStats.
        if (playerHealth == null)
        {
            foreach (CombatHealth h in allHealth)
            {
                if (CombatStats.FindFor(h) != null)
                {
                    playerHealth = h;
                    Debug.Log($"[MatchManager] Fallback player via CombatStats pada {h.gameObject.name} (tag={h.tag})");
                    break;
                }
            }
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<CombatHealth>();
            if (playerHealth != null)
                Debug.Log($"[MatchManager] Fallback CombatHealth pertama: {playerHealth.gameObject.name} (tag={playerHealth.tag})");
            else
                Debug.LogWarning("[MatchManager] Tidak ada CombatHealth sama sekali di scene!");
        }

        int kills = 0;
        int deaths = 0;

        if (playerHealth != null)
        {
            CombatStats stats = CombatStats.FindFor(playerHealth);

            if (stats != null)
            {
                kills = stats.Kills;
                deaths = stats.Deaths;
                Debug.Log($"[MatchManager] Ditemukan CombatStats pada {stats.gameObject.name}: Kill={kills}, Death={deaths}");
            }
            else
            {
                Debug.LogWarning("[MatchManager] Pemain tidak memiliki component CombatStats (tidak ada di objek itu, parent, atau child)!");
                var components = playerHealth.GetComponents<Component>();
                Debug.Log($"[MatchManager] Komponen pada {playerHealth.gameObject.name}: {string.Join(", ", components.Select(c => c.GetType().Name))}");
            }
        }

        // Gunakan akumulasi statik pemain jika lebih lengkap dari instance (mencegah result yang tidak sesuai).
        if (CombatStats.PlayerKills > 0 || CombatStats.PlayerDeaths > 0)
        {
            if (kills < CombatStats.PlayerKills || deaths < CombatStats.PlayerDeaths || (kills == 0 && deaths == 0))
            {
                kills = Mathf.Max(kills, CombatStats.PlayerKills);
                deaths = Mathf.Max(deaths, CombatStats.PlayerDeaths);
                Debug.Log($"[MatchManager] Menggunakan akumulasi statik pemain: Kill={kills}, Death={deaths}");
            }
        }

        string grade = ResultStatsUI.HitungGrade(kills, deaths);
        ResultData.SetResult(kills, deaths, grade);

        Debug.Log($"[MatchManager] Hasil akhir disimpan ke ResultData: Kill={kills}, Death={deaths}, Grade={grade}");
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

    private IEnumerator ShowBanner(RectTransform rect, string message, float hold)
    {
        if (rect == null) yield break;

        // Set text jika ada TMP_Text di dalam banner.
        TMP_Text text = rect.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = message;

        rect.gameObject.SetActive(true);

        Vector2 center = Vector2.zero;
        Vector2 top = new Vector2(0f, Screen.height);

        rect.anchoredPosition = top;

        float t = 0f;
        while (t < bannerAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / bannerAnimDuration);
            rect.anchoredPosition = Vector2.Lerp(top, center, k);
            yield return null;
        }
        rect.anchoredPosition = center;

        yield return new WaitForSecondsRealtime(hold);

        // Keluar ke atas.
        t = 0f;
        while (t < bannerAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / bannerAnimDuration);
            rect.anchoredPosition = Vector2.Lerp(center, top, k);
            yield return null;
        }
        rect.anchoredPosition = top;
        rect.gameObject.SetActive(false);
    }
}

