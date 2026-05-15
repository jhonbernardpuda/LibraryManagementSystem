using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class BooksControl : UserControl
    {
        Database db = new Database();
        private int editingId = -1; // -1 means adding, >0 means editing

        public BooksControl()
        {
            InitializeComponent();
            LoadBooks();
        }

        // ── Load / Search ────────────────────────────────────────────
        private void LoadBooks(string search = "")
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT id, title, author, genre,
                                            total_copies, available_copies
                                     FROM books
                                     WHERE title LIKE @s
                                        OR author LIKE @s
                                        OR genre LIKE @s
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
            txtTitle.Text = "";
            txtAuthor.Text = "";
            txtGenre.Text = "";
            txtCopies.Text = "1";
        }

        // ── Add ──────────────────────────────────────────────────────
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            editingId = -1;
            ShowForm("Add Book");
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
                    string query = "SELECT * FROM books WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        editingId = id;
                        txtTitle.Text = reader["title"].ToString();
                        txtAuthor.Text = reader["author"].ToString();
                        txtGenre.Text = reader["genre"].ToString();
                        txtCopies.Text = reader["total_copies"].ToString();
                        reader.Close();

                        ShowForm("Edit Book");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading book:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Save (Add or Edit) ───────────────────────────────────────
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string title = txtTitle.Text.Trim();
            string author = txtAuthor.Text.Trim();
            string genre = txtGenre.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(author))
            {
                MessageBox.Show("Title and Author are required.", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCopies.Text, out int copies) || copies < 1)
            {
                MessageBox.Show("Please enter a valid number of copies.", "Validation",
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
                        string query = @"INSERT INTO books
                                            (title, author, genre, total_copies, available_copies)
                                         VALUES (@t, @a, @g, @tc, @tc)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@t", title);
                        cmd.Parameters.AddWithValue("@a", author);
                        cmd.Parameters.AddWithValue("@g", genre);
                        cmd.Parameters.AddWithValue("@tc", copies);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Book Added",
                            $"\"{title}\" by {author} added.");
                    }
                    else
                    {
                        // EDIT
                        string query = @"UPDATE books
                                         SET title = @t, author = @a,
                                             genre = @g, total_copies = @tc
                                         WHERE id = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@t", title);
                        cmd.Parameters.AddWithValue("@a", author);
                        cmd.Parameters.AddWithValue("@g", genre);
                        cmd.Parameters.AddWithValue("@tc", copies);
                        cmd.Parameters.AddWithValue("@id", editingId);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Book Updated",
                            $"\"{title}\" updated.");
                    }
                }

                HideForm();
                LoadBooks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving book:\n" + ex.Message, "Error",
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
                "Are you sure you want to delete this book?",
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
                        string getTitle = "SELECT title FROM books WHERE id = @id";
                        MySqlCommand getCmd = new MySqlCommand(getTitle, conn);
                        getCmd.Parameters.AddWithValue("@id", id);
                        string bookTitle = getCmd.ExecuteScalar()?.ToString();

                        string query = "DELETE FROM books WHERE id = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();

                        new ActivityLogs().Log(Login.CurrentUserId, "Book Deleted",
                            $"\"{bookTitle}\" deleted.");
                    }
                    LoadBooks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting book:\n" + ex.Message, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}