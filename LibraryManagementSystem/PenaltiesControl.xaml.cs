using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryManagementSystem
{
    public partial class PenaltiesControl : UserControl
    {
        Database db = new Database();
        private string currentTab = "overdue";

        public PenaltiesControl()
        {
            InitializeComponent();
            txtRate.Text = Settings.GetPenaltyRate().ToString("F2");
            LoadPenalties();
        }

        // ── Load ─────────────────────────────────────────────────────
        private void LoadPenalties()
        {
            decimal rate = Settings.GetPenaltyRate();

            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    // Auto-update overdue status
                    new MySqlCommand(@"UPDATE borrow_transactions
                                       SET status = 'overdue'
                                       WHERE return_date IS NULL
                                         AND due_date < CURDATE()
                                         AND status = 'borrowed'", conn).ExecuteNonQuery();

                    if (currentTab == "overdue")
                        LoadOverdue(conn, rate);
                    else
                        LoadHistory(conn, rate);

                    LoadSummary(conn, rate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading penalties:\n" + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Overdue tab ──────────────────────────────────────────────
        private void LoadOverdue(MySqlConnection conn, decimal rate)
        {
            string query = @"SELECT bt.id,
                                    br.full_name, br.user_id,
                                    bk.title, bt.due_date,
                                    DATEDIFF(CURDATE(), bt.due_date) AS days_overdue,
                                    DATEDIFF(CURDATE(), bt.due_date) * @rate AS penalty
                             FROM borrow_transactions bt
                             JOIN borrowers br ON bt.borrower_id = br.id
                             JOIN books bk     ON bt.book_id     = bk.id
                             WHERE bt.return_date IS NULL
                               AND bt.due_date < CURDATE()
                             ORDER BY bt.due_date ASC";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@rate", rate);
            DataTable dt = new DataTable();
            new MySqlDataAdapter(cmd).Fill(dt);

            foreach (DataRow row in dt.Rows)
                row["penalty"] = $"₱{Convert.ToDecimal(row["penalty"]):F2}";

            dgPenalties.ItemsSource = dt.DefaultView;
            colActions.Visibility = Visibility.Visible;
        }

        // ── History tab ──────────────────────────────────────────────
        private void LoadHistory(MySqlConnection conn, decimal rate)
        {
            string query = @"SELECT bt.id,
                                    br.full_name, br.user_id,
                                    bk.title, bt.due_date,
                                    bt.return_date,
                                    bt.penalty_amount AS penalty
                             FROM borrow_transactions bt
                             JOIN borrowers br ON bt.borrower_id = br.id
                             JOIN books bk     ON bt.book_id     = bk.id
                             WHERE bt.return_date IS NOT NULL
                               AND bt.penalty_amount > 0
                             ORDER BY bt.return_date DESC";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            DataTable dt = new DataTable();
            new MySqlDataAdapter(cmd).Fill(dt);

            foreach (DataRow row in dt.Rows)
                row["penalty"] = $"₱{Convert.ToDecimal(row["penalty"]):F2}";

            dgPenalties.ItemsSource = dt.DefaultView;
            colActions.Visibility = Visibility.Collapsed;
        }

        // ── Summary cards ────────────────────────────────────────────
        private void LoadSummary(MySqlConnection conn, decimal rate)
        {
            MySqlCommand cmd = new MySqlCommand(
                @"SELECT COUNT(*),
                         SUM(DATEDIFF(CURDATE(), due_date) * @rate)
                  FROM borrow_transactions
                  WHERE return_date IS NULL AND due_date < CURDATE()", conn);
            cmd.Parameters.AddWithValue("@rate", rate);
            MySqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                int count = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetInt64(0));
                decimal total = r.IsDBNull(1) ? 0 : r.GetDecimal(1);
                lblOverdueCount.Text = count.ToString();
                lblTotalPenalty.Text = $"₱{total:F2}";
                pnlWarning.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            r.Close();
        }

        // ── Tab switching ────────────────────────────────────────────
        private void btnTabOverdue_Click(object sender, RoutedEventArgs e)
        {
            currentTab = "overdue";
            btnTabOverdue.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#178AE5"));
            btnTabOverdue.Foreground = Brushes.White;
            btnTabHistory.Background = Brushes.White;
            btnTabHistory.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#6B7280"));
            LoadPenalties();
        }

        private void btnTabHistory_Click(object sender, RoutedEventArgs e)
        {
            currentTab = "history";
            btnTabHistory.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#178AE5"));
            btnTabHistory.Foreground = Brushes.White;
            btnTabOverdue.Background = Brushes.White;
            btnTabOverdue.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#6B7280"));
            LoadPenalties();
        }

        // ── Save rate ────────────────────────────────────────────────
        private void btnSaveRate_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtRate.Text, out decimal rate) || rate < 0)
            {
                MessageBox.Show("Please enter a valid penalty rate.", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Settings.SetPenaltyRate(rate);
            new ActivityLogs().Log(Login.CurrentUserId, "Penalty Rate Updated",
                $"Penalty rate changed to ₱{rate:F2}/day.");

            MessageBox.Show($"Penalty rate updated to ₱{rate:F2}/day.", "Saved",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPenalties();
        }

        // ── Mark returned (with receipt) ─────────────────────────────
        private void btnMarkReturned_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            decimal rate = Settings.GetPenaltyRate();

            var result = MessageBox.Show(
                "Mark this book as returned?\nPenalty will be recorded.",
                "Confirm Return", MessageBoxButton.YesNo, MessageBoxImage.Question);

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
                            // Get details for receipt
                            string getInfo = @"SELECT bt.book_id, br.full_name, br.user_id,
                                                      bk.title, bt.due_date,
                                                      DATEDIFF(CURDATE(), bt.due_date) AS days,
                                                      DATEDIFF(CURDATE(), bt.due_date) * @rate AS penalty
                                               FROM borrow_transactions bt
                                               JOIN borrowers br ON bt.borrower_id = br.id
                                               JOIN books bk     ON bt.book_id     = bk.id
                                               WHERE bt.id = @id";
                            MySqlCommand getCmd = new MySqlCommand(getInfo, conn, tx);
                            getCmd.Parameters.AddWithValue("@rate", rate);
                            getCmd.Parameters.AddWithValue("@id", id);
                            MySqlDataReader r = getCmd.ExecuteReader();

                            int bookId = 0; string name = "", uid = "", title = "";
                            int days = 0; decimal penalty = 0; DateTime dueDate = DateTime.Today;

                            if (r.Read())
                            {
                                bookId = Convert.ToInt32(r["book_id"]);
                                name = r["full_name"].ToString();
                                uid = r["user_id"].ToString();
                                title = r["title"].ToString();
                                dueDate = Convert.ToDateTime(r["due_date"]);
                                days = Convert.ToInt32(r["days"]);
                                penalty = Convert.ToDecimal(r["penalty"]);
                            }
                            r.Close();

                            // Mark returned
                            string update = @"UPDATE borrow_transactions
                                              SET return_date    = CURDATE(),
                                                  status         = 'returned',
                                                  penalty_amount = @pen,
                                                  penalty_paid   = 1
                                              WHERE id = @id";
                            MySqlCommand cmd = new MySqlCommand(update, conn, tx);
                            cmd.Parameters.AddWithValue("@pen", penalty);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();

                            // Restore copies
                            string restore = "UPDATE books SET available_copies = available_copies + 1 WHERE id = @id";
                            MySqlCommand restoreCmd = new MySqlCommand(restore, conn, tx);
                            restoreCmd.Parameters.AddWithValue("@id", bookId);
                            restoreCmd.ExecuteNonQuery();

                            tx.Commit();

                            new ActivityLogs().Log(Login.CurrentUserId, "Penalty Settled",
                                $"{name} returned \"{title}\" with penalty ₱{penalty:F2}.");

                            // Show receipt
                            ShowReceipt(name, uid, title, dueDate, days, penalty, rate);
                            LoadPenalties();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing return:\n" + ex.Message, "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ── Receipt popup ────────────────────────────────────────────
        private void ShowReceipt(string name, string uid, string title,
                                  DateTime dueDate, int days, decimal penalty, decimal rate)
        {
            string receipt =
                $"══════════════════════════════\n" +
                $"        PENALTY RECEIPT\n" +
                $"══════════════════════════════\n" +
                $"Date Returned : {DateTime.Today:MMM dd, yyyy}\n" +
                $"Student       : {name}\n" +
                $"Student ID    : {uid}\n" +
                $"Book          : {title}\n" +
                $"──────────────────────────────\n" +
                $"Due Date      : {dueDate:MMM dd, yyyy}\n" +
                $"Days Overdue  : {days} days\n" +
                $"Rate          : ₱{rate:F2}/day\n" +
                $"──────────────────────────────\n" +
                $"TOTAL PENALTY : ₱{penalty:F2}\n" +
                $"══════════════════════════════\n" +
                $"     Thank you!\n";

            MessageBox.Show(receipt, "Penalty Receipt",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}