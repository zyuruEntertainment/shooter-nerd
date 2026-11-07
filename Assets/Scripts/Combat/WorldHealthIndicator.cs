using TMPro;
using UnityEngine;

public class WorldHealthIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatHealth combatHealth;
    [SerializeField] private TMP_Text healthText;

    [Header("Settings")]
    [SerializeField, Min(1)] private int barSegments = 10;
    [SerializeField] private bool billboard = true;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        UpdateHealth();
    }

    private void LateUpdate()
    {
        UpdateHealth();

        if (billboard)
            UpdateBillboard();
    }

    private void UpdateBillboard()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - mainCamera.transform.position
        );
    }

    private void UpdateHealth()
    {
        if (combatHealth == null || healthText == null)
            return;

        float maxHealth = combatHealth.MaxHealth;

        if (maxHealth <= 0f)
            return;

        float currentHealth = Mathf.Clamp(
            combatHealth.CurrentHealth,
            0f,
            maxHealth
        );

        float ratio = currentHealth / maxHealth;

        int filled = Mathf.RoundToInt(ratio * barSegments);
        int empty = barSegments - filled;

        string color = ratio > 0.5f
            ? "#55E58A"
            : ratio > 0.25f
                ? "#FFD166"
                : "#FF5C5C";

        string bar =
            $"<color={color}>{new string('■', filled)}</color>" +
            $"<color=#555555>{new string('■', empty)}</color>";

        healthText.text =
            $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}\n{bar}";
    }
}