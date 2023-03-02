using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;

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
        private readonly DispatcherTimer Timer;

        // Transition effects
        // This eventually allows for more transition effects to be added
        // but for now we only ever consider the first (index 0) effect, 
        // which will be set in the transation type
        private static readonly string[] TransitionEffects = new[] { "Fade" };
        private readonly int TransitionEffectIndex = 0;
        private readonly string TransitionType;

        // The top level photo folder, and a list of all image paths in that folder
        private readonly List<string> ImagePathsList;

        // A first-in list of previously seen photos
        private readonly List<string> HistoryList = new List<string>();
        private readonly int MaxHistoryItems = 50;
        private int HistoryIndex = -1;

        // Manages which photo is the previous and current photo
        private readonly Image[] ImageControls;
        private int CurrentCtrlIndex;

        // Used to generate and select a random image from ImagePathsList 
        private readonly Random Random = new Random();

        public MainWindow(Settings settings, bool previewing, Point windowSize)
        {
            InitializeComponent();
            // This is a hack, as I can't figure out how to convert the size of the preview window into WPF coordinates
            double pixelsToWpfRatio = .5;
            this.Settings = settings;

            // So we can access  this window from other objects
            MainWindow.MainWindowAccess = this;

            // Set the transition to the only single 'Fade' transition
            this.TransitionType = TransitionEffects[this.TransitionEffectIndex];

            // This allows us to flip between the two images
            this.ImageControls = new[] { Image1, Image2 };

            // Load all images into the ImagePaths List
            this.ImagePathsList = this.LoadImageFolder(this.Settings.PhotoFolder);
            if (this.ImagePathsList.Count == 0)
            {
                this.DisplayText.Text = "No jpeg images found in: " + this.Settings.PhotoFolderKey;
                return;
            }

            // Set up the slide show timer
            this.Timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, this.Settings.Timing)
            };
            this.Timer.Tick += TimerTick;

            if (previewing)
            {
                // We are in a preview mode.
                // A screen saver preview is drawn in the small rectangle in the windows screen saver settings dialog
                // Consequently, we need to adjust the canvas size and the position / font size of the display text to better fit the preview window
                // Note that this is a bit problematic, as so far I can't figure out how to get the size of that rectangle in WPF coordinates.
                // Instead, I just multiply it by a fixed ratio, which sort of works but is not quite sized correctly as that depends on the screen dpi.
                Point dimensions = new Point(windowSize.X * pixelsToWpfRatio, windowSize.X * pixelsToWpfRatio);
                this.RootCanvas.Width = dimensions.X;
                this.RootCanvas.Height = dimensions.Y;

                this.DisplayText.FontSize = 8.0;
                this.DisplayText.StrokeThickness = 0;

                Canvas.SetTop(this.DisplayText, 0);
                Canvas.SetLeft(this.DisplayText, 0);
                this.Start();
            }
            else
            {
                // Set the outline font to the stored settings.
                this.DisplayText.FontSize = this.Settings.DisplayFontSize;
                this.DisplayText.StrokeThickness = this.DisplayText.FontSize/26.0;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // This is NOT invoked in preview mode

            // Top left corner
            this.Left = App.PrimaryScreen.WorkingArea.Left;
            this.Top = App.PrimaryScreen.WorkingArea.Top;

            // Fit the window and canvas to the screen
            Point dimensions = App.RealPixelsToWpf(this, new Point(App.PrimaryScreen.WorkingArea.Width, App.PrimaryScreen.WorkingArea.Height));
            this.Width = dimensions.X;
            this.Height = dimensions.Y;
            this.RootCanvas.Width = dimensions.X;
            this.RootCanvas.Height = dimensions.Y;
            Canvas.SetTop(this.DisplayText, dimensions.Y - this.DisplayText.ActualHeight - 20);

            // If there are no folders, then don't start the timer
            if (this.ImagePathsList.Count == 0)
            {
                this.Stop();
            }
            else
            {
                this.Start();
            }
        }

        private List<string> LoadImageFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                DisplayText.Text = "The specified folder does not exist: " + path;
                return new List<string>();
            }

            // Get all the image paths as a list from the specified folder and its subfolders
            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || file.EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        #region Slide sow playing: Start/Stop/IsPlaying/TogglePlay
        private bool IsPlaying()
        {
            return this.Timer?.IsEnabled == true;
        }
        
        private void Start()
        {
            this.ShowSlide();
            this.Timer.Start();
        }

        private void Stop()
        {
            if (this.IsPlaying())
            {
                this.Timer.Stop();
            }
        }

        private void TogglePlay()
        {
            if (this.IsPlaying())
            {
                this.Timer.Stop();
            }
            else
            {
                this.Timer.Start();
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
                string newPath;

                if (fromHistoryList && direction == DirectionEnum.Next)
                {
                    if (this.HistoryIndex + 1 >= this.HistoryList.Count)
                    {
                        // Because we are trying to go past the history list,
                        // we generate a new slide instead.
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
               
                if (fromHistoryList && this.HistoryIndex < historyListCount ) 
                {
                    if (this.HistoryIndex < 0 || this.HistoryIndex >= historyListCount )
                    {
                        // This shouldn't happen, but just in case
                        return;
                    }
                    // Get the next image path to display from the history list and assign it to the new image
                    newPath = this.HistoryList[this.HistoryIndex];
                    newSource = CreateImageSource(newPath, true);
                    imgNew.Source = newSource;
                }
                else
                {
                    // Get a random image path to display
                    newPath = this.ImagePathsList[Random.Next(0, this.ImagePathsList.Count)];
                    newSource = CreateImageSource(newPath, false);

                    // Add the path to the history list
                    this.HistoryList.Add(newPath);
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
                this.DisplayText.Text = DisplayTextBySettings(this.Settings, newPath);
                //this.DisplayTextOutlined.Text = DisplayTextBySettings(this.Settings, newPath);
                //  Set the image source to the new image
                imgNew.Source = newSource;

                // Get the Storyboard animations and apply it
                // Out is a transition applied to the old image
                // In is a transition applied to the new image
                Storyboard StboardFadeOut = (Resources[$"{TransitionType}Out"] as Storyboard)?.Clone();
                Storyboard StboardFadeIn = Resources[$"{TransitionType}In"] as Storyboard;
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
        private void TimerTick(object sender, EventArgs e)
        {
            this.ShowSlide();
        }

        // Exit the screen saver on any mouse down press
        public void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // React to 
        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
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

        // Return a string representing the file name,
        // formatted according to the current user settings.
        private static string DisplayTextBySettings(Settings settings, string path)
        {
            if (settings.ShowFileName == false)
            {
                return string.Empty;
            }

            if (settings.DisplayByFileName)
            {
                return Path.GetFileName(path);
            }

            else if (settings.DisplayByFolderFileName)
            {
                string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                string lastFolder = fullPath.Split(Path.DirectorySeparatorChar).Last();

                return Path.Combine(lastFolder, Path.GetFileName(path));
            }
            // must be settings.DisplayByPath 
            return path;
        }
        #endregion
    }
}
