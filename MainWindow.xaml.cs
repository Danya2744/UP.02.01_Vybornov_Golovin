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
using UP._02._01_Vybornov.Pages;

namespace UP._02._01_Vybornov
{
    public partial class MainWindow : Window
    {
        private users _currentUser;
        private string _currentRole;

        public MainWindow()
        {
            InitializeComponent();
            LoadLoginPage();
        }

        private void LoadLoginPage()
        {
            var loginPage = new LoginPage();
            loginPage.UserLoggedIn += OnUserLoggedIn;
            loginPage.GuestLoggedIn += OnGuestLoggedIn;
            MainFrame.Navigate(loginPage);
            UpdateUserInterface(null, "гость");
        }

        private void OnUserLoggedIn(object sender, UserLoggedInEventArgs e)
        {
            _currentUser = e.User;
            _currentRole = e.RoleName;

            UpdateUserInterface(e.User, e.RoleName);
            NavigateToEventsPage();
        }

        private void OnGuestLoggedIn(object sender, EventArgs e)
        {
            _currentUser = null;
            _currentRole = "гость";

            UpdateUserInterface(null, "гость");
            NavigateToEventsPage();
        }

        private void UpdateUserInterface(users user, string role)
        {
            if (user != null)
            {
                string greeting = GetTimeOfDayGreeting();
                string[] nameParts = user.full_name.Split(' ');
                string firstName = nameParts.Length > 0 ? nameParts[0] : user.full_name;

                UserInfoTextBlock.Text = $"{firstName}";

                // Форматируем отображение роли с правильным склонением
                string roleDisplay = FormatRoleDisplay(role);
                UserRoleTextBlock.Text = roleDisplay;

                Title = $"Система конференций - {roleDisplay} ({greeting}, {firstName})";

                LogoutButton.Visibility = Visibility.Visible;
            }
            else
            {
                UserInfoTextBlock.Text = "Гость";
                UserRoleTextBlock.Text = "Неавторизованный пользователь";
                Title = "Система конференций - Гость";

                LogoutButton.Visibility = Visibility.Visible;
            }
        }

        private string FormatRoleDisplay(string role)
        {
            if (string.IsNullOrEmpty(role))
                return "Пользователь";

            string roleLower = role.ToLower();

            switch (roleLower)
            {
                case "участник":
                    return "Участник";
                case "модератор":
                    return "Модератор";
                case "жюри":
                    return "Член жюри";
                case "организатор":
                    return "Организатор";
                case "гость":
                    return "Гость";
                default:
                    return CapitalizeFirstLetter(role);
            }
        }

        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private string GetTimeOfDayGreeting()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 5 && hour < 12) return "Доброе утро";
            if (hour >= 12 && hour < 18) return "Добрый день";
            if (hour >= 18 && hour < 23) return "Добрый вечер";
            return "Доброй ночи";
        }

        private void NavigateToEventsPage()
        {
            var eventsPage = new EventsPage(_currentUser, _currentRole);
            MainFrame.Navigate(eventsPage);
        }

        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _currentUser = null;
                _currentRole = "гость";
                LoadLoginPage();
            }
        }
    }
}