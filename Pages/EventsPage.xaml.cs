using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UP._02._01_Vybornov.Pages
{
    public partial class EventsPage : Page
    {
        private List<EventViewModel> _allEvents = new List<EventViewModel>();
        private List<directions> _allDirections = new List<directions>();
        private users _currentUser;
        private string _currentRole;
        private bool _showMyEventsOnly = false;
        private List<int> _myRegisteredEventIds = new List<int>();
        private List<int> _myModeratorEventIds = new List<int>();
        private List<int> _myJuryEventIds = new List<int>();

        public EventsPage(users user, string role)
        {
            InitializeComponent();
            _currentUser = user;
            _currentRole = role;

            Loaded += EventsPage_Loaded;
            UpdateNavigationButtons();

            _myRegisteredEventIds = new List<int>();
            _myModeratorEventIds = new List<int>();
            _myJuryEventIds = new List<int>();
        }

        private void EventsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMyRegistrations();
            LoadDirections();
            LoadEvents();
        }

        private void UpdateNavigationButtons()
        {
            if (_currentUser != null)
            {
                ProfileButton.Visibility = Visibility.Visible;

                if (_currentRole.ToLower() == "участник" ||
                    _currentRole.ToLower() == "модератор" ||
                    _currentRole.ToLower() == "жюри")
                {
                    MyEventsButton.Visibility = Visibility.Visible;
                }
                else
                {
                    MyEventsButton.Visibility = Visibility.Collapsed;
                }

                UpdateMyEventsButtonAppearance();
            }
            else
            {
                MyEventsButton.Visibility = Visibility.Collapsed;
                ProfileButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadMyRegistrations()
        {
            if (_currentUser != null)
            {
                try
                {
                    using (var context = new ConferenceDBEntities())
                    {
                        if (_currentRole.ToLower() == "участник")
                        {
                            _myRegisteredEventIds = context.event_registrations
                                .Where(r => r.user_id == _currentUser.user_id && r.status.ToLower() != "cancelled")
                                .Select(r => r.event_id)
                                .Distinct()
                                .ToList();
                        }

                        if (_currentRole.ToLower() == "модератор")
                        {
                            _myModeratorEventIds = context.moderator_activities
                                .Where(ma => ma.moderator_id == _currentUser.user_id)
                                .Join(context.activities,
                                      ma => ma.activity_id,
                                      a => a.activity_id,
                                      (ma, a) => a.event_id)
                                .Distinct()
                                .ToList();
                        }

                        if (_currentRole.ToLower() == "жюри")
                        {
                            _myJuryEventIds = context.jury_activities
                                .Where(ja => ja.jury_id == _currentUser.user_id)
                                .Join(context.activities,
                                      ja => ja.activity_id,
                                      a => a.activity_id,
                                      (ja, a) => a.event_id)
                                .Distinct()
                                .ToList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки регистраций:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadEvents()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var events = context.events
                        .OrderByDescending(e => e.start_date)
                        .ToList();

                    var directions = context.directions.ToList();
                    var allUsers = context.users.ToList();
                    var cityEvents = context.city_event.ToList();
                    var cities = context.cities.ToList();

                    _allEvents.Clear();

                    foreach (var ev in events)
                    {
                        var viewModel = new EventViewModel
                        {
                            event_id = ev.event_id,
                            event_name = ev.event_name,
                            direction_id = ev.direction_id,
                            start_date = ev.start_date,
                            end_date = ev.end_date,
                            days_count = ev.days_count,
                            logo_path = ev.logo_path,
                            description = ev.description,
                            organizer_id = ev.organizer_id
                        };

                        var direction = directions.FirstOrDefault(d => d.direction_id == ev.direction_id);
                        viewModel.direction_name = direction?.direction_name ?? "Не указано";

                        if (ev.organizer_id.HasValue)
                        {
                            var organizer = allUsers.FirstOrDefault(u => u.user_id == ev.organizer_id.Value);
                            viewModel.organizer_name = organizer?.full_name ?? "Не указан";
                        }
                        else
                        {
                            viewModel.organizer_name = "Не указан";
                        }

                        if (ev.start_date == ev.end_date)
                        {
                            viewModel.date_range = ev.start_date.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            viewModel.date_range = $"{ev.start_date:dd.MM.yyyy} - {ev.end_date:dd.MM.yyyy}";
                        }

                        viewModel.is_upcoming = ev.end_date >= DateTime.Today;
                        viewModel.duration_days = (ev.end_date - ev.start_date).Days + 1;

                        var cityEvent = cityEvents.FirstOrDefault(ce => ce.event_id == ev.event_id);

                        if (cityEvent != null)
                        {
                            var city = cities.FirstOrDefault(c => c.city_id == cityEvent.city_id);
                            viewModel.city_name = city?.city_name ?? "Не указан";
                        }
                        else
                        {
                            viewModel.city_name = "Не указан";
                        }

                        viewModel.is_registered = false;

                        if (_currentUser != null && _currentRole.ToLower() == "участник")
                        {
                            viewModel.is_registered = _myRegisteredEventIds.Contains(ev.event_id);
                        }
                        else if (_currentUser != null && _currentRole.ToLower() == "модератор")
                        {
                            viewModel.is_registered = _myModeratorEventIds.Contains(ev.event_id);
                        }
                        else if (_currentUser != null && _currentRole.ToLower() == "жюри")
                        {
                            viewModel.is_registered = _myJuryEventIds.Contains(ev.event_id);
                        }

                        if (_currentUser != null && _currentRole.ToLower() == "модератор" && viewModel.is_registered)
                        {
                            var moderatorActivities = context.moderator_activities
                                .Where(ma => ma.moderator_id == _currentUser.user_id)
                                .Join(context.activities,
                                      ma => ma.activity_id,
                                      a => a.activity_id,
                                      (ma, a) => a)
                                .Where(a => a.event_id == ev.event_id)
                                .ToList();

                            if (moderatorActivities.Any())
                            {
                                viewModel.ModeratorActivities = moderatorActivities
                                    .Select(a => a.activity_name)
                                    .ToList();
                                viewModel.RoleInfo = $"Модератор активностей: {string.Join(", ", viewModel.ModeratorActivities)}";
                            }
                        }

                        if (_currentUser != null && _currentRole.ToLower() == "жюри" && viewModel.is_registered)
                        {
                            var juryActivities = context.jury_activities
                                .Where(ja => ja.jury_id == _currentUser.user_id)
                                .Join(context.activities,
                                      ja => ja.activity_id,
                                      a => a.activity_id,
                                      (ja, a) => a)
                                .Where(a => a.event_id == ev.event_id)
                                .ToList();

                            if (juryActivities.Any())
                            {
                                viewModel.JuryActivities = juryActivities
                                    .Select(a => a.activity_name)
                                    .ToList();
                                viewModel.RoleInfo = $"Жюри активностей: {string.Join(", ", viewModel.JuryActivities)}";
                            }
                        }

                        if (!string.IsNullOrEmpty(ev.logo_path))
                        {
                            if (!ev.logo_path.StartsWith("http") && !ev.logo_path.StartsWith("/"))
                            {
                                viewModel.logo_path = "/" + ev.logo_path.TrimStart('\\', '/');
                            }
                            else
                            {
                                viewModel.logo_path = ev.logo_path;
                            }
                        }
                        else
                        {
                            viewModel.logo_path = "/Resources/default_event.png";
                        }

                        _allEvents.Add(viewModel);
                    }

                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDirections()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    _allDirections = context.directions
                        .OrderBy(d => d.direction_name)
                        .ToList();

                    DirectionFilterComboBox.Items.Clear();

                    var allItem = new ComboBoxItem();
                    allItem.Content = "Все направления";
                    allItem.Tag = "all";
                    DirectionFilterComboBox.Items.Add(allItem);

                    foreach (var direction in _allDirections)
                    {
                        var item = new ComboBoxItem();
                        item.Content = direction.direction_name;
                        item.Tag = direction.direction_id;
                        DirectionFilterComboBox.Items.Add(item);
                    }

                    if (DirectionFilterComboBox.Items.Count > 0)
                    {
                        DirectionFilterComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки направлений:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allEvents == null || !_allEvents.Any())
                return;

            var filteredEvents = _allEvents.AsEnumerable();

            if (_showMyEventsOnly)
            {
                filteredEvents = filteredEvents.Where(e => e.is_registered);
            }

            var selectedDirection = DirectionFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedDirection?.Tag?.ToString() != "all")
            {
                if (selectedDirection?.Tag is int directionId)
                {
                    filteredEvents = filteredEvents.Where(e => e.direction_id == directionId);
                }
            }

            if (StartDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                filteredEvents = filteredEvents.Where(e => e.start_date >= startDate);
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                DateTime endDate = EndDatePicker.SelectedDate.Value;
                filteredEvents = filteredEvents.Where(e => e.end_date <= endDate);
            }

            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            ItemsControlEvents.ItemsSource = filteredEvents.ToList();
            UpdateEventsCount();
        }

        private void UpdateEventsCount()
        {
            if (_allEvents == null)
                return;

            int totalCount = _allEvents.Count;
            int filteredCount = ItemsControlEvents.Items.Count;

            int myEventsCount = 0;
            if (_currentUser != null)
            {
                switch (_currentRole.ToLower())
                {
                    case "участник":
                        myEventsCount = _myRegisteredEventIds.Count;
                        break;
                    case "модератор":
                        myEventsCount = _myModeratorEventIds.Count;
                        break;
                    case "жюри":
                        myEventsCount = _myJuryEventIds.Count;
                        break;
                }
            }

            if (_showMyEventsOnly)
            {
                string roleText = "Мои мероприятия";
                if (_currentUser != null)
                {
                    switch (_currentRole.ToLower())
                    {
                        case "участник":
                            roleText = "Мои мероприятия";
                            break;
                        case "модератор":
                            roleText = "Мои мероприятия (модератор)";
                            break;
                        case "жюри":
                            roleText = "Мои мероприятия (жюри)";
                            break;
                    }
                }
                EventsCountTextBlock.Text = $"{roleText}: {filteredCount} из {myEventsCount}";
            }
            else if (totalCount == filteredCount)
            {
                EventsCountTextBlock.Text = $"Всего мероприятий: {totalCount}";
            }
            else
            {
                EventsCountTextBlock.Text = $"Показано: {filteredCount} из {totalCount}";
            }
        }

        private void DirectionFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyDateFilterButtonClick(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFiltersButtonClick(object sender, RoutedEventArgs e)
        {
            ResetAllFilters();
        }

        private void ResetAllFilters()
        {
            if (DirectionFilterComboBox.Items.Count > 0)
            {
                DirectionFilterComboBox.SelectedIndex = 0;
            }

            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            if (_showMyEventsOnly)
            {
                _showMyEventsOnly = false;
                UpdateMyEventsButtonAppearance();
            }

            ApplyFilters();

            MessageBox.Show("Все фильтры сброшены", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EventCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null && int.TryParse(border.Tag.ToString(), out int eventId))
            {
                NavigateToEventDetails(eventId);
            }
        }

        private void NavigateToEventDetails(int eventId)
        {
            var eventDetailsPage = new EventDetailsPage(eventId, _currentUser, _currentRole);
            NavigationService.Navigate(eventDetailsPage);
        }

        private void MyEventsButtonClick(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null ||
                (_currentRole.ToLower() != "участник" &&
                 _currentRole.ToLower() != "модератор" &&
                 _currentRole.ToLower() != "жюри"))
            {
                MessageBox.Show("Эта функция доступна только для участников, модераторов и жюри",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _showMyEventsOnly = !_showMyEventsOnly;
            UpdateMyEventsButtonAppearance();
            ApplyFilters();
        }

        private void UpdateMyEventsButtonAppearance()
        {
            if (_showMyEventsOnly)
            {
                MyEventsButton.Background = new SolidColorBrush(Color.FromRgb(84, 111, 148));
                MyEventsButton.Foreground = Brushes.White;
                MyEventsButton.Content = "Все мероприятия";
                MyEventsButton.ToolTip = "Показать все мероприятия";
            }
            else
            {
                MyEventsButton.Background = new SolidColorBrush(Color.FromRgb(171, 207, 206));
                MyEventsButton.Foreground = Brushes.Black;
                MyEventsButton.Content = "Мои мероприятия";
                MyEventsButton.ToolTip = "Показать только мероприятия, на которые вы зарегистрированы";
            }
        }

        private void ProfileButtonClick(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null)
            {
                var profilePage = new ProfilePage(_currentUser);
                NavigationService.Navigate(profilePage);
            }
            else
            {
                MessageBox.Show("Для доступа к профилю необходимо авторизоваться",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMyRegistrations();
            LoadEvents();
            MessageBox.Show("Список мероприятий обновлен",
                "Обновлено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public class EventViewModel
        {
            public int event_id { get; set; }
            public string event_name { get; set; }
            public int direction_id { get; set; }
            public string direction_name { get; set; }
            public DateTime start_date { get; set; }
            public DateTime end_date { get; set; }
            public int days_count { get; set; }
            public string logo_path { get; set; }
            public string description { get; set; }
            public int? organizer_id { get; set; }
            public string organizer_name { get; set; }
            public string city_name { get; set; }
            public string date_range { get; set; }
            public bool is_upcoming { get; set; }
            public int duration_days { get; set; }
            public bool is_registered { get; set; }

            public List<string> ModeratorActivities { get; set; } = new List<string>();
            public List<string> JuryActivities { get; set; } = new List<string>();
            public string RoleInfo { get; set; }
        }
    }
}