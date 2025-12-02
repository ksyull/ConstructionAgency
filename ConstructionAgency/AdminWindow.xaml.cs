using ConstructionAgency.ConstructionProjectsDBDataSetTableAdapters;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        public class ProjectPhoto
        {
            public int PhotoId { get; set; }
            public int ProjectId { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public DateTime UploadDate { get; set; }
            public string ProjectTitle { get; set; }
        }

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

            LoadProjectsForPhotos();
        }


        private void LoadProjectsForPhotos()
        {
            try
            {
                PhotosProjectComboBox.ItemsSource = _projectsTable;
                PhotosProjectComboBox.DisplayMemberPath = "Title";
                PhotosProjectComboBox.SelectedValuePath = "ProjectId";

                if (_projectsTable.Count > 0)
                    PhotosProjectComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке проектов для фото: {ex.Message}");
            }
        }

        private void LoadProjectPhotos(int projectId)
        {
            try
            {
                var projectPhotos = new List<ProjectPhoto>();

                string targetFolder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "ProjectPhotos",
                    projectId.ToString());

                if (Directory.Exists(targetFolder))
                {
                    var files = Directory.GetFiles(targetFolder);
                    int photoId = 1;

                    foreach (var file in files)
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg" ||
                            extension == ".png" || extension == ".bmp")
                        {
                            projectPhotos.Add(new ProjectPhoto
                            {
                                PhotoId = photoId++,
                                ProjectId = projectId,
                                FileName = Path.GetFileName(file),
                                FilePath = file,
                                UploadDate = File.GetLastWriteTime(file),
                                ProjectTitle = GetProjectTitle(projectId)
                            });
                        }
                    }
                }

                PhotosItemsControl.ItemsSource = projectPhotos;
                PhotosInfo.Text = $"Найдено фотографий: {projectPhotos.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фото: {ex.Message}");
            }
        }

        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (PhotosProjectComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите проект для добавления фото");
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg; *.jpeg; *.png; *.bmp",
                Multiselect = true,
                Title = "Выберите фотографии для проекта"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                int projectId = (int)PhotosProjectComboBox.SelectedValue;
                string targetFolder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "ProjectPhotos",
                    projectId.ToString());

                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                int successCount = 0;
                foreach (string filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destPath = Path.Combine(targetFolder, fileName);

                        File.Copy(filePath, destPath, true);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении файла {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }
                LoadProjectPhotos(projectId);
                PhotosInfo.Text = $"Успешно добавлено фотографий: {successCount}";
            }
        }

        private void DeletePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var border = button.Parent as Border;
                if (border == null) return;

                var stackPanel = border.Child as StackPanel;
                if (stackPanel == null) return;

                var textBlocks = stackPanel.Children.OfType<TextBlock>().ToList();
                if (textBlocks.Count < 2) return;

                string fileName = textBlocks[0].Text;

                if (PhotosProjectComboBox.SelectedValue == null) return;
                int projectId = (int)PhotosProjectComboBox.SelectedValue;

                var result = MessageBox.Show($"Удалить фотографию '{fileName}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string targetFolder = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "ProjectPhotos",
                            projectId.ToString());
                        string fullPath = Path.Combine(targetFolder, fileName);

                        var image = stackPanel.Children.OfType<Image>().FirstOrDefault();
                        if (image != null)
                        {
                            image.Source = null;
                        }

                        border.Visibility = Visibility.Collapsed;

                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }

                        LoadProjectPhotos(projectId);

                        PhotosInfo.Text = $"Фотография '{fileName}' удалена!";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}\n\nФотография удалена из списка, но файл может остаться на диске.");

                        if (PhotosProjectComboBox.SelectedValue != null)
                        {
                            int selectedProjectId = (int)PhotosProjectComboBox.SelectedValue;
                            LoadProjectPhotos(selectedProjectId);
                        }
                    }
                }
            }
        }

        private void PhotosProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PhotosProjectComboBox.SelectedValue != null)
            {
                int projectId = (int)PhotosProjectComboBox.SelectedValue;
                LoadProjectPhotos(projectId);
            }
        }

        private string GetProjectTitle(int projectId)
        {
            var project = _projectsTable.FirstOrDefault(p => p.ProjectId == projectId);
            return project?.Title ?? "Неизвестный проект";
        }


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
            var desc = ProjectDescriptionBox.Text != null ? ProjectDescriptionBox.Text.Trim() : "";

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

                LoadProjectsForPhotos();
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
                using (var cmd = new SqlCommand("DELETE FROM Projects WHERE ProjectId = @id", con))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = projectId;
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                LoadProjects();
                ProjectMsg.Text = "Проект удалён.";

                LoadProjectsForPhotos();
            }
            catch (Exception ex)
            {
                ProjectMsg.Text = "Ошибка удаления: " + ex.Message;
            }
        }
    }
}