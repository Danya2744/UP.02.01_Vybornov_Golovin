using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace UP._02._01_Vybornov.Pages
{
    public partial class ParticipantsPage : Page
    {
        private int _eventId;
        private users _currentUser;
        private string _currentRole;

        public ParticipantsPage(int eventId, users user = null, string role = null)
        {
            InitializeComponent();
            _eventId = eventId;
            _currentUser = user;
            _currentRole = role;

            Loaded += ParticipantsPage_Loaded;
        }

        private void ParticipantsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEventInfo();
            LoadParticipants();
        }

        private void LoadEventInfo()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var ev = context.events.FirstOrDefault(e => e.event_id == _eventId);
                    if (ev != null)
                    {
                        EventTitleTextBlock.Text = ev.event_name;

                        var cityEvent = context.city_event.FirstOrDefault(ce => ce.event_id == _eventId);
                        var city = cityEvent != null ?
                            context.cities.FirstOrDefault(c => c.city_id == cityEvent.city_id) : null;

                        EventInfoTextBlock.Text = $"Город: {city?.city_name ?? "Не указан"} | " +
                                                 $"Дата: {ev.start_date:dd.MM.yyyy} - {ev.end_date:dd.MM.yyyy}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации о мероприятии: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadParticipants()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var participants = context.event_registrations
                        .Where(r => r.event_id == _eventId && r.status.ToLower() == "registered")
                        .Join(context.users,
                              r => r.user_id,
                              u => u.user_id,
                              (r, u) => new ParticipantViewModel
                              {
                                  UserId = u.user_id,
                                  FullName = u.full_name,
                                  IdNumber = u.id_number,
                                  PhotoPath = u.photo_path,
                                  RegistrationDate = r.registration_date
                              })
                        .OrderBy(p => p.RegistrationDate)
                        .ToList();

                    if (participants.Any())
                    {
                        ParticipantsItemsControl.ItemsSource = participants;
                        NoParticipantsText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ParticipantsItemsControl.ItemsSource = null;
                        NoParticipantsText.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        public class ParticipantViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string IdNumber { get; set; }
            public string PhotoPath { get; set; }
            public DateTime RegistrationDate { get; set; }
        }
    }
}