using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Sistem serialisasi penyimpanan data game (saldo pemain dan daftar item yang dimiliki).
/// Data disimpan ke berkas JSON di Application.persistentDataPath sehingga tetap tersedia
/// setelah game ditutup dan dibuka kembali. Tahan terhadap berkas yang belum ada atau rusak.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "shooter_nerd_save.json";
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, FileName);

    public static GameData currentData = new GameData();

    /// <summary>Muat data dari berkas. Mengembalikan false jika berkas tidak ada atau rusak.</summary>
    public static bool Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                currentData = new GameData();
                return false;
            }

            string json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                currentData = new GameData();
                return false;
            }

            GameData data = JsonUtility.FromJson<GameData>(json);
            currentData = data ?? new GameData();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Gagal membaca berkas penyimpanan, menggunakan data awal. {e.Message}");
            currentData = new GameData();
            return false;
        }
    }

    /// <summary>Simpan data saat ini ke berkas.</summary>
    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(currentData, true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Gagal menyimpan berkas penyimpanan. {e.Message}");
        }
    }

    [Serializable]
    public class GameData
    {
        public int credits = 2500;
        public List<string> ownedWeaponNames = new List<string>();
        public string equippedWeaponName = string.Empty;
    }
}
