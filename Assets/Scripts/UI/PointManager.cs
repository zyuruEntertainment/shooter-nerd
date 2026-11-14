using TMPro;
using UnityEngine;

/// <summary>
/// Menampilkan dan mengelola poin tim A dan tim B.
/// Setiap kill menambah poin sesuai tim pembunuh.
/// </summary>
public class PointManager : MonoBehaviour
{
    public static PointManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text teamAPointsText;
    [SerializeField] private TMP_Text teamBPointsText;

    [Header("Settings")]
    [SerializeField, Min(1)] private int pointsPerKill = 1;

    private int teamAPoints;
    private int teamBPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Tambah poin untuk tim pembunuh.</summary>
    public void AddKill(Team killerTeam)
    {
        if (killerTeam == Team.TeamA)
            teamAPoints += pointsPerKill;
        else if (killerTeam == Team.TeamB)
            teamBPoints += pointsPerKill;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (teamAPointsText != null)
            teamAPointsText.text = teamAPoints.ToString();

        if (teamBPointsText != null)
            teamBPointsText.text = teamBPoints.ToString();
    }
}