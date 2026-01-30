using Microsoft.Win32;
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
using System.Windows.Shapes;

namespace UP._02._01_Vybornov.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddEditEventWindow.xaml
    /// </summary>
    public partial class AddEditEventWindow : Window
    {
        private users _currentUser;
        private int _eventId = 0;
        private bool _isEditMode = false;
        public bool IsSaved { get; private set; } = false;

        private List<directions> _directions = new List<directions>();
        private List<cities> _cities = new List<cities>();

        // Конструктор для добавления нового мероприятия
        public AddEditEventWindow(users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _isEditMode = false;

            Loaded += AddEditEventWindow_Loaded;
        }

        // Конструктор для редактирования существующего мероприятия
        public AddEditEventWindow(users currentUser, int eventId)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _eventId = eventId;
            _isEditMode = eventId > 0;

            Loaded += AddEditEventWindow_Loaded;
        }

        private void AddEditEventWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFormData();
        }

        private void LoadFormData()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Загружаем направления
                    _directions = context.directions
                        .OrderBy(d => d.direction_name)
                        .ToList();
                    DirectionComboBox.ItemsSource = _directions;

                    // Загружаем города
                    _cities = context.cities
                        .OrderBy(c => c.city_name)
                        .ToList();
                    CityComboBox.ItemsSource = _cities;

                    if (_isEditMode)
                    {
                        // Режим редактирования
                        TitleTextBlock.Text = "Редактирование мероприятия";
                        Title = "Редактирование мероприятия";

                        // Загружаем данные мероприятия
                        var ev = context.events.Find(_eventId);
                        if (ev != null)
                        {
                            // ID
                            EventIdLabel.Visibility = Visibility.Visible;
                            EventIdTextBox.Visibility = Visibility.Visible;
                            EventIdTextBox.Text = ev.event_id.ToString();

                            // Название
                            NameTextBox.Text = ev.event_name;

                            // Логотип
                            LogoPathTextBox.Text = ev.logo_path;

                            // Направление
                            if (ev.direction_id > 0)
                            {
                                DirectionComboBox.SelectedItem = _directions
                                    .FirstOrDefault(d => d.direction_id == ev.direction_id);
                            }

                            // Город
                            var cityEvent = context.city_event
                                .FirstOrDefault(ce => ce.event_id == ev.event_id);
                            if (cityEvent != null)
                            {
                                CityComboBox.SelectedItem = _cities
                                    .FirstOrDefault(c => c.city_id == cityEvent.city_id);
                            }

                            // Даты
                            StartDatePicker.SelectedDate = ev.start_date;
                            EndDatePicker.SelectedDate = ev.end_date;

                            // Описание
                            DescriptionTextBox.Text = ev.description;
                        }
                        else
                        {
                            MessageBox.Show("Мероприятие не найдено", "Ошибка",
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

        private bool ValidateForm()
        {
            bool isValid = true;

            // Сбрасываем ошибки
            NameErrorText.Visibility = Visibility.Collapsed;
            DirectionErrorText.Visibility = Visibility.Collapsed;
            CityErrorText.Visibility = Visibility.Collapsed;
            DateErrorText.Visibility = Visibility.Collapsed;

            // Проверка названия
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Проверка направления
            if (DirectionComboBox.SelectedItem == null)
            {
                DirectionErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Проверка города
            if (CityComboBox.SelectedItem == null)
            {
                CityErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Проверка дат
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                DateErrorText.Text = "Выберите дату начала и окончания";
                DateErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value)
            {
                DateErrorText.Text = "Дата окончания не может быть раньше даты начала";
                DateErrorText.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void BrowseLogoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                LogoPathTextBox.Text = openFileDialog.FileName;
            }
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
                        // Редактирование существующего мероприятия
                        var ev = context.events.Find(_eventId);
                        if (ev != null)
                        {
                            ev.event_name = NameTextBox.Text.Trim();
                            ev.direction_id = ((directions)DirectionComboBox.SelectedItem).direction_id;
                            ev.start_date = StartDatePicker.SelectedDate.Value;
                            ev.end_date = EndDatePicker.SelectedDate.Value;
                            ev.days_count = (ev.end_date - ev.start_date).Days + 1;
                            ev.logo_path = LogoPathTextBox.Text.Trim();
                            ev.description = DescriptionTextBox.Text.Trim();

                            // Обновляем город
                            var cityEvent = context.city_event.FirstOrDefault(ce => ce.event_id == ev.event_id);
                            if (cityEvent != null)
                            {
                                cityEvent.city_id = ((cities)CityComboBox.SelectedItem).city_id;
                            }
                            else
                            {
                                // Создаем новую связь, если ее нет
                                cityEvent = new city_event
                                {
                                    id = context.city_event.Any() ? context.city_event.Max(ce => ce.id) + 1 : 1,
                                    event_id = ev.event_id,
                                    city_id = ((cities)CityComboBox.SelectedItem).city_id
                                };
                                context.city_event.Add(cityEvent);
                            }

                            context.SaveChanges();

                            MessageBox.Show("Мероприятие успешно обновлено",
                                "Успешно",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        // Добавление нового мероприятия
                        var newEvent = new events
                        {
                            event_name = NameTextBox.Text.Trim(),
                            direction_id = ((directions)DirectionComboBox.SelectedItem).direction_id,
                            start_date = StartDatePicker.SelectedDate.Value,
                            end_date = EndDatePicker.SelectedDate.Value,
                            days_count = (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days + 1,
                            logo_path = LogoPathTextBox.Text.Trim(),
                            description = DescriptionTextBox.Text.Trim(),
                            organizer_id = _currentUser.user_id
                        };

                        context.events.Add(newEvent);
                        context.SaveChanges();

                        // Создаем связь с городом
                        var cityEvent = new city_event
                        {
                            id = context.city_event.Any() ? context.city_event.Max(ce => ce.id) + 1 : 1,
                            event_id = newEvent.event_id,
                            city_id = ((cities)CityComboBox.SelectedItem).city_id
                        };
                        context.city_event.Add(cityEvent);

                        // Создаем временную сетку для активностей
                        CreateActivityTimeSlots(context, newEvent);

                        context.SaveChanges();

                        MessageBox.Show("Мероприятие успешно создано!\nАвтоматически создана временная сетка для активностей.",
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
                MessageBox.Show($"Ошибка при сохранении мероприятия:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CreateActivityTimeSlots(ConferenceDBEntities context, events newEvent)
        {
            DateTime currentDate = newEvent.start_date;
            int dayCounter = 1;

            // Время начала и окончания каждого дня (например, с 9:00 до 18:00)
            TimeSpan dayStartTime = new TimeSpan(9, 0, 0); // 9:00
            TimeSpan dayEndTime = new TimeSpan(18, 0, 0); // 18:00
            TimeSpan activityDuration = new TimeSpan(1, 30, 0); // 1 час 30 минут
            TimeSpan breakDuration = new TimeSpan(0, 15, 0); // 15 минут

            while (currentDate <= newEvent.end_date)
            {
                TimeSpan currentTime = dayStartTime;

                while (currentTime + activityDuration <= dayEndTime)
                {
                    // Создаем активность
                    var activity = new activities
                    {
                        activity_name = $"Активность день {dayCounter} - {currentTime:hh\\:mm}",
                        description = $"Запланированная активность {currentTime:hh\\:mm} - {currentTime.Add(activityDuration):hh\\:mm}",
                        event_id = newEvent.event_id,
                        activity_day = dayCounter,
                        start_time = currentTime,
                        duration_minutes = 90 // Фиксированная продолжительность
                    };

                    context.activities.Add(activity);

                    // Добавляем перерыв
                    currentTime = currentTime.Add(activityDuration).Add(breakDuration);
                }

                currentDate = currentDate.AddDays(1);
                dayCounter++;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}