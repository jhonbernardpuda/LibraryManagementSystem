using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;


namespace AttendanceMonitoringSystem
{
    class Database
    {
        private string connectionString =
            "server=localhost;user id=root;password=;database=attendance_db";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
