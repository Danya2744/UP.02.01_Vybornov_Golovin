using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class LoginPage : Page
    {
        public event EventHandler<UserLoggedInEventArgs> UserLoggedIn;
        public event EventHandler GuestLoggedIn;

        public LoginPage()
        {
            InitializeComponent();
            Loaded += LoginPage_Loaded;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            IdNumberTextBox.Focus();
        }

        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            string idNumber = IdNumberTextBox.Text.Trim();
            string password = GetCurrentPassword();

            if (string.IsNullOrEmpty(idNumber))
            {
                ShowErrorMessage("Введите ID Number", "Ошибка ввода", IdNumberTextBox);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowErrorMessage("Введите пароль", "Ошибка ввода", PasswordBox);
                return;
            }

            try
            {
                using (var context = new ConferenceDBEntities())
                {
                    var user = context.users
                        .FirstOrDefault(u => u.id_number == idNumber && u.password_hash == password);

                    if (user != null)
                    {
                        var role = context.roles.FirstOrDefault(r => r.role_id == user.role_id);
                        string roleName = role?.role_name ?? "участник";

                        ShowSuccessMessage($"Добро пожаловать, {user.full_name}!\nРоль: {CapitalizeFirstLetter(roleName)}");
                        
                        OnUserLoggedIn(user, roleName);
                        ClearFields();
                    }
                    else
                    {
                        ShowErrorMessage("Неверный ID Number или пароль\nПожалуйста, проверьте введенные данные",
                            "Ошибка авторизации", PasswordBox);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowDatabaseError(ex);
            }
        }

        private void GuestButtonClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы хотите войти как гость?\n\n",
                "Вход как гость",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ShowSuccessMessage("Вы вошли как гость\nДоступен просмотр мероприятий", "Гостевой доступ");
                OnGuestLoggedIn();
                ClearFields();
            }
        }

        private void TogglePasswordVisibilityClick(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                VisiblePasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                VisiblePasswordTextBox.Visibility = Visibility.Visible;
                (sender as Button).Content = "🙈";
            }
            else
            {
                PasswordBox.Password = VisiblePasswordTextBox.Text;
                VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                (sender as Button).Content = "👁";
            }
        }


        private string GetCurrentPassword()
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                return PasswordBox.Password;
            }
            else
            {
                return VisiblePasswordTextBox.Text;
            }
        }

        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private void ShowErrorMessage(string message, string title, Control focusControl = null)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            focusControl?.Focus();
            if (focusControl is TextBox textBox)
                textBox.SelectAll();
            else if (focusControl is PasswordBox passwordBox)
                passwordBox.SelectAll();
        }

        private void ShowSuccessMessage(string message, string title = "Успешно")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowDatabaseError(Exception ex)
        {
            MessageBox.Show(
                $"Ошибка подключения к базе данных:\n\n{ex.Message}",
                "Ошибка подключения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void ClearFields()
        {
            IdNumberTextBox.Clear();
            PasswordBox.Clear();
            VisiblePasswordTextBox.Clear();
            PasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
        }

        protected virtual void OnUserLoggedIn(users user, string roleName)
        {
            UserLoggedIn?.Invoke(this, new UserLoggedInEventArgs(user, roleName));
        }

        protected virtual void OnGuestLoggedIn()
        {
            GuestLoggedIn?.Invoke(this, EventArgs.Empty);
        }
    }

    public class UserLoggedInEventArgs : EventArgs
    {
        public users User { get; }
        public string RoleName { get; }

        public UserLoggedInEventArgs(users user, string roleName)
        {
            User = user;
            RoleName = roleName;
        }
    }
}
