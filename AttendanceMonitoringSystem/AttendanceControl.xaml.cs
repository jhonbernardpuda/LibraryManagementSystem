using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AttendanceMonitoringSystem
{
    public partial class AttendanceControl : UserControl
    {
        Database db = new Database();
        DataTable table = new DataTable();

        public AttendanceControl()
        {
            InitializeComponent();
            dpDate.SelectedDate = DateTime.Today;

            if (Login.CurrentRole == "Student")
            {
                btnSave.Visibility = Visibility.Collapsed;
                AttendanceGrid.IsReadOnly = true;
            }
        }

        private void LoadStudents_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM students";
                MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn);

                table = new DataTable();
                adapter.Fill(table);

                table.Columns.Add("status");

                AttendanceGrid.ItemsSource = table.DefaultView;
            }
        }
        private void Present_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            DataRowView row = (DataRowView)rb.DataContext;

            row["status"] = "Present";
        }

        private void Absent_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            DataRowView row = (DataRowView)rb.DataContext;

            row["status"] = "Absent";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                foreach (DataRow row in table.Rows)
                {
                    if (row["status"] == DBNull.Value) continue;

                    string query =
                    @"INSERT INTO attendance
                    (student_id,date,status)
                    VALUES
                    (@id,@date,@status)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@id", row["student_id"]);
                    cmd.Parameters.AddWithValue("@date", dpDate.SelectedDate);
                    cmd.Parameters.AddWithValue("@status", row["status"]);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Attendance Saved");
            }
        }
    }
}