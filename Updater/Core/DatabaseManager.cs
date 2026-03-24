using Microsoft.Data.SqlClient;

namespace Updater.Core
{
    public class PvpRankEntry
    {
        public string CharName { get; set; } = string.Empty;
        public int Kills { get; set; }
    }

    public static class DatabaseManager
    {
        private const string ConnectionString =
            "Server=158.69.213.250;Database=PS_GameLog;User Id=lotus;Password=$2a$13$wr34crwF1vcXtwE8wDrwtunwg9cKVlZN6lJwOHwhByN.pMMNIljIK;TrustServerCertificate=True;Connect Timeout=5;";

        public static async Task<List<PvpRankEntry>> GetDailyTop5PvpAsync()
        {
            var results = new List<PvpRankEntry>();

            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                const string query = @"
                    SELECT TOP 5 CharName, COUNT(*) as Kills 
                    FROM Kill_Log 
                    WHERE CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE) 
                    GROUP BY CharName 
                    ORDER BY Kills DESC;";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PvpRankEntry
                    {
                        CharName = reader.GetString(0),
                        Kills = reader.GetInt32(1)
                    });
                }
            }
            catch
            {
                // Silently fail - ranking is optional, the Updater must not crash
            }

            return results;
        }
    }
}
