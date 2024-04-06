using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace SlideShowScreenSaver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // So we can access this window directly from other objects
        public static MainWindow MainWindowAccess;

        // These contain the settings (stored in the registry) along with defaults that can be adjusted in the Settings dialog 
        // invoked from the Windows screensaver settings.
        public readonly Settings Settings;

        // The timer that operates the slide show
        private readonly DispatcherTimer TimerChangeSlide;
        private readonly DispatcherTimer TimerResumeAfterPause;

        private readonly bool IsPreviewMode;

        private string CurrentSlidePath;

        // Transition effects
        // This eventually allows for more transition effects to be added
        // but for now we only ever consider the first (index 0) effect, 
        // which will be set in the transation type
        private static readonly string[] TransitionEffects = new[] { "Fade" };
        private readonly int TransitionEffectIndex = 0;
        private readonly string TransitionType;

        // The top level photo folder, and a list of all image paths in that folder
        private readonly IEnumerable<string> ImagePathsList;

        // A first-in list of previously seen photos
        private readonly List<string> HistoryList = new();
        private readonly int MaxHistoryItems = 50;
        private int HistoryIndex = -1;

        // Manages which photo is the previous and current photo
        private readonly Image[] ImageControls;
        private int CurrentCtrlIndex;

        // Used to generate and select a random image from ImagePathsList 
        private readonly Random Random = new();

        // Duration left of when to resume the slide show after a pause
        private static int resumePauseDuration = 60;
        private static int pauseCountDown = 60;

        public MainWindow(Settings settings, bool isPreviewMode)
        {
            InitializeComponent();
            // This is a hack, as I can't figure out how to convert the size of the preview window into WPF coordinates
            //double pixelsToWpfRatio = .5;
            this.Settings = settings;
            this.IsPreviewMode = isPreviewMode;
            // So we can access  this window from other objects
            MainWindow.MainWindowAccess = this;

            // Set the transition to the only single 'Fade' transition
            this.TransitionType = TransitionEffects[this.TransitionEffectIndex];

            // This allows us to flip between the two images
            this.ImageControls = new[] { Image1, Image2 };

            // Load all images into the ImagePaths List
            this.ImagePathsList = this.LoadImageFolder(this.Settings.PhotoFolder);


            // Set up the slide show timers
            this.TimerChangeSlide = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, this.Settings.Timing)
            };
            this.TimerChangeSlide.Tick += TimerChangeSlide_Tick;

            this.TimerResumeAfterPause = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 2)
            };
            this.TimerResumeAfterPause.Tick += TimerResumeAfterPause_Tick;

            if (this.IsPreviewMode)
            {
                // We are in a preview mode.
                // Note that the Loaded callback is not automatically invoked in Preview mode
                // A screen saver preview is drawn in the small rectangle in the windows screen saver settings dialog
                // Autosizing of images seems to work as long as its in a grid. 
                // However, we should scale the font size to fit, and remove any margins from that text
                this.DisplayText.FontSize = 8.0;
                this.DisplayText.StrokeThickness = .2;
                this.DisplayText.Margin = new Thickness(0);
                this.InitializeSlideShow();
            }
            else
            {
                // Set the outline font to the stored settings.
                this.DisplayText.FontSize = this.Settings.DisplayFontSize;
                this.DisplayText.StrokeThickness = this.DisplayText.FontSize / 26.0;
                // The slide show will be started via the Loaded callback
            }

            if (!this.ImagePathsList.Any())
            {
                this.DisplayText.Text = "No jpeg images found in: " + this.Settings.PhotoFolderKey;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded is NOT explicitly invoked in preview mode
            this.InitializeSlideShow();
        }

        private void InitializeSlideShow()
        {
            // If there are no folders, then don't start the timer
            if (!this.ImagePathsList.Any())
            {
                this.DisplayText.Text = "No jpeg images found in: " + this.Settings.PhotoFolderKey;
                this.Stop();
            }
            else
            {
                this.Start();
            }
        }

        private IEnumerable<string> LoadImageFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                DisplayText.Text = "The specified folder does not exist: " + path;
                return new List<string>();
            }

            // Get all the image paths as a list from the specified folder and its subfolders
            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || file.EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase));
        }

        #region Slide show playing: Start/Stop/IsPlaying/TogglePlay
        private bool IsPlaying()
        {
            return this.TimerChangeSlide?.IsEnabled == true;
        }

        private void Start()
        {
            pauseCountDown = resumePauseDuration;
            TimerResumeAfterPause.Stop();
            this.TimerChangeSlide.Start();
            this.ShowSlide();
        }

        private void Stop()
        {
            pauseCountDown = resumePauseDuration;
            if (this.IsPlaying())
            {
                 this.TimerChangeSlide.Stop();
                // reset the resume pause duration, and start again.
                TimerResumeAfterPause.Start();
            }
        }

        private void TogglePlay()
        {
            pauseCountDown = resumePauseDuration;
            if (this.IsPlaying())
            {
                this.Stop();
            }
            else
            {
                this.Start();
            }
        }

        #region Navigate and ShowSlide
        #endregion

        private void Navigate(DirectionEnum direction)
        {
            this.Stop();
            this.ShowSlide(true, direction);
        }

        public void ShowSlide()
        {
            ShowSlide(false, DirectionEnum.None);
        }

        public void ShowSlide(bool fromHistoryList, DirectionEnum direction)
        {
            if (!this.ImagePathsList.Any())
            {
                // No slides to show
                this.DisplayText.Text = "No jpeg images found in: " + this.Settings.PhotoFolderKey;
                this.Stop();
                return;
            }

            try
            {
                // Switch the index so that:
                int oldCtrlIndex = this.CurrentCtrlIndex;
                int historyListCount = this.HistoryList.Count;

                // Swap the images
                this.CurrentCtrlIndex = (this.CurrentCtrlIndex + 1) % 2;
                Image imgOld = this.ImageControls[oldCtrlIndex];
                Image imgNew = this.ImageControls[this.CurrentCtrlIndex];

                ImageSource newSource;

                if (fromHistoryList && direction == DirectionEnum.Next)
                {
                    if (this.HistoryIndex + 1 >= this.HistoryList.Count)
                    {
                        // Because we are trying to go past the history list,
                        // we generate a new slide instead
                        fromHistoryList = false;
                    }
                    else
                    {
                        this.HistoryIndex++;
                    }
                }
                else if (fromHistoryList && direction == DirectionEnum.Previous)
                {
                    if (this.HistoryIndex > 0)
                    {
                        this.HistoryIndex--;
                    }
                }

                if (fromHistoryList && this.HistoryIndex < historyListCount)
                {
                    if (this.HistoryIndex < 0 || this.HistoryIndex >= historyListCount)
                    {
                        // This shouldn't happen, but just in case
                        return;
                    }
                    // Get the next image path to display from the history list and assign it to the new image
                    this.CurrentSlidePath = this.HistoryList[this.HistoryIndex];
                    newSource = CreateImageSource(this.CurrentSlidePath, true);
                    imgNew.Source = newSource;
                }
                else
                {
                    // Get a random image path to display
                    this.CurrentSlidePath = this.ImagePathsList.ElementAt(Random.Next(0, this.ImagePathsList.Count()));
                    newSource = CreateImageSource(this.CurrentSlidePath, true);

                    // Add the path to the history list
                    this.HistoryList.Add(this.CurrentSlidePath);
                    if (historyListCount == this.MaxHistoryItems)
                    {
                        this.HistoryList.RemoveAt(0);
                        this.HistoryIndex = this.MaxHistoryItems - 1;
                    }
                    else
                    {
                        this.HistoryIndex++;
                    }
                }

                // Display the image name
                this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, this.IsPlaying());

                //  Set the image source to the new image
                imgNew.Source = newSource;

                // Get the Storyboard animations and apply it
                // Out is a transition applied to the old image
                // In is a transition applied to the new image
                Storyboard? StboardFadeOut = (Resources[$"{TransitionType}Out"] as Storyboard)?.Clone();
                Storyboard? StboardFadeIn = Resources[$"{TransitionType}In"] as Storyboard;
                StboardFadeOut?.Begin(imgOld);
                StboardFadeIn?.Begin(imgNew);
            }
            catch (Exception ex)
            {
                this.DisplayText.Text = ex.Message;
            }
        }
        #endregion

        #region UI Callbacks
        private void TimerChangeSlide_Tick(object? sender, EventArgs e)
        {
            this.ShowSlide();
            this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, this.IsPlaying());
        }

        private void TimerResumeAfterPause_Tick(object? sender, EventArgs e)
        {
            if (pauseCountDown > 0)
            {
                pauseCountDown -= 2;
            }
            else
            {
                this.Start();
            }
            this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, this.IsPlaying());
        }

        // Exit the screen saver on any mouse down press
        public void Window_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (this.IsPreviewMode)
            {
                // The preview window in settings should not react to input done over it
                return;
            }
            Application.Current.Shutdown();
        }

        // React to a key press as specified below
        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.IsPreviewMode)
            {
                // The preview window in settings should not react to input done over it
                return;
            }
            switch (e.Key)
            {
                case Key.Space:
                    // Pause or resume the slide show
                    this.TogglePlay();
                    break;
                case Key.Left:
                    // Go to the previous slide
                    this.Navigate(DirectionEnum.Previous);
                    break;
                case Key.Right:
                    // Go to the next slide
                    this.Navigate(DirectionEnum.Next);
                    break;
                default:
                    // Exit the screen saver on any other key press
                    Application.Current.Shutdown();
                    break;
            }
            // Redisplay the image name to show the toggled pause state
            this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, this.IsPlaying());
        }
        #endregion

        #region Utilities
        // Given a file, return an image source 
        // Note that this assumes the file exists and is a valid image.
        // We could put in some error checking, e.g. by checking the file existence, and by
        // catching the DownloadFailed event. See https://www.codeproject.com/Questions/785061/How-do-I-determine-an-Bitmapimage-loaded-successfu for how to do this.
        private static ImageSource CreateImageSource(string file, bool forcePreLoad)
        {
            if (forcePreLoad)
            {
                var src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(file, UriKind.Absolute);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                src.Freeze();
                return src;
            }
            else
            {
                var src = new BitmapImage(new Uri(file, UriKind.Absolute));
                src.Freeze();
                return src;
            }
        }


        private static string GetPauseString()
        {
            return $" - paused {new string('.', pauseCountDown <= 0 ? 1 : pauseCountDown / 2) }";
        }

        // Return a string representing the file name,
        // formatted according to the current user settings.
        private static string DisplayTextBasedOnSettings(Settings settings, string path, bool isPlaying)
        {
            string paused = isPlaying ? string.Empty : GetPauseString();

            if (settings.ShowFileName == false)
            {
                return string.Empty + paused;
            }

            if (settings.DisplayByFileName)
            {
                return Path.GetFileName(path) + paused;
            }

            if (settings.DisplayByFolderName)
            {
                string? foldername = Path.GetFileName(Path.GetDirectoryName(path));
                string filename = Path.GetFileName(path);

                return foldername != null
                    ? foldername + paused
                    : filename + paused;
            }

            if (settings.DisplayByFolderFileName)
            {
                string? foldername = Path.GetFileName(Path.GetDirectoryName(path));
                string filename = Path.GetFileName(path);

                return foldername == null
                    ? filename + paused
                    : Path.Combine(foldername, filename) + paused;
            }
            // Only other possibilitys is settings.DisplayByPath 
            return path + paused;
        }
        #endregion
    }
}
