namespace RPGBattleMaker.Infrastructure.Database;

public static class DatabaseConfiguration
{
    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

    public static string ConnectionString
    {
        get
        {
            Directory.CreateDirectory(DataDirectory);
            return $"Data Source={Path.Combine(DataDirectory, "rpg_battle.db")}";
        }
    }
}
