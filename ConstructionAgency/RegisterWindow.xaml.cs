using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using ConstructionAgency.ConstructionProjectsDBDataSetTableAdapters;

namespace ConstructionAgency
{
    public partial class RegisterWindow : Window
    {
        private readonly UsersTableAdapter _usersAdapter = new UsersTableAdapter();

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            var userName = UserNameBox.Text?.Trim();
            var email = EmailBox.Text?.Trim();
            var password = PwdBox.Password;
            var confirm  = ConfirmBox.Password;

            if (string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Заполните имя, email и пароль.";
                return;
            }

            if (!string.Equals(password, confirm))
            {
                ErrorText.Text = "Пароли не совпадают.";
                return;
            }

            var users = _usersAdapter.GetData();
            if (users.Any(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = "Пользователь с таким именем уже существует.";
                return;
            }
            if (users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = "Пользователь с таким email уже существует.";
                return;
            }

            try
            {
                _usersAdapter.Insert(userName, email, password, "User");
                MessageBox.Show("Регистрация успешна!");
                DialogResult = true;
                Close();
            }
            catch (SqlException ex)
            {
                ErrorText.Text = $"Ошибка сохранения: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Не удалось зарегистрировать: {ex.Message}";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}