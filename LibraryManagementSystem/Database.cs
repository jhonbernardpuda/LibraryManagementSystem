using MySql.Data.MySqlClient;

namespace LibraryManagementSystem
{
    public class Database
    {
        private string server = "localhost";
        private string database = "library_db";
        private string uid = "root";
        private string password = "";

        public MySqlConnection GetConnection()
        {
            string connStr = $"Server={server};Database={database};Uid={uid};Pwd={password};";
            return new MySqlConnection(connStr);
        }
    }
}