using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AttendanceMonitoringSystem
{
    public partial class ReportsControl : UserControl
    {
        Database db = new Database();

        public ReportsControl()
        {
            InitializeComponent();
            dpReportDate.SelectedDate = DateTime.Today;
        }

        private void LoadReport_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                SELECT 
                    s.student_id,
                    s.first_name,
                    s.last_name,
                    s.program,
                    s.year_section,
                    a.status,
                    a.date
                FROM attendance a
                INNER JOIN students s ON s.student_id = a.student_id
                WHERE DATE(a.date) = @date";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@date", dpReportDate.SelectedDate?.Date);

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                ReportGrid.ItemsSource = dt.DefaultView;
            }
        }
    }
}