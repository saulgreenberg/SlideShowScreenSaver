using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

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
            // Initialize the settings from the values in the registry
            this.Settings = settings;
            this.SliderTiming.Value = this.Settings.Timing;
            this.SliderDisplayFontSize.Value = this.Settings.DisplayFontSize;
            this.TextBlockCurrentPath.Text = this.Settings.PhotoFolder;

            this.CBShowFileName.IsChecked = this.Settings.ShowFileName;
            this.CBShowFileName.Checked += CBShowFileName_CheckChanged;
            this.CBShowFileName.Unchecked += CBShowFileName_CheckChanged;
             
            this.RBFile.IsChecked = this.Settings.DisplayByFileName;
            this.RBFile.Checked += this.RBFileNaming_CheckChanged;

            this.RBFolder.IsChecked = this.Settings.DisplayByFolderName;
            this.RBFolder.Checked += this.RBFileNaming_CheckChanged;

            this.RBFolderFile.IsChecked = this.Settings.DisplayByFolderFileName;
            this.RBFolderFile.Checked += this.RBFileNaming_CheckChanged;

            this.RBPath.IsChecked = this.Settings.DisplayByPath;
            this.RBPath.Checked += this.RBFileNaming_CheckChanged;

            this.SetRBVisibility(this.CBShowFileName.IsChecked == true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SliderTiming.ValueChanged += SliderTiming_ValueChanged;
            SliderDisplayFontSize.ValueChanged += SliderDisplayFontSize_ValueChanged;
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

        private void SliderDisplayFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Settings != null)
            {
                this.Settings.DisplayFontSize = Convert.ToInt32(e.NewValue);
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

        // Prompt the user for a folder location via an an open file dialog. 
        // returns true/false. If true returns the selected path
        public static bool TryGetFolderFromUserUsingOpenFileDialog(string title, string initialFolder, out string selectedFolderPath)
        {
            selectedFolderPath = string.Empty;
            using CommonOpenFileDialog folderSelectionDialog = new CommonOpenFileDialog()
            {
                Title = title,
                DefaultDirectory = initialFolder,
                IsFolderPicker = true,
                EnsurePathExists = false,
                Multiselect = false

            };
            folderSelectionDialog.InitialDirectory = folderSelectionDialog.DefaultDirectory;
            if (folderSelectionDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                selectedFolderPath = folderSelectionDialog.FileName;
                return true;
            }
            return false;
        }

        private void CBShowFileName_CheckChanged(object sender, RoutedEventArgs e)
        {
            this.Settings.ShowFileName = this.CBShowFileName.IsChecked == true;
            this.SetRBVisibility(this.CBShowFileName.IsChecked == true);
        }

        private void RBFileNaming_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                switch (rb.Name)
                {
                    case "RBFile":
                        this.Settings.DisplayByFileName = true;
                        this.Settings.DisplayByFolderName = false;
                        this.Settings.DisplayByFolderFileName = false;
                        this.Settings.DisplayByPath = false;
                        break;
                    case "RBFolder":
                        this.Settings.DisplayByFileName = false;
                        this.Settings.DisplayByFolderName = true;
                        this.Settings.DisplayByFolderFileName = false;
                        this.Settings.DisplayByPath = false;
                        break;
                    case "RBFolderFile":
                        this.Settings.DisplayByFileName = false;
                        this.Settings.DisplayByFolderName = false;
                        this.Settings.DisplayByFolderFileName = true;
                        this.Settings.DisplayByPath = false;
                        break;
                    case "RBPath":
                        this.Settings.DisplayByFileName = false;
                        this.Settings.DisplayByFolderName = false;
                        this.Settings.DisplayByFolderFileName = false;
                        this.Settings.DisplayByPath = true;
                        break;
                }
            }
        }

        private void SetRBVisibility(bool enabled)
        {
            this.RBFile.IsEnabled = enabled;
            this.RBFolderFile.IsEnabled = enabled;
            this.RBPath.IsEnabled = enabled;
        }
    }
}
