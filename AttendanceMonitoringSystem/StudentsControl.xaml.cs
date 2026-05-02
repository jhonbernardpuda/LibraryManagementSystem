using MySql.Data.MySqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AttendanceMonitoringSystem
{
    public partial class StudentsControl : UserControl
    {
        Database db = new Database();

        public StudentsControl()
        {
            InitializeComponent();
            LoadStudents();
        }
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"SELECT * FROM students 
                         WHERE student_id LIKE @search
                         OR first_name LIKE @search
                         OR last_name LIKE @search
                         OR program LIKE @search
                         OR year_section LIKE @search";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);

                DataTable table = new DataTable();
                adapter.Fill(table);

                StudentsGrid.ItemsSource = table.DefaultView;
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadStudents();
        }
        private void LoadStudents()
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM students";
                MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn);

                DataTable table = new DataTable();
                adapter.Fill(table);

                StudentsGrid.ItemsSource = table.DefaultView;
            }
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"INSERT INTO students
                (student_id, first_name, last_name, program, year_section)
                VALUES
                (@id, @first, @last, @program, @year)";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", txtID.Text);
                cmd.Parameters.AddWithValue("@first", txtFirst.Text);
                cmd.Parameters.AddWithValue("@last", txtLast.Text);
                cmd.Parameters.AddWithValue("@program", txtProgram.Text);
                cmd.Parameters.AddWithValue("@year", txtYearSection.Text);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Student Added");

                LoadStudents();
            }
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null)
            {
                MessageBox.Show("Select student to delete");
                return;
            }

            DataRowView row = (DataRowView)StudentsGrid.SelectedItem;
            string id = row["student_id"].ToString();

            MessageBoxResult result = MessageBox.Show(
                "Delete selected student?",
                "Confirm",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = "DELETE FROM students WHERE student_id=@id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Student Deleted");

                    LoadStudents();
                }
            }
        }
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"UPDATE students SET 
                        first_name=@first,
                        last_name=@last,
                        program=@program,
                        year_section=@year
                        WHERE student_id=@id";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", txtID.Text);
                cmd.Parameters.AddWithValue("@first", txtFirst.Text);
                cmd.Parameters.AddWithValue("@last", txtLast.Text);
                cmd.Parameters.AddWithValue("@program", txtProgram.Text);
                cmd.Parameters.AddWithValue("@year", txtYearSection.Text);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Student Updated");

                LoadStudents();
            }
        }
        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null) return;

            DataRowView row = (DataRowView)StudentsGrid.SelectedItem;

            txtID.Text = row["student_id"].ToString();
            txtFirst.Text = row["first_name"].ToString();
            txtLast.Text = row["last_name"].ToString();
            txtProgram.Text = row["program"].ToString();
            txtYearSection.Text = row["year_section"].ToString();
        }
    }
}