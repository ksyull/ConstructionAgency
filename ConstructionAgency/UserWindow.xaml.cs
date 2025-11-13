using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ConstructionAgency.ConstructionProjectsDBDataSetTableAdapters;

namespace ConstructionAgency
{
    public partial class UserWindow : Window
    {
        private readonly int _userId;
        private readonly string _userName;

        private readonly ProjectsTableAdapter _projectsAdapter = new ProjectsTableAdapter();
        private readonly CategoriesTableAdapter _categoriesAdapter = new CategoriesTableAdapter();

        private ConstructionProjectsDBDataSet.ProjectsDataTable _projects = new ConstructionProjectsDBDataSet.ProjectsDataTable();
        private ConstructionProjectsDBDataSet.CategoriesDataTable _categories = new ConstructionProjectsDBDataSet.CategoriesDataTable();

        // Локальное избранное (без БД)
        private List<int> _favorites = new List<int>();
        private string FavDir
            => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConstructionAgency");
        private string FavPath
            => System.IO.Path.Combine(FavDir, "favorites_" + _userId + ".json");

        public UserWindow(int userId, string userName)
        {
            InitializeComponent();
            _userId = userId;
            _userName = userName;
            Loaded += UserWindow_Loaded;
        }

        private void UserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _categories = _categoriesAdapter.GetData();
            FilterCategoryBox.ItemsSource = _categories;

            _projects = _projectsAdapter.GetData();
            UserProjectsGrid.ItemsSource = _projects.DefaultView;

            SortBox.SelectedIndex = 0; // Дата: новые → старые
            ApplyFilterAndSort();

            LoadFavoritesLocal();
            RebindFavoritesGrid();
        }

        /* ===== ФИЛЬТР + СОРТИРОВКА ===== */
        private void FilterOrSort_Changed(object sender, RoutedEventArgs e) { ApplyFilterAndSort(); }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterCategoryBox.SelectedItem = null;
            SearchBox.Text = "";
            SortBox.SelectedIndex = 0;
            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            var view = _projects.DefaultView;

            // Категория
            string catFilter = "";
            if (FilterCategoryBox.SelectedValue is int)
                catFilter = "CategoryId = " + (int)FilterCategoryBox.SelectedValue;

            // Поиск по названию
            string q = SearchBox.Text ?? "";
            q = q.Replace("'", "''");
            string searchFilter = string.IsNullOrWhiteSpace(q) ? "" : "Title LIKE '%" + q + "%'";

            string rf = "1=1";
            if (!string.IsNullOrEmpty(catFilter)) rf += " AND " + catFilter;
            if (!string.IsNullOrEmpty(searchFilter)) rf += " AND " + searchFilter;

            view.RowFilter = rf;

            // Сортировка
            string sort = "CreatedAt DESC";
            var sel = SortBox.SelectedItem as ComboBoxItem;
            if (sel != null && sel.Tag != null) sort = sel.Tag.ToString();
            view.Sort = sort;
        }

        /* ===== Детали проекта ===== */
        private void OpenDetails_Click(object sender, RoutedEventArgs e) { OpenDetailsFromGrid(UserProjectsGrid); }

        private void UserProjectsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenDetailsFromGrid(UserProjectsGrid);
        }

        private void OpenFavoriteDetails_Click(object sender, RoutedEventArgs e) { OpenDetailsFromGrid(FavoritesGrid); }

        private void OpenDetailsFromGrid(DataGrid grid)
        {
            var drv = grid.SelectedItem as DataRowView;
            if (drv == null) { MessageBox.Show("Выберите проект."); return; }

            var row = (ConstructionProjectsDBDataSet.ProjectsRow)drv.Row;

            string categoryName = row.CategoryId.ToString();
            var cat = _categories.FindByCategoryId(row.CategoryId);
            if (cat != null) categoryName = cat.Name;

            var w = new ProjectDetailsWindow(
                row.ProjectId,
                row.Title,
                row.IsDescriptionNull() ? "" : row.Description,
                categoryName,
                row.Status,
                row.CreatedAt);
            w.Owner = this;
            w.ShowDialog();
        }

        /* ===== ИЗБРАННОЕ (локально) ===== */
        private void AddFavorite_Click(object sender, RoutedEventArgs e)
        {
            var drv = UserProjectsGrid.SelectedItem as DataRowView;
            if (drv == null) { MessageBox.Show("Выберите проект."); return; }
            int projectId = (int)drv["ProjectId"];

            if (!_favorites.Contains(projectId))
            {
                _favorites.Add(projectId);
                SaveFavoritesLocal();
                RebindFavoritesGrid();
            }
        }

        private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            var drv = UserProjectsGrid.SelectedItem as DataRowView;
            if (drv == null) { MessageBox.Show("Выберите проект."); return; }
            int projectId = (int)drv["ProjectId"];

            if (_favorites.Contains(projectId))
            {
                _favorites.Remove(projectId);
                SaveFavoritesLocal();
                RebindFavoritesGrid();
            }
        }

        private void RemoveFavoriteFromTab_Click(object sender, RoutedEventArgs e)
        {
            var drv = FavoritesGrid.SelectedItem as DataRowView;
            if (drv == null) { MessageBox.Show("Выберите проект."); return; }
            int projectId = (int)drv["ProjectId"];

            if (_favorites.Contains(projectId))
            {
                _favorites.Remove(projectId);
                SaveFavoritesLocal();
                RebindFavoritesGrid();
            }
        }

        private void RebindFavoritesGrid()
        {
            // Собираем таблицу избранного из уже загруженных проектов
            var dt = _projects.Clone();
            foreach (var id in _favorites.Distinct())
            {
                var row = _projects.FindByProjectId(id);
                if (row != null) dt.ImportRow(row);
            }
            FavoritesGrid.ItemsSource = dt.DefaultView;
        }

        private void LoadFavoritesLocal()
        {
            _favorites.Clear();
            try
            {
                if (File.Exists(FavPath))
                {
                    var json = File.ReadAllText(FavPath);
                    var list = JsonConvert.DeserializeObject<List<int>>(json);
                    if (list != null) _favorites = list.Distinct().ToList();
                }
            }
            catch { /* игнор поврежденного файла */ }
        }

        private void SaveFavoritesLocal()
        {
            try
            {
                Directory.CreateDirectory(FavDir);
                var json = JsonConvert.SerializeObject(_favorites.Distinct().ToList());
                File.WriteAllText(FavPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить избранное: " + ex.Message);
            }
        }

    }
}
