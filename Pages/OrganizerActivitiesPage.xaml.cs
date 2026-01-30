using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UP._02._01_Vybornov.Pages
{
    public partial class OrganizerActivitiesPage : Page
    {
        private users _currentUser;
        private List<ActivityViewModel> _allActivities = new List<ActivityViewModel>();
        private List<events> _allEvents = new List<events>();
        private int? _selectedEventId = null;
        private bool _sortAscending = true;
        private static AddEditActivityWindow _currentEditWindow = null;

        public OrganizerActivitiesPage(users user, int? eventId = null)
        {
            InitializeComponent();
            _currentUser = user;
            _selectedEventId = eventId;
            Loaded += OrganizerActivitiesPage_Loaded;
        }

        private void OrganizerActivitiesPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEvents();
            LoadActivities();
            UpdateSortButtons();
        }

        private void LoadEvents()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    _allEvents = context.events
                        .Where(e => e.organizer_id == _currentUser.user_id)
                        .OrderBy(e => e.start_date)
                        .ToList();

                    EventFilterComboBox.Items.Clear();

                    var allItem = new ComboBoxItem { Content = "Все мероприятия", Tag = "all" };
                    EventFilterComboBox.Items.Add(allItem);

                    foreach (var ev in _allEvents)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = $"{ev.event_name} ({ev.start_date:dd.MM.yyyy})",
                            Tag = ev.event_id
                        };
                        EventFilterComboBox.Items.Add(item);
                    }

                    if (_selectedEventId.HasValue)
                    {
                        foreach (ComboBoxItem item in EventFilterComboBox.Items)
                        {
                            if (item.Tag is int eventId && eventId == _selectedEventId.Value)
                            {
                                EventFilterComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    else
                    {
                        EventFilterComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadActivities()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var activities = context.activities.ToList();
                    var events = context.events.ToList();
                    var juryActivities = context.jury_activities.ToList();

                    _allActivities.Clear();

                    foreach (var activity in activities)
                    {
                        var eventObj = events.FirstOrDefault(e => e.event_id == activity.event_id);

                        if (eventObj?.organizer_id != _currentUser.user_id)
                            continue;

                        var viewModel = new ActivityViewModel
                        {
                            ActivityId = activity.activity_id,
                            ActivityName = activity.activity_name,
                            Description = activity.description,
                            EventId = activity.event_id,
                            ActivityDay = activity.activity_day,
                            StartTime = activity.start_time,
                            DurationMinutes = activity.duration_minutes ?? 90
                        };

                        if (eventObj != null)
                        {
                            viewModel.EventName = eventObj.event_name;
                            viewModel.EventStartDate = eventObj.start_date;
                        }

                        bool hasJury = juryActivities.Any(ja => ja.activity_id == activity.activity_id);
                        viewModel.HasJury = hasJury;
                        viewModel.HasJuryText = hasJury ? "Есть жюри" : "Нет жюри";
                        viewModel.HasJuryColor = hasJury ? Brushes.Orange : Brushes.Green;

                        viewModel.StartTimeFormatted = activity.start_time.ToString(@"hh\:mm");
                        viewModel.Duration = $"{viewModel.DurationMinutes} мин.";

                        _allActivities.Add(viewModel);
                    }

                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активностей:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filteredActivities = _allActivities.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                string searchText = SearchTextBox.Text.ToLower();
                filteredActivities = filteredActivities.Where(a =>
                    (a.ActivityName?.ToLower() ?? "").Contains(searchText) ||
                    (a.Description?.ToLower() ?? "").Contains(searchText));
            }

            if (EventFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag?.ToString() != "all" && selectedItem.Tag is int eventId)
                {
                    filteredActivities = filteredActivities.Where(a => a.EventId == eventId);
                }
            }

            if (_sortAscending)
            {
                filteredActivities = filteredActivities
                    .OrderBy(a => a.EventStartDate)
                    .ThenBy(a => a.ActivityDay)
                    .ThenBy(a => a.StartTime);
            }
            else
            {
                filteredActivities = filteredActivities
                    .OrderByDescending(a => a.EventStartDate)
                    .ThenByDescending(a => a.ActivityDay)
                    .ThenByDescending(a => a.StartTime);
            }

            ActivitiesItemsControl.ItemsSource = filteredActivities.ToList();
        }

        private void UpdateSortButtons()
        {
            if (_sortAscending)
            {
                SortAscButton.Background = (SolidColorBrush)FindResource("AccentColor");
                SortAscButton.Foreground = Brushes.White;
                SortDescButton.Background = (SolidColorBrush)FindResource("SecondaryBackground");
                SortDescButton.Foreground = (SolidColorBrush)FindResource("TextColor");
            }
            else
            {
                SortDescButton.Background = (SolidColorBrush)FindResource("AccentColor");
                SortDescButton.Foreground = Brushes.White;
                SortAscButton.Background = (SolidColorBrush)FindResource("SecondaryBackground");
                SortAscButton.Foreground = (SolidColorBrush)FindResource("TextColor");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var organizerPage = new OrganizerPage(_currentUser);
            NavigationService.Navigate(organizerPage);
        }

        private void AddActivityButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEditWindow != null)
            {
                MessageBox.Show("Пожалуйста, закройте окно редактирования перед созданием новой активности.",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _currentEditWindow.Focus();
                return;
            }

            _currentEditWindow = new AddEditActivityWindow(_currentUser);
            _currentEditWindow.Closed += EditWindow_Closed;
            _currentEditWindow.ShowDialog();
        }

        private void EditActivityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int activityId)
            {
                OpenEditWindow(activityId);
            }
        }

        private void ActivityCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int activityId)
            {
                OpenEditWindow(activityId);
            }
        }

        private void OpenEditWindow(int activityId)
        {
            if (_currentEditWindow != null)
            {
                MessageBox.Show("Пожалуйста, закройте текущее окно редактирования перед открытием другого.",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _currentEditWindow.Focus();
                return;
            }

            _currentEditWindow = new AddEditActivityWindow(_currentUser, activityId);
            _currentEditWindow.Closed += EditWindow_Closed;
            _currentEditWindow.ShowDialog();
        }

        private void EditWindow_Closed(object sender, EventArgs e)
        {
            if (_currentEditWindow != null)
            {
                if (_currentEditWindow.IsSaved)
                {
                    LoadActivities();
                }

                _currentEditWindow.Closed -= EditWindow_Closed;
                _currentEditWindow = null;
            }
        }

        private void DeleteActivityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int activityId)
            {
                try
                {
                    using (var context = new ConferenceDBEntities())
                    {
                        var activity = context.activities.Find(activityId);
                        if (activity == null)
                        {
                            MessageBox.Show("Активность не найдена",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        bool hasJury = context.jury_activities.Any(ja => ja.activity_id == activityId);
                        if (hasJury)
                        {
                            MessageBox.Show("Невозможно удалить активность, так как для неё уже назначено жюри.\n" +
                                "Сначала удалите всех членов жюри из этой активности.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        var result = MessageBox.Show(
                            $"Вы уверены, что хотите удалить активность?\n\n" +
                            $"Название: {activity.activity_name}\n" +
                            $"Это действие нельзя будет отменить.",
                            "Подтверждение удаления",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            var moderatorActivities = context.moderator_activities
                                .Where(ma => ma.activity_id == activityId)
                                .ToList();
                            context.moderator_activities.RemoveRange(moderatorActivities);

                            context.activities.Remove(activity);
                            context.SaveChanges();

                            MessageBox.Show("Активность успешно удалена",
                                "Успешно",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            LoadActivities();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении активности:\n{ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void EventFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SortAscButton_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = true;
            UpdateSortButtons();
            ApplyFilters();
        }

        private void SortDescButton_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = false;
            UpdateSortButtons();
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadActivities();
            MessageBox.Show("Список активностей обновлен",
                "Обновлено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public class ActivityViewModel
        {
            public int ActivityId { get; set; }
            public string ActivityName { get; set; }
            public string Description { get; set; }
            public int EventId { get; set; }
            public string EventName { get; set; }
            public DateTime EventStartDate { get; set; }
            public int ActivityDay { get; set; }
            public TimeSpan StartTime { get; set; }
            public string StartTimeFormatted { get; set; }
            public int DurationMinutes { get; set; }
            public string Duration { get; set; }
            public bool HasJury { get; set; }
            public string HasJuryText { get; set; }
            public Brush HasJuryColor { get; set; }
        }
    }
}