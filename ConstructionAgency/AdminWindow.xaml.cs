using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ConstructionAgency.ConstructionProjectsDBDataSetTableAdapters;

namespace ConstructionAgency
{
    public partial class AdminWindow : Window
    {
        private readonly int _currentUserId;
        private readonly string _currentUserName;

        private readonly UsersTableAdapter _usersAdapter = new UsersTableAdapter();
        private readonly ProjectsTableAdapter _projectsAdapter = new ProjectsTableAdapter();
        private readonly CategoriesTableAdapter _categoriesAdapter = new CategoriesTableAdapter();

        private ConstructionProjectsDBDataSet.UsersDataTable _usersTable = new ConstructionProjectsDBDataSet.UsersDataTable();
        private ConstructionProjectsDBDataSet.ProjectsDataTable _projectsTable = new ConstructionProjectsDBDataSet.ProjectsDataTable();
        private ConstructionProjectsDBDataSet.CategoriesDataTable _categoriesTable = new ConstructionProjectsDBDataSet.CategoriesDataTable();

        public AdminWindow(int currentUserId, string currentUserName)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            _currentUserName = currentUserName;
            Loaded += AdminWindow_Loaded;
        }

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUsers();
            LoadCategories();
            LoadProjects();
            ProjectStatusBox.SelectedIndex = 1; // Open
        }

        // ===== Users =====
        private void LoadUsers()
        {
            _usersTable = _usersAdapter.GetData();
            UsersGrid.ItemsSource = _usersTable;
            UsersInfo.Text = "Всего: " + _usersTable.Count;
        }

        private void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var drv = UsersGrid.SelectedItem as DataRowView;
            if (drv == null)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            var row = (ConstructionProjectsDBDataSet.UsersRow)drv.Row;

            if (row.UserId == _currentUserId ||
                row.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Нельзя удалить текущего пользователя или встроенного администратора.");
                return;
            }

            if (MessageBox.Show("Удалить пользователя '" + row.UserName + "'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                var cs = Properties.Settings.Default.ConstructionProjectsDBConnectionString;
                using (var con = new SqlConnection(cs))
                using (var cmd = new SqlCommand("DELETE FROM Users WHERE UserId=@id", con))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = row.UserId;
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                LoadUsers();
                MessageBox.Show("Пользователь удалён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }

        // ===== Projects =====
        private void LoadCategories()
        {
            _categoriesTable = _categoriesAdapter.GetData();
            ProjectCategoryBox.ItemsSource = _categoriesTable;
            if (_categoriesTable.Count > 0) ProjectCategoryBox.SelectedIndex = 0;
        }

        private void LoadProjects()
        {
            _projectsTable = _projectsAdapter.GetData();
            ProjectsGrid.ItemsSource = _projectsTable;
        }

        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            ProjectMsg.Text = "";

            var title = ProjectTitleBox.Text != null ? ProjectTitleBox.Text.Trim() : "";
            var desc  = ProjectDescriptionBox.Text != null ? ProjectDescriptionBox.Text.Trim() : "";

            string status = "Draft";
            var statusItem = ProjectStatusBox.SelectedItem as ComboBoxItem;
            if (statusItem != null && statusItem.Content != null)
                status = statusItem.Content.ToString();

            if (string.IsNullOrWhiteSpace(title) || ProjectCategoryBox.SelectedValue == null)
            {
                ProjectMsg.Text = "Укажите название и категорию.";
                return;
            }

            var categoryId = (int)ProjectCategoryBox.SelectedValue;

            try
            {
                var cs = Properties.Settings.Default.ConstructionProjectsDBConnectionString;
                using (var con = new SqlConnection(cs))
                using (var cmd = new SqlCommand(
                    "INSERT INTO Projects (Title, Description, CategoryId, Status) VALUES (@t,@d,@c,@s)", con))
                {
                    cmd.Parameters.Add("@t", SqlDbType.NVarChar, 200).Value = title;
                    if (string.IsNullOrWhiteSpace(desc))
                        cmd.Parameters.Add("@d", SqlDbType.NVarChar).Value = DBNull.Value;
                    else
                        cmd.Parameters.Add("@d", SqlDbType.NVarChar).Value = desc;

                    cmd.Parameters.Add("@c", SqlDbType.Int).Value = categoryId;
                    cmd.Parameters.Add("@s", SqlDbType.NVarChar, 50).Value = status;
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                ProjectTitleBox.Clear();
                ProjectDescriptionBox.Clear();
                LoadProjects();
                ProjectMsg.Text = "Проект добавлен.";
            }
            catch (Exception ex)
            {
                ProjectMsg.Text = "Ошибка: " + ex.Message;
            }
        }

        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            ProjectMsg.Text = "";

            var drv = ProjectsGrid.SelectedItem as DataRowView;
            if (drv == null)
            {
                MessageBox.Show("Выберите проект в списке.");
                return;
            }

            int projectId = (int)drv["ProjectId"];
            string title = drv["Title"] != DBNull.Value ? drv["Title"].ToString() : ("ID " + projectId);

            if (MessageBox.Show("Удалить проект \"" + title + "\"?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var cs = Properties.Settings.Default.ConstructionProjectsDBConnectionString;

            try
            {
                using (var con = new SqlConnection(cs))
                using (var cmd = new SqlCommand(@"
BEGIN TRY
    BEGIN TRAN;
    IF OBJECT_ID('UserFavorites','U') IS NOT NULL
        DELETE FROM UserFavorites WHERE ProjectId = @id;
    DELETE FROM Projects WHERE ProjectId = @id;
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
", con))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = projectId;
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                LoadProjects();
                ProjectMsg.Text = "Проект удалён.";
            }
            catch (Exception ex)
            {
                ProjectMsg.Text = "Ошибка удаления: " + ex.Message;
            }
        }
    }
}
