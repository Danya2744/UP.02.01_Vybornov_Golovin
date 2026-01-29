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
            //NavigateBasedOnRole(e.RoleName);
        }

        private void OnGuestLoggedIn(object sender, EventArgs e)
        {
            _currentUser = null;
            _currentRole = "гость";

            UpdateUserInterface(null, "гость");
            //NavigateToEventsPage();
        }

        private void UpdateUserInterface(users user, string role)
        {
            if (user != null)
            {
                string greeting = GetTimeOfDayGreeting();
                string[] nameParts = user.full_name.Split(' ');
                string firstName = nameParts.Length > 0 ? nameParts[0] : user.full_name;

                UserInfoTextBlock.Text = $"{firstName}";
                UserRoleTextBlock.Text = CapitalizeFirstLetter(role);
                Title = $"Система конференций - {CapitalizeFirstLetter(role)} ({greeting}, {firstName})";
            }
            else
            {
                UserInfoTextBlock.Text = "Гость";
                UserRoleTextBlock.Text = "Неавторизованный пользователь";
                Title = "Система конференций - Гость";
            }

            LogoutButton.Visibility = role == "гость" ? Visibility.Visible : Visibility.Visible;
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

            if (hour >= 4 && hour < 12) return "Доброе утро";
            if (hour >= 12 && hour < 18) return "Добрый день";
            if (hour >= 18 && hour < 23) return "Добрый вечер";
            return "Доброй ночи";
        }

        //private void NavigateBasedOnRole(string role)
        //{
        //    switch (role.ToLower())
        //    {
        //        case "организатор":
        //            NavigateToOrganizerPage();
        //            break;
        //        case "участник":
        //            NavigateToParticipantPage();
        //            break;
        //        case "модератор":
        //            NavigateToModeratorPage();
        //            break;
        //        case "жюри":
        //            NavigateToJuryPage();
        //            break;
        //        default:
        //            NavigateToEventsPage();
        //            break;
        //    }
        //}

        //private void NavigateToOrganizerPage()
        //{
        //    StatusBarText.Text = "Режим организатора";
        //    var organizerPage = new OrganizerPage(_currentUser);
        //    MainFrame.Navigate(organizerPage);
        //}

        //private void NavigateToParticipantPage()
        //{
        //    StatusBarText.Text = "Режим участника";
        //    var participantPage = new ParticipantPage(_currentUser);
        //    MainFrame.Navigate(participantPage);
        //}

        //private void NavigateToModeratorPage()
        //{
        //    StatusBarText.Text = "Режим модератора";
        //    var moderatorPage = new ModeratorPage(_currentUser);
        //    MainFrame.Navigate(moderatorPage);
        //}

        //private void NavigateToJuryPage()
        //{
        //    StatusBarText.Text = "Режим жюри";
        //    var juryPage = new JuryPage(_currentUser);
        //    MainFrame.Navigate(juryPage);
        //}

        //private void NavigateToEventsPage()
        //{
        //    StatusBarText.Text = "Просмотр мероприятий";
        //    var eventsPage = new EventsPage(_currentUser, _currentRole);
        //    MainFrame.Navigate(eventsPage);
        //}

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
