/// <summary>
/// Penyimpanan statistik akhir match (kill, death, grade) yang bertahan antar scene.
/// Digunakan untuk menampilkan result di scene "Result" setelah match berakhir.
/// </summary>
public static class ResultData
{
    public static int Kills { get; private set; }
    public static int Deaths { get; private set; }
    public static string Grade { get; private set; }
    public static bool HasResult { get; private set; }

    public static void SetResult(int kills, int deaths, string grade)
    {
        Kills = kills;
        Deaths = deaths;
        Grade = grade;
        HasResult = true;
    }

    public static void Clear()
    {
        Kills = 0;
        Deaths = 0;
        Grade = "-";
        HasResult = false;
    }
}
