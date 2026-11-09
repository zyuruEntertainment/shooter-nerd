using TMPro;
using UnityEngine;

public class CombatStatsUI : MonoBehaviour
{
    [SerializeField] private CombatStats targetStats;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;

    private void OnEnable()
    {
        if (targetStats != null)
            targetStats.OnStatsChanged += UpdateUI;

        UpdateUI();
    }

    private void OnDisable()
    {
        if (targetStats != null)
            targetStats.OnStatsChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (targetStats == null)
            return;

        if (killsText != null)
            killsText.text = $"KILLS: {targetStats.Kills}";

        if (deathsText != null)
            deathsText.text = $"DEATHS: {targetStats.Deaths}";
    }
}