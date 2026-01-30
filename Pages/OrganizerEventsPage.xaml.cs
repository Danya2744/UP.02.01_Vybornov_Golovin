using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UP._02._01_Vybornov.Pages
{
    public partial class OrganizerEventsPage : Page
    {
        private users _currentUser;
        private List<EventViewModel> _allEvents = new List<EventViewModel>();
        private List<directions> _allDirections = new List<directions>();

        public OrganizerEventsPage(users user)
        {
            InitializeComponent();
            _currentUser = user;
            Loaded += OrganizerEventsPage_Loaded;
        }

        private void OrganizerEventsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEvents();
            LoadDirections();
        }

        private void LoadEvents()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var events = context.events
                        .Where(e => e.organizer_id == _currentUser.user_id)
                        .ToList();

                    var directions = context.directions.ToList();
                    var cities = context.cities.ToList();
                    var cityEvents = context.city_event.ToList();

                    var allActivities = context.activities.ToList();

                    _allEvents.Clear();

                    foreach (var ev in events)
                    {
                        var viewModel = new EventViewModel
                        {
                            EventId = ev.event_id,
                            EventName = ev.event_name,
                            DirectionId = ev.direction_id,
                            StartDate = ev.start_date,
                            EndDate = ev.end_date,
                            DaysCount = ev.days_count,
                            LogoPath = ev.logo_path,
                            Description = ev.description,
                            OrganizerId = ev.organizer_id
                        };

                        var direction = directions.FirstOrDefault(d => d.direction_id == ev.direction_id);
                        viewModel.DirectionName = direction?.direction_name ?? "Не указано";

                        var cityEvent = cityEvents.FirstOrDefault(ce => ce.event_id == ev.event_id);
                        var city = cityEvent != null ?
                            cities.FirstOrDefault(c => c.city_id == cityEvent.city_id) : null;
                        viewModel.CityName = city?.city_name ?? "Не указан";

                        if (ev.start_date == ev.end_date)
                        {
                            viewModel.DateRange = ev.start_date.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            viewModel.DateRange = $"{ev.start_date:dd.MM.yyyy} - {ev.end_date:dd.MM.yyyy}";
                        }

                        if (ev.end_date < DateTime.Today)
                        {
                            viewModel.StatusText = "Завершено";
                            viewModel.StatusColor = Brushes.Gray;
                        }
                        else if (ev.start_date > DateTime.Today)
                        {
                            viewModel.StatusText = "Предстоящее";
                            viewModel.StatusColor = Brushes.Green;
                        }
                        else
                        {
                            viewModel.StatusText = "В процессе";
                            viewModel.StatusColor = Brushes.Orange;
                        }

                        viewModel.ActivitiesCount = allActivities
                            .Count(a => a.event_id == ev.event_id);

                        if (!string.IsNullOrEmpty(ev.logo_path))
                        {
                            if (!ev.logo_path.StartsWith("http") && !ev.logo_path.StartsWith("/"))
                            {
                                viewModel.LogoPath = "/" + ev.logo_path.TrimStart('\\', '/');
                            }
                            else
                            {
                                viewModel.LogoPath = ev.logo_path;
                            }
                        }
                        else
                        {
                            viewModel.LogoPath = "/Resources/default_event.png";
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
            var filteredEvents = _allEvents.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                string searchText = SearchTextBox.Text.ToLower();
                filteredEvents = filteredEvents.Where(e =>
                    (e.EventName?.ToLower() ?? "").Contains(searchText) ||
                    (e.Description?.ToLower() ?? "").Contains(searchText) ||
                    (e.CityName?.ToLower() ?? "").Contains(searchText));
            }

            var selectedDirection = DirectionFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedDirection?.Tag?.ToString() != "all")
            {
                if (selectedDirection?.Tag is int directionId)
                {
                    filteredEvents = filteredEvents.Where(e => e.DirectionId == directionId);
                }
            }

            if (StartDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                filteredEvents = filteredEvents.Where(e => e.StartDate >= startDate);
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                DateTime endDate = EndDatePicker.SelectedDate.Value;
                filteredEvents = filteredEvents.Where(e => e.EndDate <= endDate);
            }

            EventsItemsControl.ItemsSource = filteredEvents.ToList();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var organizerPage = new OrganizerPage(_currentUser);
            NavigationService.Navigate(organizerPage);
        }

        private void AddEventButton_Click(object sender, RoutedEventArgs e)
        {
            var addEventWindow = new AddEditEventWindow(_currentUser);
            addEventWindow.ShowDialog();

            if (addEventWindow.IsSaved)
            {
                LoadEvents();
            }
        }

        private void EditEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int eventId)
            {
                var editEventWindow = new AddEditEventWindow(_currentUser, eventId);
                editEventWindow.ShowDialog();

                if (editEventWindow.IsSaved)
                {
                    LoadEvents();
                }
            }
        }

        private void ManageActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int eventId)
            {
                var activitiesPage = new OrganizerActivitiesPage(_currentUser, eventId);
                NavigationService.Navigate(activitiesPage);
            }
        }

        private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int eventId)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить это мероприятие?\n" +
                    "Это действие нельзя будет отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new ConferenceDBEntities())
                        {
                            var hasActivities = context.activities
                                .Any(a => a.event_id == eventId);

                            if (hasActivities)
                            {
                                MessageBox.Show("Невозможно удалить мероприятие, так как у него есть активности.\n" +
                                    "Сначала удалите все активности этого мероприятия.",
                                    "Ошибка",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }

                            var ev = context.events.Find(eventId);
                            if (ev != null)
                            {
                                context.events.Remove(ev);
                                context.SaveChanges();

                                MessageBox.Show("Мероприятие успешно удалено",
                                    "Успешно",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                                LoadEvents();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении мероприятия:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DirectionFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEvents();
            MessageBox.Show("Список мероприятий обновлен",
                "Обновлено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public class EventViewModel
        {
            public int EventId { get; set; }
            public string EventName { get; set; }
            public int DirectionId { get; set; }
            public string DirectionName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int DaysCount { get; set; }
            public string LogoPath { get; set; }
            public string Description { get; set; }
            public int? OrganizerId { get; set; }
            public string CityName { get; set; }
            public string DateRange { get; set; }
            public int ActivitiesCount { get; set; }
            public string StatusText { get; set; }
            public Brush StatusColor { get; set; }
        }
    }
}