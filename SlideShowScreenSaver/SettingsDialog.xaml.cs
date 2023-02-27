using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SlideShowScreenSaver
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog
    {
        private readonly Settings Settings;
        public SettingsDialog(Settings settings)
        {
            InitializeComponent();
            this.Settings = settings;
            this.SliderTiming.Value = this.Settings.Timing;
            this.TextBlockCurrentPath.Text = this.Settings.PhotoFolder;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SliderTiming.ValueChanged += SliderTiming_ValueChanged;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void SliderTiming_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Settings != null)
            {
                this.Settings.Timing = Convert.ToInt32(e.NewValue);
            }
        }

        private void ButtonSelectFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetFolderFromUserUsingOpenFileDialog("Select a folder with photos in it", this.Settings.PhotoFolder,
                out string path))
            {

                this.Settings.PhotoFolder = path;
                this.TextBlockCurrentPath.Text = path;
            }
        }

        /// <summary>
        /// Prompt the user for a folder location via an an open file dialog. 
        /// </summary>
        /// <returns>The selected path, otherwise null </returns>
        public static bool TryGetFolderFromUserUsingOpenFileDialog(string title, string initialFolder, out string selectedFolderPath)
        {
            selectedFolderPath = string.Empty;
            using (CommonOpenFileDialog folderSelectionDialog = new CommonOpenFileDialog()
            {
                Title = title,
                DefaultDirectory = initialFolder,
                IsFolderPicker = true,
                EnsurePathExists = false,
                Multiselect = false

            })
            {
                folderSelectionDialog.InitialDirectory = folderSelectionDialog.DefaultDirectory;
                if (folderSelectionDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    selectedFolderPath = folderSelectionDialog.FileName;
                    return true;
                }
                return false;
            }
        }
    }
}
