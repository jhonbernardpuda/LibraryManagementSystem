using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AttendanceMonitoringSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Login.CurrentRole == "Student")
            {
                btnDashboard.Visibility = Visibility.Collapsed;
                btnStudents.Visibility = Visibility.Collapsed;
                btnAttendance.Visibility = Visibility.Collapsed;

                MainFrame.Content = new ReportsControl(); // or Navigate if Frame
            }
            else
            {
                MainFrame.Content = new DashboardControl();
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new DashboardControl();
        }

        private void Students_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new StudentsControl();
        }

        private void Attendance_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new AttendanceControl();
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new ReportsControl();
        }

        private void ApplyRole()
        {
            string role = Login.CurrentRole;

            if (role == "Student")
            {
                btnDashboard.Visibility = Visibility.Collapsed;
                btnStudents.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;

                btnAttendance.Visibility = Visibility.Visible;
            }
            else if (role == "Teacher")
            {
                btnDashboard.Visibility = Visibility.Visible;
                btnStudents.Visibility = Visibility.Visible;
                btnAttendance.Visibility = Visibility.Visible;
                btnReports.Visibility = Visibility.Visible;
            }
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Login login = new Login();
                login.Show();

                this.Close();
            }
        }
    }
}
