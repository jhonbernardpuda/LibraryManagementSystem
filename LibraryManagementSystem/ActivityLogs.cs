using MySql.Data.MySqlClient;

namespace LibraryManagementSystem
{
    public class ActivityLogs  // class name stays the same
    {
        Database db = new Database();

        public void Log(int userId, string action, string detail)
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string sql = "INSERT INTO activity_logs (user_id, action, detail) VALUES (@uid, @a, @d)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@a", action);
                    cmd.Parameters.AddWithValue("@d", detail);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* never crash on logging */ }
        }
    }
}