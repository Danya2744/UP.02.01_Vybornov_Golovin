using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP._02._01_Vybornov.Pages
{
    public partial class AddEditActivityWindow : Window
    {
        private users _currentUser;
        private int _activityId = 0;
        private bool _isEditMode = false;
        public bool IsSaved { get; private set; } = false;

        private List<events> _events = new List<events>();
        private List<activities> _existingActivities = new List<activities>();

        // Конструктор для добавления новой активности
        public AddEditActivityWindow(users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _isEditMode = false;

            Loaded += AddEditActivityWindow_Loaded;
        }

        // Конструктор для редактирования существующей активности
        public AddEditActivityWindow(users currentUser, int activityId)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _activityId = activityId;
            _isEditMode = activityId > 0;

            Loaded += AddEditActivityWindow_Loaded;
        }

        private void AddEditActivityWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFormData();
        }

        private void LoadFormData()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Загружаем мероприятия, где текущий пользователь является организатором
                    _events = context.events
                        .Where(e => e.organizer_id == _currentUser.user_id)
                        .OrderBy(e => e.start_date)
                        .ToList();
                    EventComboBox.ItemsSource = _events;

                    // Загружаем существующие активности ДРУГИМ СПОСОБОМ
                    // Сначала получаем ID мероприятий
                    var eventIds = _events.Select(e => e.event_id).ToList();

                    // Теперь загружаем активности для этих мероприятий
                    _existingActivities = context.activities
                        .Where(a => eventIds.Contains(a.event_id))
                        .ToList();

                    if (_isEditMode)
                    {
                        // Режим редактирования
                        TitleTextBlock.Text = "Редактирование активности";
                        Title = "Редактирование активности";

                        // Загружаем данные активности
                        var activity = context.activities.Find(_activityId);
                        if (activity != null)
                        {
                            // ID
                            ActivityIdLabel.Visibility = Visibility.Visible;
                            ActivityIdTextBox.Visibility = Visibility.Visible;
                            ActivityIdTextBox.Text = activity.activity_id.ToString();

                            // Название
                            NameTextBox.Text = activity.activity_name;

                            // Описание
                            DescriptionTextBox.Text = activity.description;

                            // Мероприятие
                            var ev = _events.FirstOrDefault(e => e.event_id == activity.event_id);
                            EventComboBox.SelectedItem = ev;

                            // Загружаем дни и время после выбора мероприятия
                            if (ev != null)
                            {
                                LoadDaysForEvent(ev.event_id);
                                LoadAvailableTimes(ev.event_id, activity.activity_day, activity.start_time);

                                // Выбираем день
                                foreach (ComboBoxItem dayItem in DayComboBox.Items)
                                {
                                    if (dayItem.Tag is int day && day == activity.activity_day)
                                    {
                                        DayComboBox.SelectedItem = dayItem;
                                        break;
                                    }
                                }

                                // Выбираем время
                                foreach (ComboBoxItem timeItem in TimeComboBox.Items)
                                {
                                    if (timeItem.Tag is TimeSpan time && time == activity.start_time)
                                    {
                                        TimeComboBox.SelectedItem = timeItem;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Активность не найдена", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных формы:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void EventComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventComboBox.SelectedItem is events selectedEvent)
            {
                LoadDaysForEvent(selectedEvent.event_id);
                DayComboBox.SelectedIndex = -1;
                TimeComboBox.Items.Clear();
                TimeInfoBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadDaysForEvent(int eventId)
        {
            try
            {
                DayComboBox.Items.Clear();

                using (var context = new ConferenceDBEntities())
                {
                    var ev = context.events.Find(eventId);
                    if (ev != null)
                    {
                        int daysCount = (ev.end_date - ev.start_date).Days + 1;

                        for (int day = 1; day <= daysCount; day++)
                        {
                            var item = new ComboBoxItem
                            {
                                Content = $"День {day}",
                                Tag = day
                            };
                            DayComboBox.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дней мероприятия:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DayComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (EventComboBox.SelectedItem is events selectedEvent &&
                DayComboBox.SelectedItem is ComboBoxItem selectedDay &&
                selectedDay.Tag is int day)
            {
                LoadAvailableTimes(selectedEvent.event_id, day);
            }
        }

        private void LoadAvailableTimes(int eventId, int day, TimeSpan? currentTime = null)
        {
            try
            {
                TimeComboBox.Items.Clear();
                TimeInfoBorder.Visibility = Visibility.Collapsed;

                using (var context = new ConferenceDBEntities())
                {
                    var ev = context.events.Find(eventId);
                    if (ev == null) return;

                    // Параметры дня
                    TimeSpan dayStart = new TimeSpan(9, 0, 0); // 9:00
                    TimeSpan dayEnd = new TimeSpan(18, 0, 0); // 18:00
                    TimeSpan activityDuration = new TimeSpan(1, 30, 0); // 1 час 30 минут
                    TimeSpan breakDuration = new TimeSpan(0, 15, 0); // 15 минут

                    // Получаем существующие активности для этого дня и мероприятия
                    var existingActivities = _existingActivities
                        .Where(a => a.event_id == eventId && a.activity_day == day)
                        .OrderBy(a => a.start_time)
                        .ToList();

                    List<TimeSpan> availableTimes = new List<TimeSpan>();
                    TimeSpan currentTimeSlot = dayStart;
                    int availableSlots = 0;

                    // Пока текущее время + продолжительность активности не превышает конец дня
                    while (currentTimeSlot + activityDuration <= dayEnd)
                    {
                        // Проверяем, не пересекается ли этот слот с существующими активностями
                        bool isSlotAvailable = true;
                        TimeSpan slotEnd = currentTimeSlot + activityDuration;

                        foreach (var activity in existingActivities)
                        {
                            TimeSpan activityEnd = activity.start_time +
                                TimeSpan.FromMinutes(activity.duration_minutes ?? 90);

                            // Проверяем пересечение (включая обязательный перерыв в 15 минут)
                            if (currentTimeSlot < activityEnd + breakDuration &&
                                slotEnd + breakDuration > activity.start_time)
                            {
                                // Если это редактирование текущей активности, разрешаем ее же время
                                if (_isEditMode && currentTime.HasValue &&
                                    activity.activity_id == _activityId &&
                                    currentTime.Value == activity.start_time)
                                {
                                    // Разрешаем текущее время редактируемой активности
                                    isSlotAvailable = true;
                                }
                                else
                                {
                                    isSlotAvailable = false;
                                }
                                break;
                            }
                        }

                        // Если слот доступен, добавляем его в список
                        if (isSlotAvailable)
                        {
                            availableTimes.Add(currentTimeSlot);
                            availableSlots++;
                        }

                        // Переходим к следующему слоту (активность + перерыв)
                        currentTimeSlot = currentTimeSlot.Add(activityDuration).Add(breakDuration);
                    }

                    // Заполняем ComboBox доступными временами
                    foreach (var time in availableTimes)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = time.ToString(@"hh\:mm"),
                            Tag = time
                        };
                        TimeComboBox.Items.Add(item);
                    }

                    // Показываем информацию о доступных слотах
                    if (availableSlots > 0)
                    {
                        TimeInfoText.Text = $"Доступно {availableSlots} временных слотов. " +
                                           $"Каждый слот - 90 минут активности с 15-минутным перерывом после.";
                        TimeInfoBorder.Visibility = Visibility.Visible;
                        TimeInfoBorder.Background = System.Windows.Media.Brushes.LightGreen;
                        TimeInfoBorder.BorderBrush = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        TimeInfoText.Text = "Нет доступных временных слотов на этот день. " +
                                           "Все слоты заняты существующими активностями.";
                        TimeInfoBorder.Visibility = Visibility.Visible;
                        TimeInfoBorder.Background = System.Windows.Media.Brushes.LightPink;
                        TimeInfoBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                    }

                    // В режиме редактирования выбираем текущее время
                    if (_isEditMode && currentTime.HasValue)
                    {
                        foreach (ComboBoxItem item in TimeComboBox.Items)
                        {
                            if (item.Tag is TimeSpan time && time == currentTime.Value)
                            {
                                TimeComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки доступного времени:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateForm()
        {
            bool isValid = true;

            // Сбрасываем ошибки
            NameErrorText.Visibility = Visibility.Collapsed;
            EventErrorText.Visibility = Visibility.Collapsed;
            DayErrorText.Visibility = Visibility.Collapsed;
            TimeErrorText.Visibility = Visibility.Collapsed;

            // Проверка названия
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Проверка мероприятия
            if (EventComboBox.SelectedItem == null)
            {
                EventErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Проверка дня
            if (DayComboBox.SelectedItem == null)
            {
                DayErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Проверка времени
            if (TimeComboBox.SelectedItem == null)
            {
                TimeErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Пожалуйста, исправьте ошибки в форме",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    if (_isEditMode)
                    {
                        // Редактирование существующей активности
                        var activity = context.activities.Find(_activityId);
                        if (activity != null)
                        {
                            activity.activity_name = NameTextBox.Text.Trim();
                            activity.description = DescriptionTextBox.Text.Trim();
                            activity.event_id = ((events)EventComboBox.SelectedItem).event_id;
                            activity.activity_day = (int)((ComboBoxItem)DayComboBox.SelectedItem).Tag;
                            activity.start_time = (TimeSpan)((ComboBoxItem)TimeComboBox.SelectedItem).Tag;
                            activity.duration_minutes = 90; // Фиксированная продолжительность

                            context.SaveChanges();

                            MessageBox.Show("Активность успешно обновлена",
                                "Успешно",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        // Добавление новой активности
                        var newActivity = new activities
                        {
                            activity_name = NameTextBox.Text.Trim(),
                            description = DescriptionTextBox.Text.Trim(),
                            event_id = ((events)EventComboBox.SelectedItem).event_id,
                            activity_day = (int)((ComboBoxItem)DayComboBox.SelectedItem).Tag,
                            start_time = (TimeSpan)((ComboBoxItem)TimeComboBox.SelectedItem).Tag,
                            duration_minutes = 90 // Фиксированная продолжительность
                        };

                        context.activities.Add(newActivity);
                        context.SaveChanges();

                        MessageBox.Show("Активность успешно создана",
                            "Успешно",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }

                    IsSaved = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении активности:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            DayComboBox.DropDownClosed += DayComboBox_DropDownClosed;
        }
    }
}