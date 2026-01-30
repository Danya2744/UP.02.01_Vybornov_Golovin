using System;
using System.Collections.Generic;
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
        private List<activities> _activities;

        public EventDetailsPage(int eventId, users user = null, string role = null)
        {
            InitializeComponent();
            _eventId = eventId;
            _currentUser = user;
            _currentRole = role?.ToLower();

            Loaded += EventDetailsPage_Loaded;
            UpdateUIForUser();
        }

        private void UpdateUIForUser()
        {
            // Скрываем все панели сначала
            GuestActionsPanel.Visibility = Visibility.Collapsed;
            ParticipantActionsPanel.Visibility = Visibility.Collapsed;
            ModeratorActionsPanel.Visibility = Visibility.Collapsed;
            JuryActionsPanel.Visibility = Visibility.Collapsed;
            OrganizerActionsPanel.Visibility = Visibility.Collapsed;

            if (_currentUser != null)
            {
                // Пользователь авторизован
                GuestActionsPanel.Visibility = Visibility.Collapsed;

                switch (_currentRole)
                {
                    case "участник":
                        ParticipantActionsPanel.Visibility = Visibility.Visible;
                        break;
                    case "модератор":
                        ModeratorActionsPanel.Visibility = Visibility.Visible;
                        break;
                    case "жюри":
                        JuryActionsPanel.Visibility = Visibility.Visible;
                        break;
                    case "организатор":
                        OrganizerActionsPanel.Visibility = Visibility.Visible;
                        break;
                    default:
                        GuestActionsPanel.Visibility = Visibility.Visible;
                        break;
                }
            }
            else
            {
                // Гость
                GuestActionsPanel.Visibility = Visibility.Visible;
            }
        }

        private void EventDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEventDetails();
            CheckUserRegistration();
            LoadUserRoles();
        }

        private void LoadUserRoles()
        {
            if (_currentUser != null)
            {
                try
                {
                    using (var context = new ConferenceDBEntities())
                    {
                        // Загружаем активности для текущего мероприятия
                        _activities = context.activities
                            .Where(a => a.event_id == _eventId)
                            .OrderBy(a => a.activity_day)
                            .ThenBy(a => a.start_time)
                            .ToList();

                        // Для модератора: проверяем, на какие активности он уже зарегистрирован
                        if (_currentRole == "модератор")
                        {
                            var moderatorActivities = context.moderator_activities
                                .Where(ma => ma.moderator_id == _currentUser.user_id)
                                .Select(ma => ma.activity_id)
                                .ToList();

                            ModeratorActivitiesComboBox.ItemsSource = _activities
                                .Where(a => !moderatorActivities.Contains(a.activity_id))
                                .ToList();
                        }

                        // Для жюри: проверяем, на какие активности он уже зарегистрирован
                        if (_currentRole == "жюри")
                        {
                            var juryActivities = context.jury_activities
                                .Where(ja => ja.jury_id == _currentUser.user_id)
                                .Select(ja => ja.activity_id)
                                .ToList();

                            JuryActivitiesComboBox.ItemsSource = _activities
                                .Where(a => !juryActivities.Contains(a.activity_id))
                                .ToList();

                            // Загружаем уже выбранные активности для отображения
                            LoadSelectedJuryActivities();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadSelectedJuryActivities()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var selectedActivities = context.jury_activities
                        .Where(ja => ja.jury_id == _currentUser.user_id)
                        .Join(context.activities,
                              ja => ja.activity_id,
                              a => a.activity_id,
                              (ja, a) => a)
                        .Where(a => a.event_id == _eventId)
                        .ToList();

                    SelectedJuryActivitiesListBox.ItemsSource = selectedActivities;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки выбранных активностей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    // PageTitleTextBlock.Text остается "Детали мероприятия" как установлено в XAML

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
            if (_currentUser != null && _currentRole == "участник")
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

        private void RegisterAsModeratorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModeratorActivitiesComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите активность для модерации",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedActivity = ModeratorActivitiesComboBox.SelectedItem as activities;

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Проверяем, не занята ли уже эта активность другим модератором
                    var existingModerator = context.moderator_activities
                        .FirstOrDefault(ma => ma.activity_id == selectedActivity.activity_id);

                    if (existingModerator != null)
                    {
                        MessageBox.Show("Эта активность уже имеет модератора",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Добавляем модератора к активности
                    var moderatorActivity = new moderator_activities
                    {
                        activity_id = selectedActivity.activity_id,
                        moderator_id = _currentUser.user_id
                    };

                    context.moderator_activities.Add(moderatorActivity);
                    context.SaveChanges();

                    MessageBox.Show($"Вы успешно зарегистрированы как модератор на активность:\n\"{selectedActivity.activity_name}\"",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем список доступных активностей
                    LoadUserRoles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации как модератор: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterAsJuryButton_Click(object sender, RoutedEventArgs e)
        {
            if (JuryActivitiesComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите активность для оценки",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedActivity = JuryActivitiesComboBox.SelectedItem as activities;

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Проверяем, не зарегистрирован ли уже пользователь на эту активность как жюри
                    var existingJury = context.jury_activities
                        .FirstOrDefault(ja => ja.activity_id == selectedActivity.activity_id &&
                                             ja.jury_id == _currentUser.user_id);

                    if (existingJury != null)
                    {
                        MessageBox.Show("Вы уже зарегистрированы как жюри на эту активность",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Добавляем жюри к активности
                    var juryActivity = new jury_activities
                    {
                        activity_id = selectedActivity.activity_id,
                        jury_id = _currentUser.user_id
                    };

                    context.jury_activities.Add(juryActivity);
                    context.SaveChanges();

                    MessageBox.Show($"Вы успешно зарегистрированы как жюри на активность:\n\"{selectedActivity.activity_name}\"",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем списки
                    LoadUserRoles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации как жюри: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveJuryActivityButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedJuryActivitiesListBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите активность для удаления",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedActivity = SelectedJuryActivitiesListBox.SelectedItem as activities;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить себя как жюри из активности:\n\"{selectedActivity.activity_name}\"?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ConferenceDBEntities())
                    {
                        var juryActivity = context.jury_activities
                            .FirstOrDefault(ja => ja.activity_id == selectedActivity.activity_id &&
                                                 ja.jury_id == _currentUser.user_id);

                        if (juryActivity != null)
                        {
                            context.jury_activities.Remove(juryActivity);
                            context.SaveChanges();

                            MessageBox.Show("Вы успешно удалены как жюри из этой активности",
                                "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Обновляем списки
                            LoadUserRoles();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentRole != "участник")
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

        // Новый метод для просмотра участников
        private void ViewParticipantsButtonClick(object sender, RoutedEventArgs e)
        {
            // Проверяем, доступна ли эта функция для текущей роли
            if (_currentUser != null && (_currentRole == "модератор" || _currentRole == "жюри" || _currentRole == "организатор"))
            {
                var participantsPage = new ParticipantsPage(_eventId, _currentUser, _currentRole);
                NavigationService.Navigate(participantsPage);
            }
            else
            {
                MessageBox.Show("Эта функция доступна только для модераторов, жюри и организаторов",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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