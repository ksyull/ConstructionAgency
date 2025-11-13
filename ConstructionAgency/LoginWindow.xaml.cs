using System.Linq;
using System.Windows;
using ConstructionAgency.ConstructionProjectsDBDataSetTableAdapters;

namespace ConstructionAgency
{
    public partial class LoginWindow : Window
    {
        private readonly UsersTableAdapter _usersAdapter = new UsersTableAdapter();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;
            var login = LoginBox.Text?.Trim();
            var password = PwdBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Введите логин и пароль.";
                return;
            }

            var users = _usersAdapter.GetData();
            var user = users.FirstOrDefault(r =>
                (r.UserName.Equals(login) || r.Email.Equals(login)) &&
                 r.PasswordHash.Equals(password));

            if (user == null)
            {
                ErrorText.Text = "Неверный логин или пароль.";
                return;
            }

            if (user.Role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase))
            {
                var w = new AdminWindow(user.UserId, user.UserName);
                w.Show();
            }
            else
            {
                var w = new UserWindow(user.UserId, user.UserName);
                w.Show();
            }

            Close();
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().ShowDialog();
        }
    }
}
