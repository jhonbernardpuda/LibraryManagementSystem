using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace AttendanceMonitoringSystem
{
    public partial class StudentsControl : UserControl
    {
        Database db = new Database();

        private string _originalStudentId = "";
        public StudentsControl()
        {
            InitializeComponent();
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

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtID.Text) ||
                string.IsNullOrWhiteSpace(txtFirst.Text) ||
                string.IsNullOrWhiteSpace(txtLast.Text) ||
                string.IsNullOrWhiteSpace(txtProgram.Text) ||
                string.IsNullOrWhiteSpace(txtYearSection.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
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

                    MessageBox.Show("Student Added", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadStudents();

                    txtID.Clear();
                    txtFirst.Clear();
                    txtLast.Clear();
                    txtProgram.Clear();
                    txtYearSection.Clear();
                    txtID.Focus();
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtID.Clear();
            txtFirst.Clear();
            txtLast.Clear();
            txtProgram.Clear();
            txtYearSection.Clear();
            txtID.Focus();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null)
            {
                MessageBox.Show("Select a student to delete");
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
        private void StudentsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                StudentsGrid.RowEditEnding -= StudentsGrid_RowEditEnding;
                StudentsGrid.CommitEdit(DataGridEditingUnit.Row, true);
                StudentsGrid.RowEditEnding += StudentsGrid_RowEditEnding;

                DataRowView row = e.Row.Item as DataRowView;
                if (row == null) return;

                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE students SET
                student_id=@newId,
                first_name=@first,
                last_name=@last,
                program=@program,
                year_section=@year
                WHERE student_id=@originalId";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@originalId", _originalStudentId);
                    cmd.Parameters.AddWithValue("@newId", row["student_id"].ToString());
                    cmd.Parameters.AddWithValue("@first", row["first_name"].ToString());
                    cmd.Parameters.AddWithValue("@last", row["last_name"].ToString());
                    cmd.Parameters.AddWithValue("@program", row["program"].ToString());
                    cmd.Parameters.AddWithValue("@year", row["year_section"].ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null) return;

            DataRowView row = (DataRowView)StudentsGrid.SelectedItem;
            _originalStudentId = row["student_id"].ToString();
        }
    }
}