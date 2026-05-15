using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class BookSearchWindow : Window
    {
        Database db = new Database();

        public BookSearchWindow()
        {
            InitializeComponent();
            LoadBooks();
        }

        private void LoadBooks(string search = "")
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT title, author, genre,
                                            total_copies, available_copies
                                     FROM books
                                     WHERE title  LIKE @s
                                        OR author LIKE @s
                                        OR genre  LIKE @s
                                     ORDER BY title";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@s", $"%{search}%");
                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(cmd).Fill(dt);
                    dgBooks.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading books:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadBooks(txtSearch.Text.Trim());
        }
    }
}