using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Input;

namespace LibraryManagementSystem
{
    public partial class Login : Window
    {
        Database db = new Database();
        public static string CurrentUser;
        public static string CurrentRole;
        public static string CurrentFullName;
        public static int CurrentUserId;

        public Login()
        {
            InitializeComponent();

            // Allow pressing Enter on password field to trigger login
            txtPass.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    Login_Click(null, null);
            };

            txtUser.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    txtPass.Focus();
            };
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Password.Trim();

            // Basic validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter your username and password.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    // SHA2 hashed password — matches database_setup.sql
                    string query = "SELECT * FROM users WHERE username = @user AND password = @pass AND is_active = 1";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", username);
                    cmd.Parameters.AddWithValue("@pass", password);

                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        // Store session info
                        CurrentUser = reader["username"].ToString();
                        CurrentRole = reader["role"].ToString();
                        CurrentFullName = reader["full_name"].ToString();
                        int userId = Convert.ToInt32(reader["id"]);
                        CurrentUserId = Convert.ToInt32(reader["id"]);
                        reader.Close();

                        // Log the login activity
                        new ActivityLogs().Log(userId, "Login", $"{CurrentFullName} signed in.");

                        // Open main window
                        MainWindow main = new MainWindow();
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        lblError.Text = "Invalid username or password.";
                        lblError.Visibility = Visibility.Visible;
                        txtPass.Clear();
                        txtPass.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error:\n" + ex.Message, "Connection Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSearchBooks_Click(object sender, RoutedEventArgs e)
        {
            new BookSearchWindow().Show();
        }
    }
}