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

        public EventsPage(users user, string role)
        {
            InitializeComponent();
            _currentUser = user;
            _currentRole = role;

            Loaded += EventsPage_Loaded;
            UpdateNavigationButtons();
        }

        private void EventsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEvents();
            LoadDirections();
        }

        private void UpdateNavigationButtons()
        {
            if (_currentUser != null && _currentRole.ToLower() == "участник")
            {
                // Показываем кнопки для участника
                MyEventsButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
            }
            else if (_currentUser != null)
            {
                // Другие роли (кроме участника)
                ProfileButton.Visibility = Visibility.Visible;
                MyEventsButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Гость - скрываем все кнопки
                MyEventsButton.Visibility = Visibility.Collapsed;
                ProfileButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadEvents()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Загружаем все мероприятия
                    var events = context.events.ToList();
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

                        // Находим направление
                        var direction = directions.FirstOrDefault(d => d.direction_id == ev.direction_id);
                        viewModel.direction_name = direction?.direction_name ?? "Не указано";

                        // Находим организатора
                        if (ev.organizer_id.HasValue)
                        {
                            var organizer = allUsers.FirstOrDefault(u => u.user_id == ev.organizer_id.Value);
                            viewModel.organizer_name = organizer?.full_name ?? "Не указан";
                        }
                        else
                        {
                            viewModel.organizer_name = "Не указан";
                        }

                        // Форматируем даты
                        if (ev.start_date == ev.end_date)
                        {
                            viewModel.date_range = ev.start_date.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            viewModel.date_range = $"{ev.start_date:dd.MM.yyyy} - {ev.end_date:dd.MM.yyyy}";
                        }

                        // Определяем статус мероприятия
                        viewModel.is_upcoming = ev.end_date >= DateTime.Today;
                        viewModel.duration_days = (ev.end_date - ev.start_date).Days + 1;

                        // Находим город проведения
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

                        // Исправляем путь к логотипу, если он относительный
                        if (!string.IsNullOrEmpty(ev.logo_path))
                        {
                            // Если путь не содержит полного URI, добавляем префикс
                            if (!ev.logo_path.StartsWith("http") && !ev.logo_path.StartsWith("/"))
                            {
                                viewModel.logo_path = "/" + ev.logo_path.TrimStart('\\', '/');
                            }
                            else
                            {
                                viewModel.logo_path = ev.logo_path;
                            }
                        }

                        _allEvents.Add(viewModel);
                    }

                    // Показываем все мероприятия (без фильтров)
                    ItemsControlEvents.ItemsSource = _allEvents;
                    UpdateEventsCount();
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

                    // Добавляем элемент "Все направления"
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

                    // Выбираем первый элемент
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
            var filteredEvents = _allEvents.AsEnumerable();

            // Применяем фильтр по направлению
            var selectedDirection = DirectionFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedDirection?.Tag?.ToString() != "all")
            {
                if (selectedDirection?.Tag is int directionId)
                {
                    filteredEvents = filteredEvents.Where(e => e.direction_id == directionId);
                }
            }

            // Применяем фильтр по дате начала (если выбрана)
            if (StartDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                filteredEvents = filteredEvents.Where(e => e.start_date >= startDate);
            }

            // Применяем фильтр по дате окончания (если выбрана)
            if (EndDatePicker.SelectedDate.HasValue)
            {
                DateTime endDate = EndDatePicker.SelectedDate.Value;
                filteredEvents = filteredEvents.Where(e => e.end_date <= endDate);
            }

            // Проверяем, чтобы дата начала была раньше даты окончания (если обе выбраны)
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Обновляем список
            ItemsControlEvents.ItemsSource = filteredEvents.ToList();
            UpdateEventsCount();
        }

        private void UpdateEventsCount()
        {
            int totalCount = _allEvents.Count;
            int filteredCount = ItemsControlEvents.Items.Count;

            if (_showMyEventsOnly)
            {
                EventsCountTextBlock.Text = $"Мои мероприятия: {filteredCount}";
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

        // Обработчики фильтров
        private void DirectionFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Автоматически применяем фильтр при изменении даты
            ApplyFilters();
        }

        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Автоматически применяем фильтр при изменении даты
            ApplyFilters();
        }

        private void ApplyDateFilterButtonClick(object sender, RoutedEventArgs e)
        {
            // Явное применение фильтра
            ApplyFilters();
        }

        private void ResetFiltersButtonClick(object sender, RoutedEventArgs e)
        {
            // Сбрасываем все фильтры
            ResetAllFilters();
        }

        private void ResetAllFilters()
        {
            // Сбрасываем фильтр по направлению
            if (DirectionFilterComboBox.Items.Count > 0)
            {
                DirectionFilterComboBox.SelectedIndex = 0;
            }

            // Очищаем DatePicker
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            // Очищаем текст в DatePicker (если он остался)
            StartDatePicker.Text = "";
            EndDatePicker.Text = "";

            // Показываем все мероприятия
            ItemsControlEvents.ItemsSource = _allEvents;
            UpdateEventsCount();

            MessageBox.Show("Все фильтры сброшены", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Обработчики кликов
        private void EventCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null && int.TryParse(border.Tag.ToString(), out int eventId))
            {
                NavigateToEventDetails(eventId);
            }
        }

        private void RegisterButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int eventId))
            {
                RegisterForEvent(eventId);
            }
        }

        private void NavigateToEventDetails(int eventId)
        {
            var eventDetailsPage = new EventDetailsPage(eventId, _currentUser, _currentRole);
            NavigationService.Navigate(eventDetailsPage);
        }

        private void RegisterForEvent(int eventId)
        {
            if (_currentUser == null || _currentRole.ToLower() != "участник")
            {
                MessageBox.Show("Для регистрации на мероприятия необходимо войти как участник",
                    "Требуется авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Проверяем, не зарегистрирован ли уже пользователь
                    // Здесь будет логика регистрации

                    MessageBox.Show("Регистрация на мероприятие прошла успешно!",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Навигационные кнопки
        private void MyEventsButtonClick(object sender, RoutedEventArgs e)
        {
            _showMyEventsOnly = !_showMyEventsOnly;

            if (_showMyEventsOnly)
            {
                MyEventsButton.Background = (SolidColorBrush)FindResource("AccentColor");
                MyEventsButton.Foreground = Brushes.White;
            }
            else
            {
                MyEventsButton.Background = (SolidColorBrush)FindResource("SecondaryBackground");
                MyEventsButton.Foreground = (SolidColorBrush)FindResource("TextColor");
            }

            ApplyFilters();
        }

        private void ProfileButtonClick(object sender, RoutedEventArgs e)
        {
            var profilePage = new ProfilePage(_currentUser);
            NavigationService.Navigate(profilePage);
        }

        // Класс для отображения мероприятий
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
        }
    }
}