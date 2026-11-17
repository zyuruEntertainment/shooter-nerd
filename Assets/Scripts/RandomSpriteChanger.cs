using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan sprite secara acak dan otomatis berganti setiap interval waktu tertentu.
/// </summary>
public class RandomSpriteChanger : MonoBehaviour
{
    [Header("Daftar Sprite")]
    [Tooltip("List sprite yang akan ditampilkan secara acak.")]
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();

    [Header("Referensi Sprite")]
    [Tooltip("SpriteRenderer target (untuk sprite di dunia 3D/2D).")]
    [SerializeField] private SpriteRenderer spriteReference;
    [Tooltip("Image target (untuk UI).")]
    [SerializeField] private Image imageReference;

    [Header("Waktu Ganti")]
    [Tooltip("Interval waktu (detik) antar pergantian sprite.")]
    [SerializeField, Min(0.01f)] private float timeChange = 1f;

    [Header("Opsi")]
    [SerializeField] private bool changeOnStart = true;

    private float timer;

    private void Start()
    {
        if (changeOnStart)
            ChangeSprite();
    }

    private void Update()
    {
        if (sprites == null || sprites.Count == 0)
            return;

        timer += Time.deltaTime;
        if (timer >= timeChange)
        {
            timer = 0f;
            ChangeSprite();
        }
    }

    /// <summary>Mengganti sprite ke salah satu sprite acak dari list.</summary>
    public void ChangeSprite()
    {
        if (sprites == null || sprites.Count == 0)
            return;

        Sprite chosen = sprites[Random.Range(0, sprites.Count)];
        if (spriteReference != null)
            spriteReference.sprite = chosen;
        if (imageReference != null)
            imageReference.sprite = chosen;
    }
}