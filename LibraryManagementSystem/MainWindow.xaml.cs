using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LibraryManagementSystem
{
    public partial class MainWindow : Window
    {
        private Button _activeNav;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set user badge
            lblUserName.Text = Login.CurrentFullName ?? Login.CurrentUser;
            lblUserRole.Text = Login.CurrentRole == "admin" ? "Administrator" : "Librarian";

            // Role-based nav visibility
            ApplyRole();

            // Default page
            NavigateTo(btnDashboard, "Dashboard", new DashboardControl());
        }

        // ── Role-based nav ───────────────────────────────────────────
        private void ApplyRole()
        {
            string role = Login.CurrentRole;

            if (role == "librarian")
            {
                btnUsers.Visibility = Visibility.Collapsed;
                btnLogs.Visibility = Visibility.Collapsed;
            }
        }
     

        // ── Navigation helper ────────────────────────────────────────
        private void NavigateTo(Button btn, string title, UserControl page)
        {
            // Reset previous active button
            if (_activeNav != null)
            {
                _activeNav.Background = Brushes.Transparent;
                _activeNav.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4B5563"));
                _activeNav.FontWeight = FontWeights.Normal;
            }

            // Set new active button
            btn.Background = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8F1FC"));
            btn.Foreground = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1764B4"));
            btn.FontWeight = FontWeights.SemiBold;
            _activeNav = btn;

            // Update topbar title and load page
            lblPageTitle.Text = title;
            MainFrame.Content = page;
        }

        // ── Nav button clicks ────────────────────────────────────────
        private void Dashboard_Click(object sender, RoutedEventArgs e)
            => NavigateTo(btnDashboard, "Dashboard", new DashboardControl());

        private void Books_Click(object sender, RoutedEventArgs e)
            => NavigateTo(btnBooks, "Books", new BooksControl());

        private void Borrowers_Click(object sender, RoutedEventArgs e)
            => NavigateTo(btnBorrowers, "Borrowers", new BorrowersControl());

        private void Borrow_Click(object sender, RoutedEventArgs e)
            => NavigateTo(btnBorrow, "Borrow / Return", new BorrowControl());

        private void Penalties_Click(object sender, RoutedEventArgs e)
            => NavigateTo(btnPenalties, "Penalties", new PenaltiesControl());

        private void Reports_Click(object sender, RoutedEventArgs e)
            => NavigateTo(btnReports, "Reports", new ReportsControl());

        private void Logs_Click(object sender, RoutedEventArgs e)
        {
            var logsControl = new ActivityLogsControl();
            NavigateTo(btnLogs, "Activity Logs", logsControl);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to sign out?",
                "Confirm Sign Out",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                new ActivityLogs().Log(Login.CurrentUserId, "Logout", $"{Login.CurrentFullName} signed out.");
                new Login().Show();
                this.Close();
            }
        }
        private void Users_Click(object sender, RoutedEventArgs e)
    => NavigateTo(btnUsers, "Users", new UsersControl());
    }
}