using System;
using System.Windows;

namespace ConstructionAgency
{
    public partial class ProjectDetailsWindow : Window
    {
        public ProjectDetailsWindow(int id, string title, string description, string categoryName, string status, DateTime createdAt)
        {
            InitializeComponent();
            TitleText.Text   = title;
            MetaText.Text    = "Категория: " + categoryName;
            StatusText.Text  = "Статус: " + status;
            CreatedText.Text = "Создан: " + createdAt.ToString("g");
            DescText.Text    = string.IsNullOrWhiteSpace(description) ? "(Описание отсутствует)" : description;
        }
    }
}
