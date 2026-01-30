using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace UP._02._01_Vybornov.Pages
{
    public partial class ProfilePage : Page
    {
        private users _currentUser;
        private bool _isPasswordChanging = false;
        private string _newPhotoPath = null;

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
                        // Проверяем, существует ли файл
                        if (File.Exists(photoPath))
                        {
                            UserPhotoImage.Source = new BitmapImage(new Uri(photoPath, UriKind.Absolute));
                        }
                        else
                        {
                            // Если путь относительный, пробуем загрузить из ресурсов
                            string relativePath = photoPath.StartsWith("/") ? photoPath : "/" + photoPath;
                            UserPhotoImage.Source = new BitmapImage(new Uri(relativePath, UriKind.Relative));
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
            _newPhotoPath = null;
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

                    // Обновляем данные
                    user.full_name = FullNameTextBox.Text.Trim();
                    user.phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                    user.birth_date = BirthDatePicker.SelectedDate;

                    // Обработка нового фото
                    if (!string.IsNullOrEmpty(_newPhotoPath))
                    {
                        // Сохраняем фото в папку Resources
                        string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

                        // Создаем папку Resources, если она не существует
                        if (!Directory.Exists(resourcesPath))
                        {
                            Directory.CreateDirectory(resourcesPath);
                        }

                        // Генерируем уникальное имя файла
                        string fileName = $"user_{user.user_id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(_newPhotoPath)}";
                        string destinationPath = Path.Combine(resourcesPath, fileName);

                        try
                        {
                            // Копируем файл в папку Resources
                            File.Copy(_newPhotoPath, destinationPath, true);

                            // Сохраняем относительный путь в базу данных
                            user.photo_path = $"/Resources/{fileName}";

                            // Обновляем изображение
                            LoadUserPhoto(user.photo_path);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка сохранения фото: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

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
                    _newPhotoPath = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для изменения фото
        private void PhotoBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PhotoOverlay.Opacity = 0.7;
        }

        private void PhotoBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            PhotoOverlay.Opacity = 0;
        }

        private void PhotoBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeProfilePhoto();
        }

        private void ChangeProfilePhoto()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*",
                Title = "Выберите фото профиля",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Проверяем размер файла (максимум 5MB)
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024) // 5MB
                    {
                        MessageBox.Show("Размер файла не должен превышать 5MB", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Загружаем изображение для предпросмотра
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    UserPhotoImage.Source = bitmap;

                    // Сохраняем путь к новому фото
                    _newPhotoPath = openFileDialog.FileName;

                    MessageBox.Show("Фото загружено. Нажмите 'Сохранить изменения' для применения.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            PhotoOverlay.Opacity = 0.7;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            PhotoOverlay.Opacity = 0;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeProfilePhoto();
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