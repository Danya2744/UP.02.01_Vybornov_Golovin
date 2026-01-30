using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace UP._02._01_Vybornov.Pages
{
    public partial class ProfilePage : Page
    {
        private users _currentUser;
        private bool _isPasswordChanging = false;

        public ProfilePage(users user)
        {
            InitializeComponent();
            _currentUser = user;

            Loaded += ProfilePage_Loaded;
            InitializeValidation();
        }

        private void InitializeValidation()
        {
            // Подписываемся на события изменения полей
            FullNameTextBox.TextChanged += ValidateField_TextChanged;
            PhoneTextBox.TextChanged += ValidateField_TextChanged;
            CurrentPasswordBox.PasswordChanged += PasswordField_PasswordChanged;
            NewPasswordBox.PasswordChanged += PasswordField_PasswordChanged;
            ConfirmPasswordBox.PasswordChanged += PasswordField_PasswordChanged;
            BirthDatePicker.SelectedDateChanged += BirthDatePicker_SelectedDateChanged;
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
                    LoadUserPhoto(user.photo_path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserPhoto(string photoPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(photoPath))
                {
                    try
                    {
                        if (System.IO.File.Exists(photoPath))
                        {
                            UserPhotoImage.Source = new BitmapImage(new Uri(photoPath, UriKind.Absolute));
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
            }
            catch
            {
                UserPhotoImage.Source = new BitmapImage(new Uri("/Resources/default_user.png", UriKind.Relative));
            }
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
            ClearPasswordFields();
            HideAllErrorMessages();
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateAllFields())
                return;

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var user = context.users.FirstOrDefault(u => u.user_id == _currentUser.user_id);

                    if (user == null)
                        return;

                    // Проверяем уникальность email (если изменился)
                    string oldEmail = user.email;
                    if (EmailTextBox.Text != oldEmail)
                    {
                        var emailExists = context.users.Any(u => u.email == EmailTextBox.Text && u.user_id != user.user_id);
                        if (emailExists)
                        {
                            MessageBox.Show("Этот email уже используется другим пользователем", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Обновляем данные
                    user.full_name = FullNameTextBox.Text.Trim();
                    user.phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                    user.birth_date = BirthDatePicker.SelectedDate;

                    // Смена пароля
                    if (_isPasswordChanging)
                    {
                        if (user.password_hash != CurrentPasswordBox.Password)
                        {
                            ShowError(CurrentPasswordErrorText, "Текущий пароль указан неверно");
                            CurrentPasswordBox.Focus();
                            return;
                        }

                        user.password_hash = NewPasswordBox.Password;
                    }

                    context.SaveChanges();

                    // Обновляем текущего пользователя
                    _currentUser = user;

                    MessageBox.Show("Данные успешно сохранены", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем поля паролей
                    ClearPasswordFields();
                    HideAllErrorMessages();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Валидация полей

        private bool ValidateAllFields()
        {
            HideAllErrorMessages();

            bool isValid = true;

            // Валидация ФИО
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                ShowError(FullNameErrorText, "Поле ФИО обязательно для заполнения");
                isValid = false;
            }
            else if (FullNameTextBox.Text.Length < 2)
            {
                ShowError(FullNameErrorText, "ФИО должно содержать минимум 2 символа");
                isValid = false;
            }
            else if (FullNameTextBox.Text.Length > 150)
            {
                ShowError(FullNameErrorText, "ФИО не должно превышать 150 символов");
                isValid = false;
            }

            // Валидация телефона
            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                string phone = PhoneTextBox.Text.Trim();
                if (!IsValidPhoneNumber(phone))
                {
                    ShowError(PhoneErrorText, "Введите корректный номер телефона");
                    isValid = false;
                }
            }

            // Валидация даты рождения
            if (BirthDatePicker.SelectedDate.HasValue)
            {
                if (BirthDatePicker.SelectedDate.Value > DateTime.Now)
                {
                    ShowError(BirthDateErrorText, "Дата рождения не может быть в будущем");
                    isValid = false;
                }
                else if (BirthDatePicker.SelectedDate.Value < new DateTime(1900, 1, 1))
                {
                    ShowError(BirthDateErrorText, "Дата рождения не может быть ранее 1900 года");
                    isValid = false;
                }
                else if ((DateTime.Now - BirthDatePicker.SelectedDate.Value).TotalDays / 365 < 14)
                {
                    ShowError(BirthDateErrorText, "Пользователь должен быть старше 14 лет");
                    isValid = false;
                }
            }

            // Валидация паролей
            bool anyPasswordFilled = !string.IsNullOrEmpty(CurrentPasswordBox.Password) ||
                                     !string.IsNullOrEmpty(NewPasswordBox.Password) ||
                                     !string.IsNullOrEmpty(ConfirmPasswordBox.Password);

            bool allPasswordsFilled = !string.IsNullOrEmpty(CurrentPasswordBox.Password) &&
                                      !string.IsNullOrEmpty(NewPasswordBox.Password) &&
                                      !string.IsNullOrEmpty(ConfirmPasswordBox.Password);

            if (anyPasswordFilled && !allPasswordsFilled)
            {
                if (string.IsNullOrEmpty(CurrentPasswordBox.Password))
                    ShowError(CurrentPasswordErrorText, "Введите текущий пароль");

                if (string.IsNullOrEmpty(NewPasswordBox.Password))
                    ShowError(NewPasswordErrorText, "Введите новый пароль");

                if (string.IsNullOrEmpty(ConfirmPasswordBox.Password))
                    ShowError(ConfirmPasswordErrorText, "Повторите новый пароль");

                isValid = false;
            }

            if (allPasswordsFilled)
            {
                _isPasswordChanging = true;

                if (NewPasswordBox.Password.Length < 6)
                {
                    ShowError(NewPasswordErrorText, "Пароль должен содержать минимум 6 символов");
                    isValid = false;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    ShowError(ConfirmPasswordErrorText, "Пароли не совпадают");
                    isValid = false;
                }
            }
            else
            {
                _isPasswordChanging = false;
            }

            return isValid;
        }

        private bool IsValidPhoneNumber(string phone)
        {
            // Простая валидация телефона
            if (string.IsNullOrWhiteSpace(phone))
                return true;

            // Удаляем все нецифровые символы
            string digitsOnly = Regex.Replace(phone, @"[^\d]", "");

            // Проверяем длину номера (с кодом страны минимум 10 цифр)
            return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
        }

        private void ValidateField_TextChanged(object sender, TextChangedEventArgs e)
        {
            HideAllErrorMessages();
        }

        private void PasswordField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            HideAllErrorMessages();
        }

        private void BirthDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            HideAllErrorMessages();
        }

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideAllErrorMessages()
        {
            FullNameErrorText.Visibility = Visibility.Collapsed;
            PhoneErrorText.Visibility = Visibility.Collapsed;
            BirthDateErrorText.Visibility = Visibility.Collapsed;
            CurrentPasswordErrorText.Visibility = Visibility.Collapsed;
            NewPasswordErrorText.Visibility = Visibility.Collapsed;
            ConfirmPasswordErrorText.Visibility = Visibility.Collapsed;
        }

        private void ClearPasswordFields()
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            _isPasswordChanging = false;
        }

        #endregion

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