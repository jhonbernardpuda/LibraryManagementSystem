using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class BorrowersControl : UserControl
    {
        Database db = new Database();
        private int editingId = -1;

        public BorrowersControl()
        {
            InitializeComponent();
            LoadBorrowers();
        }

        // ── Load / Search ────────────────────────────────────────────
        private void LoadBorrowers(string search = "")
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT id, user_id, type, full_name, course,
                                            year_level, email, is_active
                                     FROM borrowers
                                     WHERE full_name  LIKE @s
                                        OR user_id    LIKE @s
                                        OR course     LIKE @s
                                     ORDER BY type, full_name";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@s", $"%{search}%");

                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(cmd).Fill(dt);
                    dgBorrowers.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading borrowers:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadBorrowers(txtSearch.Text.Trim());
        }

        // ── Show / Hide form ─────────────────────────────────────────
        private void ShowForm(string title)
        {
            lblFormTitle.Text = title;
            pnlForm.Visibility = Visibility.Visible;
            btnAdd.IsEnabled = false;
        }

        private void HideForm()
        {
            pnlForm.Visibility = Visibility.Collapsed;
            btnAdd.IsEnabled = true;
            editingId = -1;
            txtUserId.Text = "";
            txtFullName.Text = "";
            txtCourse.Text = "";
            cmbType.SelectedIndex = 0;
            cmbYearLevel.SelectedIndex = -1;
            pnlYearLevel.Visibility = Visibility.Visible;
            txtEmail.Text = "";
        }

        // ── Add ──────────────────────────────────────────────────────
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            editingId = -1;
            ShowForm("Add Borrower");
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
                    string query = "SELECT * FROM borrowers WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        editingId = id;
                        txtUserId.Text = reader["user_id"].ToString();
                        txtFullName.Text = reader["full_name"].ToString();
                        txtCourse.Text = reader["course"].ToString();
                        txtEmail.Text = reader["email"].ToString();

                        // Match type to ComboBox
                        string type = reader["type"].ToString();
                        foreach (ComboBoxItem item in cmbType.Items)
                        {
                            if (item.Content.ToString() == type)
                            {
                                cmbType.SelectedItem = item;
                                break;
                            }
                        }

                        // Show or hide year level based on type
                        pnlYearLevel.Visibility = type == "Student"
                            ? Visibility.Visible
                            : Visibility.Collapsed;

                        // Match year level to ComboBox (only if Student)
                        if (type == "Student")
                        {
                            string yl = reader["year_level"].ToString();
                            foreach (ComboBoxItem item in cmbYearLevel.Items)
                            {
                                if (item.Content.ToString() == yl)
                                {
                                    cmbYearLevel.SelectedItem = item;
                                    break;
                                }
                            }
                        }

                        reader.Close();
                        ShowForm("Edit Borrower");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading borrower:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Save (Add or Edit) ───────────────────────────────────────
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string userId = txtUserId.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string course = txtCourse.Text.Trim();
            string type = (cmbType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Student";
            string yearLevel = type == "Student" ? (cmbYearLevel.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "": "";
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("User ID and Full Name are required.", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    if (editingId == -1)
                    {
                        // ADD
                        string query = @"INSERT INTO borrowers
                                            (user_id, type, full_name, course, year_level, email)
                                         VALUES (@uid, @type, @n, @c, @yl, @e)";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@n", fullName);
                        cmd.Parameters.AddWithValue("@c", course);
                        cmd.Parameters.AddWithValue("@yl", yearLevel);
                        cmd.Parameters.AddWithValue("@e", email);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Borrower Added",
                            $"{fullName} ({userId}) registered.");
                    }
                    else
                    {
                        // EDIT
                        string query = @"UPDATE borrowers
                                     SET user_id    = @uid,
                                         type       = @type,
                                         full_name  = @n,
                                         course     = @c,
                                         year_level = @yl,
                                         email      = @e
                                     WHERE id = @id";

                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@n", fullName);
                        cmd.Parameters.AddWithValue("@c", course);
                        cmd.Parameters.AddWithValue("@yl", yearLevel);
                        cmd.Parameters.AddWithValue("@e", email);
                        cmd.Parameters.AddWithValue("@id", editingId);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Borrower Updated",
                            $"{fullName} ({userId}) updated.");
                    }
                }

                HideForm();
                LoadBorrowers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving borrower:\n" + ex.Message, "Error",
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

            var result = MessageBox.Show(
                "Are you sure you want to delete this borrower?",
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
                        string getName = "SELECT full_name, user_id FROM borrowers WHERE id = @id";
                        MySqlCommand getCmd = new MySqlCommand(getName, conn);
                        getCmd.Parameters.AddWithValue("@id", id);
                        MySqlDataReader r = getCmd.ExecuteReader();
                        string name = "", uid = "";
                        if (r.Read()) { name = r["full_name"].ToString(); uid = r["user_id"].ToString(); }
                        r.Close();

                        string query = "DELETE FROM borrowers WHERE id = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Borrower Deleted",
                            $"{name} ({uid}) deleted.");
                    }
                    LoadBorrowers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting borrower:\n" + ex.Message, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pnlYearLevel == null) return;
            string type = (cmbType.SelectedItem as ComboBoxItem)?.Content.ToString();
            pnlYearLevel.Visibility = type == "Student" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}