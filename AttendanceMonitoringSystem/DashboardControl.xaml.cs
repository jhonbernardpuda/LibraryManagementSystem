using System.Windows;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using System.Windows.Controls;

namespace AttendanceMonitoringSystem
{
    public partial class DashboardControl : UserControl
    {
        Database db = new Database();

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            LoadTotalStudents();
            LoadTodayAttendance();
        }

        // TOTAL STUDENTS
        private void LoadTotalStudents()
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT COUNT(*) FROM students";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                lblTotal.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString();
            }
        }

        // PRESENT & ABSENT ONLY
        private void LoadTodayAttendance()
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT status 
                    FROM attendance 
                    WHERE DATE(date) = CURDATE()";

                MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                int present = dt.AsEnumerable()
                                .Count(r => r["status"].ToString() == "Present");

                int absent = dt.AsEnumerable()
                               .Count(r => r["status"].ToString() == "Absent");

                lblPresent.Text = present.ToString();
                lblAbsent.Text = absent.ToString();
            }
        }
    }
}