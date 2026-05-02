using MySql.Data.MySqlClient;
using System;
using System.Windows;

namespace AttendanceMonitoringSystem
{
    public partial class Login : Window
    {
        Database db = new Database();

        public static string CurrentUser;
        public static string CurrentRole;

        public Login()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Password.Trim();

            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM users WHERE username=@user AND password=@pass";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", username);
                cmd.Parameters.AddWithValue("@pass", password);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    CurrentUser = reader["username"].ToString();
                    CurrentRole = reader["role"].ToString();

                    MainWindow main = new MainWindow();
                    main.Show();

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password");
                }
            }
        }
    }
}