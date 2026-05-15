using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class BorrowControl : UserControl
    {
        Database db = new Database();
        private string currentTab = "active";

        public BorrowControl()
        {
            InitializeComponent();
            dpDueDate.SelectedDate = DateTime.Today.AddDays(14);
            LoadTransactions();
        }

        // ── Load transactions ────────────────────────────────────────
        private void LoadTransactions()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    // Auto-update overdue status first
                    string updateOverdue = @"UPDATE borrow_transactions
                                             SET status = 'overdue'
                                             WHERE return_date IS NULL
                                               AND due_date < CURDATE()
                                               AND status = 'borrowed'";
                    new MySqlCommand(updateOverdue, conn).ExecuteNonQuery();

                    string where = currentTab == "active"
                        ? "WHERE bt.status IN ('borrowed','overdue')"
                        : "WHERE bt.status = 'returned'";

                    string query = $@"SELECT bt.id, br.full_name, br.user_id,
                                             b.title, bt.borrow_date, bt.due_date,
                                             bt.return_date, bt.status
                                      FROM borrow_transactions bt
                                      JOIN borrowers br ON bt.borrower_id = br.id
                                      JOIN books b      ON bt.book_id     = b.id
                                      {where}
                                      ORDER BY bt.borrow_date DESC";

                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(query, conn).Fill(dt);
                    dgTransactions.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading transactions:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Tab switching ────────────────────────────────────────────
        private void btnTabActive_Click(object sender, RoutedEventArgs e)
        {
            currentTab = "active";
            btnTabActive.Background = System.Windows.Media.Brushes.Transparent;
            btnTabActive.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#178AE5"));

            // Active tab styling
            btnTabActive.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#178AE5"));
            btnTabActive.Foreground = System.Windows.Media.Brushes.White;
            btnTabReturned.Background = System.Windows.Media.Brushes.White;
            btnTabReturned.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280"));

            HideForm();
            LoadTransactions();
        }

        private void btnTabReturned_Click(object sender, RoutedEventArgs e)
        {
            currentTab = "returned";
            btnTabReturned.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#178AE5"));
            btnTabReturned.Foreground = System.Windows.Media.Brushes.White;
            btnTabActive.Background = System.Windows.Media.Brushes.White;
            btnTabActive.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280"));

            HideForm();
            LoadTransactions();
        }

        // ── Show / Hide form ─────────────────────────────────────────
        private void ShowForm()
        {
            LoadStudentsCombo();
            LoadBooksCombo();
            dpDueDate.SelectedDate = DateTime.Today.AddDays(14);
            pnlForm.Visibility = Visibility.Visible;
            btnNewBorrow.IsEnabled = false;
        }

        private void HideForm()
        {
            pnlForm.Visibility = Visibility.Collapsed;
            btnNewBorrow.IsEnabled = true;
            cmbStudent.SelectedIndex = -1;
            cmbBook.SelectedIndex = -1;
        }

        private void btnNewBorrow_Click(object sender, RoutedEventArgs e)
        {
            ShowForm();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            HideForm();
        }

        // ── Load ComboBoxes ──────────────────────────────────────────
        private void LoadStudentsCombo()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT id,
                                            CONCAT(full_name, ' (', user_id, ')') AS display
                                     FROM borrowers
                                     WHERE is_active = 1
                                     ORDER BY full_name";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(cmd).Fill(dt);
                    cmbStudent.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading students:\n" + ex.Message);
            }
        }

        private void LoadBooksCombo()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT id,
                                            CONCAT(title, ' (', available_copies, ' available)') AS display
                                     FROM books
                                     WHERE available_copies > 0
                                     ORDER BY title";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    DataTable dt = new DataTable();
                    new MySqlDataAdapter(cmd).Fill(dt);
                    cmbBook.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading books:\n" + ex.Message);
            }
        }

        // ── Confirm borrow ───────────────────────────────────────────
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStudent.SelectedItem == null || cmbBook.SelectedItem == null)
            {
                MessageBox.Show("Please select a student and a book.", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpDueDate.SelectedDate == null || dpDueDate.SelectedDate <= DateTime.Today)
            {
                MessageBox.Show("Please select a valid due date (must be after today).", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int borrowerId = Convert.ToInt32((cmbStudent.SelectedItem as DataRowView)?["id"]);
            int bookId = Convert.ToInt32((cmbBook.SelectedItem as DataRowView)?["id"]);
            DateTime dueDate = dpDueDate.SelectedDate.Value;

            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    MySqlTransaction tx = conn.BeginTransaction();

                    try
                    {
                        // Insert transaction
                        string insert = @"INSERT INTO borrow_transactions
                                            (book_id, borrower_id, processed_by, borrow_date, due_date, status)
                                          VALUES (@bid, @brid, @uid, CURDATE(), @due, 'borrowed')";
                        MySqlCommand cmd = new MySqlCommand(insert, conn, tx);
                        cmd.Parameters.AddWithValue("@bid", bookId);
                        cmd.Parameters.AddWithValue("@brid", borrowerId);
                        cmd.Parameters.AddWithValue("@uid", Login.CurrentUserId);
                        cmd.Parameters.AddWithValue("@due", dueDate.Date);
                        cmd.ExecuteNonQuery();

                        // Decrease available copies
                        string update = "UPDATE books SET available_copies = available_copies - 1 WHERE id = @id";
                        MySqlCommand updCmd = new MySqlCommand(update, conn, tx);
                        updCmd.Parameters.AddWithValue("@id", bookId);
                        updCmd.ExecuteNonQuery();

                        tx.Commit();

                        // Log
                        string studentName = (cmbStudent.SelectedItem as DataRowView)?["display"]?.ToString();
                        string bookName = (cmbBook.SelectedItem as DataRowView)?["display"]?.ToString();
                        new ActivityLogs().Log(Login.CurrentUserId, "Book Borrowed",
                            $"{studentName} borrowed {bookName}. Due: {dueDate:MMM dd, yyyy}");

                        HideForm();
                        LoadTransactions();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing borrow:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Return book ──────────────────────────────────────────────
        private void btnReturn_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);

            var result = MessageBox.Show(
                "Confirm return of this book?",
                "Confirm Return",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = db.GetConnection())
                    {
                        conn.Open();
                        MySqlTransaction tx = conn.BeginTransaction();

                        try
                        {
                            // Get book_id for updating copies
                            string getBookId = "SELECT book_id FROM borrow_transactions WHERE id = @id";
                            MySqlCommand getCmd = new MySqlCommand(getBookId, conn, tx);
                            getCmd.Parameters.AddWithValue("@id", id);
                            int bookId = Convert.ToInt32(getCmd.ExecuteScalar());

                            // Mark returned
                            string update = @"UPDATE borrow_transactions
                                              SET return_date = CURDATE(),
                                                  status = 'returned'
                                              WHERE id = @id";
                            MySqlCommand cmd = new MySqlCommand(update, conn, tx);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();

                            // Increase available copies
                            string restoreBook = "UPDATE books SET available_copies = available_copies + 1 WHERE id = @id";
                            MySqlCommand restoreCmd = new MySqlCommand(restoreBook, conn, tx);
                            restoreCmd.Parameters.AddWithValue("@id", bookId);
                            restoreCmd.ExecuteNonQuery();

                            tx.Commit();

                            new ActivityLogs().Log(Login.CurrentUserId, "Book Returned",
                                $"Transaction #{id} marked as returned.");

                            LoadTransactions();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing return:\n" + ex.Message, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}