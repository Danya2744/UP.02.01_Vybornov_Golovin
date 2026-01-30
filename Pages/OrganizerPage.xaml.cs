using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UP._02._01_Vybornov.Pages
{
    public partial class OrganizerPage : Page
    {
        private users _currentUser;

        public OrganizerPage(users user)
        {
            InitializeComponent();
            _currentUser = user;
            Loaded += OrganizerPage_Loaded;
        }

        private void OrganizerPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWelcomeMessage();

            // Загружаем фото пользователя
            LoadUserPhoto();
        }

        private void LoadWelcomeMessage()
        {
            string timeOfDay = GetTimeOfDayGreeting();

            // Извлекаем имя и отчество (если есть)
            string[] nameParts = _currentUser.full_name.Split(' ');
            string greetingName = nameParts[0];

            if (nameParts.Length > 1)
            {
                greetingName += " " + nameParts[1];
            }

            WelcomeTextBlock.Text = $"{timeOfDay}, {greetingName}!";
        }

        private void LoadUserPhoto()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentUser.photo_path))
                {
                    // Можно добавить Image на страницу для отображения фото
                    // Например, добавить Image элемент в XAML
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки загрузки фото
                Console.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
        }

        private string GetTimeOfDayGreeting()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 5 && hour < 12) return "Доброе утро";
            if (hour >= 12 && hour < 18) return "Добрый день";
            if (hour >= 18 && hour < 23) return "Добрый вечер";
            return "Доброй ночи";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к просмотру мероприятий
            var eventsPage = new EventsPage(_currentUser, "организатор");
            NavigationService.Navigate(eventsPage);
        }

        private void EventsManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу управления мероприятиями
            var eventsManagementPage = new OrganizerEventsPage(_currentUser);
            NavigationService.Navigate(eventsManagementPage);
        }

        private void ActivitiesManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу управления активностями
            var activitiesManagementPage = new OrganizerActivitiesPage(_currentUser);
            NavigationService.Navigate(activitiesManagementPage);
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу профиля
            var profilePage = new ProfilePage(_currentUser);
            NavigationService.Navigate(profilePage);
        }
    }
}