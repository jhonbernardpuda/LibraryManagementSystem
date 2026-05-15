using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class ActivityLogsControl : UserControl
    {
        Database db = new Database();
        private bool isLoaded = false;

        public ActivityLogsControl()
        {
            InitializeComponent();
            isLoaded = true;
            LoadLogs();
        }

        private void LoadLogs(string search = "", string filter = "")
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string where = "WHERE 1=1";

                    if (!string.IsNullOrEmpty(search))
                        where += " AND (al.action LIKE @s OR al.detail LIKE @s)";

                    if (!string.IsNullOrEmpty(filter) && filter != "All Actions")
                        where += " AND al.action = @filter";

                    string query = $@"SELECT al.id,
                                             u.full_name,
                                             al.action,
                                             al.detail,
                                             al.log_time
                                      FROM activity_logs al
                                      JOIN users u ON al.user_id = u.id
                                      {where}
                                      ORDER BY al.log_time DESC
                                      LIMIT 200";

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.AddWithValue("@s", $"%{search}%");

                    if (!string.IsNullOrEmpty(filter) && filter != "All Actions")
                        cmd.Parameters.AddWithValue("@filter", filter);

                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(cmd).Fill(dt);
                    dgLogs.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading logs:\n" + ex.Message, "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isLoaded) return;
            string filter = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            LoadLogs(txtSearch.Text.Trim(), filter);
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            string filter = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            LoadLogs(txtSearch.Text.Trim(), filter);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbFilter.SelectedIndex = 0;
            LoadLogs();
        }
    }
}