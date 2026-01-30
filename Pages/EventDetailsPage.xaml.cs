using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace UP._02._01_Vybornov.Pages
{
    public partial class EventDetailsPage : Page
    {
        private int _eventId;
        private users _currentUser;
        private string _currentRole;
        private event_registrations _currentRegistration;

        public EventDetailsPage(int eventId, users user = null, string role = null)
        {
            InitializeComponent();
            _eventId = eventId;
            _currentUser = user;
            _currentRole = role;

            Loaded += EventDetailsPage_Loaded;
            UpdateUIForUser();
        }

        private void UpdateUIForUser()
        {
            if (_currentUser != null)
            {
                // Пользователь авторизован
                GuestActionsPanel.Visibility = Visibility.Collapsed;

                if (_currentRole.ToLower() == "участник")
                {
                    // Показываем панель действий для участника
                    ParticipantActionsPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    // Для других ролей (организатор, модератор, жюри)
                    ParticipantActionsPanel.Visibility = Visibility.Collapsed;
                    GuestActionsPanel.Visibility = Visibility.Visible;

                }
            }
            else
            {
                // Гость
                GuestActionsPanel.Visibility = Visibility.Visible;
                ParticipantActionsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void EventDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEventDetails();
            CheckUserRegistration();
        }

        private void LoadEventDetails()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Находим мероприятие
                    var ev = context.events.FirstOrDefault(e => e.event_id == _eventId);
                    if (ev == null)
                    {
                        MessageBox.Show("Мероприятие не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    // Загружаем связанные данные
                    var direction = context.directions.FirstOrDefault(d => d.direction_id == ev.direction_id);
                    var organizer = ev.organizer_id.HasValue ?
                        context.users.FirstOrDefault(u => u.user_id == ev.organizer_id.Value) : null;

                    // Находим город проведения
                    var cityEvent = context.city_event.FirstOrDefault(ce => ce.event_id == ev.event_id);
                    var city = cityEvent != null ?
                        context.cities.FirstOrDefault(c => c.city_id == cityEvent.city_id) : null;

                    // Обновляем UI
                    EventTitleTextBlock.Text = ev.event_name;

                    // Дата
                    if (ev.start_date == ev.end_date)
                    {
                        DateTextBlock.Text = ev.start_date.ToString("dd.MM.yyyy");
                    }
                    else
                    {
                        DateTextBlock.Text = $"{ev.start_date:dd.MM.yyyy} - {ev.end_date:dd.MM.yyyy}";
                    }

                    // Город
                    CityTextBlock.Text = city?.city_name ?? "Не указан";

                    // Организатор
                    OrganizerTextBlock.Text = organizer?.full_name ?? "Не указан";

                    // Направление
                    DirectionTextBlock.Text = direction?.direction_name ?? "Не указано";

                    // Длительность
                    int durationDays = (ev.end_date - ev.start_date).Days + 1;
                    DurationTextBlock.Text = $"{durationDays} дн.";

                    // Описание
                    DescriptionTextBlock.Text = ev.description ?? "Описание отсутствует";

                    // Логотип
                    if (!string.IsNullOrEmpty(ev.logo_path))
                    {
                        try
                        {
                            EventLogoImage.Source = new BitmapImage(new Uri(ev.logo_path, UriKind.RelativeOrAbsolute));
                        }
                        catch
                        {
                            EventLogoImage.Source = new BitmapImage(new Uri("/Resources/default_event.png", UriKind.Relative));
                        }
                    }
                    else
                    {
                        EventLogoImage.Source = new BitmapImage(new Uri("/Resources/default_event.png", UriKind.Relative));
                    }

                    // Загружаем активности
                    LoadActivities(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей мероприятия:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckUserRegistration()
        {
            if (_currentUser != null && _currentRole.ToLower() == "участник")
            {
                try
                {
                    using (var context = new ConferenceDBEntities())
                    {
                        // Проверяем, зарегистрирован ли пользователь на это мероприятие
                        _currentRegistration = context.event_registrations
                            .FirstOrDefault(r => r.event_id == _eventId && r.user_id == _currentUser.user_id);

                        if (_currentRegistration != null)
                        {
                            // Пользователь уже зарегистрирован
                            UpdateRegistrationStatus(true);
                        }
                        else
                        {
                            // Пользователь не зарегистрирован
                            UpdateRegistrationStatus(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки регистрации: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateRegistrationStatus(bool isRegistered)
        {
            if (isRegistered && _currentRegistration != null)
            {
                // Показываем статус регистрации
                RegistrationStatusBorder.Visibility = Visibility.Visible;
                RegistrationStatusText.Text = $"Вы зарегистрированы на это мероприятие ({_currentRegistration.registration_date:dd.MM.yyyy})";

                // Обновляем кнопки
                RegisterButton.Visibility = Visibility.Collapsed;
                CancelRegistrationButton.Visibility = Visibility.Visible;
                RegistrationInfoText.Text = "Ваша регистрация подтверждена. Вы можете отменить её, если передумаете.";
            }
            else
            {
                // Скрываем статус регистрации
                RegistrationStatusBorder.Visibility = Visibility.Collapsed;

                // Обновляем кнопки
                RegisterButton.Visibility = Visibility.Visible;
                CancelRegistrationButton.Visibility = Visibility.Collapsed;
                RegistrationInfoText.Text = "Нажмите кнопку, чтобы зарегистрироваться на это мероприятие.";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentRole.ToLower() != "участник")
            {
                MessageBox.Show("Для регистрации необходимо быть авторизованным участником",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Проверяем, не зарегистрирован ли уже пользователь
                    var existingRegistration = context.event_registrations
                        .FirstOrDefault(r => r.event_id == _eventId && r.user_id == _currentUser.user_id);

                    if (existingRegistration != null)
                    {
                        MessageBox.Show("Вы уже зарегистрированы на это мероприятие",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Создаем новую регистрацию
                    var registration = new event_registrations
                    {
                        event_id = _eventId,
                        user_id = _currentUser.user_id,
                        registration_date = DateTime.Now,
                        status = "registered"
                    };

                    context.event_registrations.Add(registration);
                    context.SaveChanges();

                    _currentRegistration = registration;

                    MessageBox.Show("Вы успешно зарегистрированы на мероприятие!",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем UI
                    UpdateRegistrationStatus(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelRegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRegistration == null)
            {
                MessageBox.Show("Регистрация не найдена",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите отменить регистрацию на это мероприятие?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ConferenceDBEntities())
                    {
                        var registration = context.event_registrations
                            .FirstOrDefault(r => r.registration_id == _currentRegistration.registration_id);

                        if (registration != null)
                        {
                            context.event_registrations.Remove(registration);
                            context.SaveChanges();

                            _currentRegistration = null;

                            MessageBox.Show("Регистрация успешно отменена",
                                "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Обновляем UI
                            UpdateRegistrationStatus(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отмене регистрации: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadActivities(ConferenceDBEntities context)
        {
            try
            {
                var activities = context.activities
                    .Where(a => a.event_id == _eventId)
                    .OrderBy(a => a.activity_day)
                    .ThenBy(a => a.start_time)
                    .ToList();

                ActivitiesItemsControl.ItemsSource = activities;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активностей:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void LoginForActionsButtonClick(object sender, RoutedEventArgs e)
        {
            // Переход на страницу входа с возвратом на эту страницу
            var loginPage = new LoginPage();
            loginPage.ReturnToEventId = _eventId;
            NavigationService.Navigate(loginPage);
        }
    }
}