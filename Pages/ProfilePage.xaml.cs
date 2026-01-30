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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UP._02._01_Vybornov.Pages
{
    public partial class ProfilePage : Page
    {
        private users _currentUser;
        private bool _isEditing = false;

        public ProfilePage(users user)
        {
            InitializeComponent();
            _currentUser = user;

            Loaded += ProfilePage_Loaded;
        }

        private void ProfilePage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserProfile();
        }

        private void LoadUserProfile()
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Пользователь не найден", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.GoBack();
                return;
            }

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    // Загружаем данные пользователя
                    var user = context.users.FirstOrDefault(u => u.user_id == _currentUser.user_id);

                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    // Заполняем поля
                    IdNumberTextBox.Text = user.id_number;
                    FullNameTextBox.Text = user.full_name;
                    EmailTextBox.Text = user.email;
                    PhoneTextBox.Text = user.phone ?? "";

                    if (user.birth_date.HasValue)
                    {
                        BirthDatePicker.SelectedDate = user.birth_date.Value;
                    }

                    // Роль
                    var role = context.roles.FirstOrDefault(r => r.role_id == user.role_id);
                    RoleTextBox.Text = role?.role_name ?? "Не указана";

                    // Фото пользователя
                    if (!string.IsNullOrEmpty(user.photo_path))
                    {
                        try
                        {
                            if (System.IO.File.Exists(user.photo_path))
                            {
                                UserPhotoImage.Source = new BitmapImage(new Uri(user.photo_path, UriKind.Absolute));
                            }
                            else
                            {
                                UserPhotoImage.Source = new BitmapImage(new Uri("/Resources/default_user.png", UriKind.Relative));
                            }
                        }
                        catch
                        {
                            UserPhotoImage.Source = new BitmapImage(new Uri("/Resources/default_user.png", UriKind.Relative));
                        }
                    }
                    else
                    {
                        UserPhotoImage.Source = new BitmapImage(new Uri("/Resources/default_user.png", UriKind.Relative));
                    }

                    // Загружаем статистику
                    LoadUserStatistics(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserStatistics(ConferenceDBEntities context)
        {
            // Здесь будет загрузка статистики пользователя
            // Например: количество зарегистрированных мероприятий и т.д.

            EventsCountText.Text = "0";
            ActiveEventsText.Text = "0";
            VisitedEventsText.Text = "0";
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                var eventsPage = new EventsPage(_currentUser, GetUserRole());
                NavigationService.Navigate(eventsPage);
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            // Сбрасываем изменения
            LoadUserProfile();
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var user = context.users.FirstOrDefault(u => u.user_id == _currentUser.user_id);

                    if (user == null)
                        return;

                    // Обновляем данные
                    user.full_name = FullNameTextBox.Text.Trim();
                    user.email = EmailTextBox.Text.Trim();
                    user.phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                    user.birth_date = BirthDatePicker.SelectedDate;

                    // Смена пароля
                    if (!string.IsNullOrEmpty(CurrentPasswordBox.Password) &&
                        !string.IsNullOrEmpty(NewPasswordBox.Password))
                    {
                        if (user.password_hash == CurrentPasswordBox.Password)
                        {
                            if (NewPasswordBox.Password == ConfirmPasswordBox.Password)
                            {
                                user.password_hash = NewPasswordBox.Password;
                            }
                            else
                            {
                                MessageBox.Show("Новые пароли не совпадают", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Текущий пароль указан неверно", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    context.SaveChanges();

                    // Обновляем текущего пользователя
                    _currentUser = user;

                    MessageBox.Show("Данные успешно сохранены", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем поля паролей
                    CurrentPasswordBox.Clear();
                    NewPasswordBox.Clear();
                    ConfirmPasswordBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Введите ФИО", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                FullNameTextBox.Focus();
                return false;
            }

            // Проверка email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                !EmailTextBox.Text.Contains("@"))
            {
                MessageBox.Show("Введите корректный email", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                EmailTextBox.Focus();
                return false;
            }

            // Проверка паролей
            if (!string.IsNullOrEmpty(CurrentPasswordBox.Password) ||
                !string.IsNullOrEmpty(NewPasswordBox.Password) ||
                !string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                if (string.IsNullOrEmpty(CurrentPasswordBox.Password))
                {
                    MessageBox.Show("Введите текущий пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentPasswordBox.Focus();
                    return false;
                }

                if (string.IsNullOrEmpty(NewPasswordBox.Password))
                {
                    MessageBox.Show("Введите новый пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    NewPasswordBox.Focus();
                    return false;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Новые пароли не совпадают", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ConfirmPasswordBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private string GetUserRole()
        {
            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var role = context.roles.FirstOrDefault(r => r.role_id == _currentUser.role_id);
                    return role?.role_name ?? "участник";
                }
            }
            catch
            {
                return "участник";
            }
        }
    }
}
