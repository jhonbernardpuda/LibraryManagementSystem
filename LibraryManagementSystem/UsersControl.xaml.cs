using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class UsersControl : UserControl
    {
        Database db = new Database();
        private int editingId = -1;
        private bool isChangingPassword = false;

        public UsersControl()
        {
            InitializeComponent();
            LoadUsers();
        }

        // ── Load / Search ────────────────────────────────────────────
        private void LoadUsers(string search = "")
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT id, full_name, username, role, is_active
                                     FROM users
                                     WHERE full_name LIKE @s OR username LIKE @s
                                     ORDER BY role, full_name";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@s", $"%{search}%");
                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(cmd).Fill(dt);
                    dgUsers.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadUsers(txtSearch.Text.Trim());
        }

        // ── Show / Hide form ─────────────────────────────────────────
        private void ShowForm(string title)
        {
            lblFormTitle.Text = title;
            pnlForm.Visibility = Visibility.Visible;
            btnAdd.IsEnabled = false;
            isChangingPassword = false;

            // Show/hide fields based on mode
            txtFullName.IsEnabled = true;
            txtUsername.IsEnabled = true;
            txtPassword.IsEnabled = true;
            cmbRole.IsEnabled = true;
            lblPassword.Text = editingId == -1 ? "Password" : "New password (optional)";
        }

        private void ShowChangePasswordForm(int id, string name)
        {
            editingId = id;
            isChangingPassword = true;
            lblFormTitle.Text = $"Change password — {name}";
            txtFullName.Text = "";
            txtUsername.Text = "";
            txtPassword.Password = "";
            cmbRole.SelectedIndex = -1;

            txtFullName.IsEnabled = false;
            txtUsername.IsEnabled = false;
            cmbRole.IsEnabled = false;
            txtPassword.IsEnabled = true;
            lblPassword.Text = "New password";

            pnlForm.Visibility = Visibility.Visible;
            btnAdd.IsEnabled = false;
        }

        private void HideForm()
        {
            pnlForm.Visibility = Visibility.Collapsed;
            btnAdd.IsEnabled = true;
            editingId = -1;
            isChangingPassword = false;
            txtFullName.Text = "";
            txtUsername.Text = "";
            txtPassword.Password = "";
            cmbRole.SelectedIndex = -1;
        }

        // ── Add ──────────────────────────────────────────────────────
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            editingId = -1;
            ShowForm("Add User");
        }

        // ── Edit ─────────────────────────────────────────────────────
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);

            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM users WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        editingId = id;
                        txtFullName.Text = reader["full_name"].ToString();
                        txtUsername.Text = reader["username"].ToString();
                        txtPassword.Password = "";

                        string role = reader["role"].ToString();
                        foreach (ComboBoxItem item in cmbRole.Items)
                            if (item.Content.ToString() == role)
                            { cmbRole.SelectedItem = item; break; }

                        reader.Close();
                        ShowForm("Edit User");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading user:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Change Password ──────────────────────────────────────────
        private void btnChangePass_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);

            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT full_name FROM users WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    string name = cmd.ExecuteScalar()?.ToString();
                    ShowChangePasswordForm(id, name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Save ─────────────────────────────────────────────────────
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    if (isChangingPassword)
                    {
                        // Change password only
                        string newPass = txtPassword.Password.Trim();
                        if (string.IsNullOrEmpty(newPass))
                        {
                            MessageBox.Show("Please enter a new password.", "Validation",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        string query = "UPDATE users SET password = @p WHERE id = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@p", newPass);
                        cmd.Parameters.AddWithValue("@id", editingId);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Password Changed",
                            $"Password changed for user ID {editingId}.");
                    }
                    else if (editingId == -1)
                    {
                        // ADD
                        string fullName = txtFullName.Text.Trim();
                        string username = txtUsername.Text.Trim();
                        string password = txtPassword.Password.Trim();
                        string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

                        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
                        {
                            MessageBox.Show("All fields are required.", "Validation",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        string query = @"INSERT INTO users (full_name, username, password, role)
                                         VALUES (@n, @u, @p, @r)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@n", fullName);
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", password);
                        cmd.Parameters.AddWithValue("@r", role);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "User Added",
                            $"{fullName} ({role}) added.");
                    }
                    else
                    {
                        // EDIT
                        string fullName = txtFullName.Text.Trim();
                        string username = txtUsername.Text.Trim();
                        string password = txtPassword.Password.Trim();
                        string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

                        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                            string.IsNullOrEmpty(role))
                        {
                            MessageBox.Show("Full name, username and role are required.", "Validation",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Update password only if provided
                        string query = string.IsNullOrEmpty(password)
                            ? "UPDATE users SET full_name=@n, username=@u, role=@r WHERE id=@id"
                            : "UPDATE users SET full_name=@n, username=@u, password=@p, role=@r WHERE id=@id";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@n", fullName);
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@r", role);
                        cmd.Parameters.AddWithValue("@id", editingId);
                        if (!string.IsNullOrEmpty(password))
                            cmd.Parameters.AddWithValue("@p", password);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "User Updated",
                            $"{fullName} updated.");
                    }
                }

                HideForm();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving user:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Cancel ───────────────────────────────────────────────────
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            HideForm();
        }

        // ── Delete ───────────────────────────────────────────────────
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);

            if (id == Login.CurrentUserId)
            {
                MessageBox.Show("You cannot delete your own account.", "Not allowed",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to delete this user?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = db.GetConnection())
                    {
                        conn.Open();
                        string getName = "SELECT full_name FROM users WHERE id = @id";
                        MySqlCommand getCmd = new MySqlCommand(getName, conn);
                        getCmd.Parameters.AddWithValue("@id", id);
                        string name = getCmd.ExecuteScalar()?.ToString();

                        string query = "DELETE FROM users WHERE id = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "User Deleted",
                            $"{name} deleted.");
                    }
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting user:\n" + ex.Message, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}