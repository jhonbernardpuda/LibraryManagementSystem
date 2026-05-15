using MySql.Data.MySqlClient;
using System;

namespace LibraryManagementSystem
{
    public static class Settings
    {
        private static Database db = new Database();

        public static decimal GetPenaltyRate()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT value FROM settings WHERE name = 'penalty_rate'";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    object result = cmd.ExecuteScalar();
                    if (result != null && decimal.TryParse(result.ToString(), out decimal rate))
                        return rate;
                }
            }
            catch { }
            return 5.00m; // fallback
        }

        public static void SetPenaltyRate(decimal rate)
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO settings (name, value) VALUES ('penalty_rate', @v)
                                     ON DUPLICATE KEY UPDATE value = @v";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@v", rate.ToString("F2"));
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }
}