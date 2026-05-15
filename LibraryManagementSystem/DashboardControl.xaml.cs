using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class DashboardControl : UserControl
    {
        Database db = new Database();
        private decimal PENALTY_PER_DAY => Settings.GetPenaltyRate();

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    // Auto-update overdue status
                    string updateOverdue = @"UPDATE borrow_transactions
                                             SET status = 'overdue'
                                             WHERE return_date IS NULL
                                               AND due_date < CURDATE()
                                               AND status = 'borrowed'";
                    new MySqlCommand(updateOverdue, conn).ExecuteNonQuery();

                    SetWelcome();
                    LoadCards(conn);
                    LoadRecentTransactions(conn);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading dashboard:\n" + ex.Message, "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ── Welcome message ──────────────────────────────────────────
        private void SetWelcome()
        {
            string role = Login.CurrentRole == "admin" ? "Administrator" : "Librarian";
            lblWelcome.Text = $"Welcome back, {Login.CurrentFullName}!";
            lblDate.Text = $"{role}  •  {DateTime.Now:dddd, MMMM dd, yyyy}";
        }

        // ── Summary cards ────────────────────────────────────────────
        private void LoadCards(MySqlConnection conn)
        {
            // Total books
            var cmd = new MySqlCommand(
                "SELECT SUM(total_copies), SUM(available_copies) FROM books", conn);
            var r = cmd.ExecuteReader();
            if (r.Read())
            {
                lblTotalBooks.Text = r.IsDBNull(0) ? "0" : r.GetInt64(0).ToString();
                lblAvailableBooks.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} available";
            }
            r.Close();

            // Total students
            cmd = new MySqlCommand(
                "SELECT COUNT(*), SUM(is_active) FROM borrowers", conn);
            r = cmd.ExecuteReader();
            if (r.Read())
            {
                lblTotalStudents.Text = r.IsDBNull(0) ? "0" : r.GetInt64(0).ToString();
                lblActiveStudents.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} active";
            }
            r.Close();

            // Active borrows and overdue
            cmd = new MySqlCommand(
                @"SELECT COUNT(*),
                         SUM(CASE WHEN status = 'overdue' THEN 1 ELSE 0 END)
                  FROM borrow_transactions
                  WHERE status IN ('borrowed','overdue')", conn);
            r = cmd.ExecuteReader();
            if (r.Read())
            {
                lblActiveBorrows.Text = r.IsDBNull(0) ? "0" : r.GetInt64(0).ToString();
                lblOverdueCount.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} overdue";
            }
            r.Close();

            // Total penalties
            cmd = new MySqlCommand(
                @"SELECT SUM(DATEDIFF(CURDATE(), due_date) * @rate)
                  FROM borrow_transactions
                  WHERE return_date IS NULL AND due_date < CURDATE()", conn);
            cmd.Parameters.AddWithValue("@rate", PENALTY_PER_DAY);
            r = cmd.ExecuteReader();
            if (r.Read())
            {
                decimal total = r.IsDBNull(0) ? 0 : r.GetDecimal(0);
                lblTotalPenalties.Text = $"₱{total:F2}";
            }
            r.Close();
        }

        // ── Recent transactions ──────────────────────────────────────
        private void LoadRecentTransactions(MySqlConnection conn)
        {
            string query = @"SELECT br.full_name,
                                    br.user_id,
                                    bk.title,
                                    bt.borrow_date,
                                    bt.due_date,
                                    bt.status
                             FROM borrow_transactions bt
                             JOIN borrowers br ON bt.borrower_id = br.id
                             JOIN books bk     ON bt.book_id     = bk.id
                             ORDER BY bt.borrow_date DESC
                             LIMIT 10";
            DataTable dt = new DataTable();
            new MySqlDataAdapter(query, conn).Fill(dt);
            dgRecent.ItemsSource = dt.DefaultView;
        }
    }
}