using System;
using System.Windows;
using System.Windows.Controls;
using WorkTracker.ViewModels;

namespace WorkTracker.Views
{
    public partial class EditTimelineBlockWindow : Window
    {
        public string SelectedBlockCategory { get; private set; } = "Away";
        public string SelectedBlockDescription { get; private set; } = string.Empty;

        public EditTimelineBlockWindow(TimelineBlock block, DateTime date)
        {
            InitializeComponent();

            TxtInterval.Text = block.TimeLabel;
            TxtDescription.Text = block.AppDetails.Contains(" (Active:") || block.AppDetails.Contains(" (Planned)") || block.AppDetails.Contains(" (")
                ? string.Empty 
                : block.AppDetails;
            
            // Set category selection
            foreach (ComboBoxItem item in CbCategory.Items)
            {
                if (item.Content.ToString() == block.Category)
                {
                    CbCategory.SelectedItem = item;
                    break;
                }
            }
            if (CbCategory.SelectedItem == null)
            {
                CbCategory.SelectedIndex = 0; // Default to Work
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (CbCategory.SelectedItem is ComboBoxItem item)
            {
                SelectedBlockCategory = item.Content.ToString() ?? "Away";
            }
            SelectedBlockDescription = TxtDescription.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            SelectedBlockCategory = "Away";
            SelectedBlockDescription = string.Empty;
            DialogResult = true;
            Close();
        }
    }
}
