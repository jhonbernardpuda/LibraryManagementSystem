using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Controls;

namespace LibraryManagementSystem
{
    public partial class ReportsControl : UserControl
    {
        Database db = new Database();
        private decimal PENALTY_PER_DAY => Settings.GetPenaltyRate();

        public ReportsControl()
        {
            InitializeComponent();
            LoadReports();
        }

        private void LoadReports()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    LoadSummaryCards(conn);
                    LoadTopBooks(conn);
                    LoadTopStudents(conn);
                    LoadOverdueStudents(conn);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading reports:\n" + ex.Message, "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ── Summary Cards ────────────────────────────────────────────
        private void LoadSummaryCards(MySqlConnection conn)
        {
            // Total books
            var cmd = new MySqlCommand("SELECT SUM(total_copies), COUNT(*) FROM books", conn);
            var r = cmd.ExecuteReader();
            if (r.Read())
            {
                lblTotalBooks.Text = r.IsDBNull(0) ? "0" : r.GetInt64(0).ToString();
                lblTotalTitles.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} titles";
            }
            r.Close();

            // Total students
            cmd = new MySqlCommand("SELECT COUNT(*), SUM(is_active) FROM borrowers", conn);
            r = cmd.ExecuteReader();
            if (r.Read())
            {
                lblTotalStudents.Text = r.IsDBNull(0) ? "0" : r.GetInt64(0).ToString();
                lblActiveStudents.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} active";
            }
            r.Close();

            // Total transactions
            cmd = new MySqlCommand(
                "SELECT COUNT(*), SUM(CASE WHEN status IN ('borrowed','overdue') THEN 1 ELSE 0 END) FROM borrow_transactions", conn);
            r = cmd.ExecuteReader();
            if (r.Read())
            {
                lblTotalTransactions.Text = r.IsDBNull(0) ? "0" : r.GetInt64(0).ToString();
                lblActiveTransactions.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} active";
            }
            r.Close();

            // Total penalties
            cmd = new MySqlCommand(
                @"SELECT SUM(DATEDIFF(CURDATE(), due_date) * @rate),
                         COUNT(*)
                  FROM borrow_transactions
                  WHERE return_date IS NULL AND due_date < CURDATE()", conn);
            cmd.Parameters.AddWithValue("@rate", PENALTY_PER_DAY);
            r = cmd.ExecuteReader();
            if (r.Read())
            {
                decimal total = r.IsDBNull(0) ? 0 : r.GetDecimal(0);
                lblTotalPenalties.Text = $"₱{total:F2}";
                lblOverdueCount.Text = $"{(r.IsDBNull(1) ? 0 : r.GetInt64(1))} overdue";
            }
            r.Close();
        }

        // ── Most Borrowed Books ──────────────────────────────────────
        private void LoadTopBooks(MySqlConnection conn)
        {
            string query = @"SELECT b.title, COUNT(bt.id) AS count
                             FROM borrow_transactions bt
                             JOIN books b ON bt.book_id = b.id
                             GROUP BY b.id, b.title
                             ORDER BY count DESC
                             LIMIT 5";
            DataTable dt = new DataTable();
            new MySqlDataAdapter(query, conn).Fill(dt);
            dgTopBooks.ItemsSource = dt.DefaultView;
        }

        // ── Most Active Students ─────────────────────────────────────
        private void LoadTopStudents(MySqlConnection conn)
        {
            string query = @"SELECT br.full_name, br.user_id, COUNT(bt.id) AS count
                             FROM borrow_transactions bt
                             JOIN borrowers br ON bt.borrower_id = br.id
                             GROUP BY br.id, br.full_name, br.user_id
                             ORDER BY count DESC
                             LIMIT 5";
            DataTable dt = new DataTable();
            new MySqlDataAdapter(query, conn).Fill(dt);
            dgTopStudents.ItemsSource = dt.DefaultView;
        }

        // ── Overdue Students ─────────────────────────────────────────
        private void LoadOverdueStudents(MySqlConnection conn)
        {
            string query = @"SELECT br.full_name,
                                    br.user_id,
                                    bk.title,
                                    bt.due_date,
                                    DATEDIFF(CURDATE(), bt.due_date)        AS days_overdue,
                                    CONCAT('₱', FORMAT(DATEDIFF(CURDATE(), bt.due_date) * @rate, 2)) AS penalty
                             FROM borrow_transactions bt
                             JOIN borrowers br ON bt.borrower_id = br.id
                             JOIN books bk     ON bt.book_id     = bk.id
                             WHERE bt.return_date IS NULL
                               AND bt.due_date < CURDATE()
                             ORDER BY bt.due_date ASC";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@rate", PENALTY_PER_DAY);
            DataTable dt = new DataTable();
            new MySqlDataAdapter(cmd).Fill(dt);
            dgOverdueStudents.ItemsSource = dt.DefaultView;
        }
    }
}