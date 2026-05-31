using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public partial class MainWindow
    {
        public static MainWindow? MainWindowAccess;

        public readonly Settings Settings;

        private readonly DispatcherTimer TimerChangeSlide;
        private readonly DispatcherTimer TimerResumeAfterPause;

        private readonly bool IsPreviewMode;

        private string? CurrentSlidePath;

        private static readonly string[] TransitionEffects = new[] { "Fade" };
        private readonly int TransitionEffectIndex = 0;
        private readonly string TransitionType;

        private List<string> MediaPathsList = new();

        private readonly List<string> HistoryList = new();
        private readonly int MaxHistoryItems = 50;
        private int HistoryIndex = -1;

        private readonly Image[] ImageControls;
        private int CurrentCtrlIndex;

        private readonly Random Random = new();

        private static int resumePauseDuration = 60;
        private static int pauseCountDown = 60;

        // Video state
        private bool _videoIsActive;
        private bool _isClosing;
        private bool _isPaused;
        private DispatcherTimer? _clipTimer;

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".mov", ".avi", ".wmv", ".m4v", ".webm"
        };

        private static bool IsVideoFile(string path) =>
            VideoExtensions.Contains(Path.GetExtension(path));

        public MainWindow(Settings settings, bool isPreviewMode)
        {
            InitializeComponent();
            this.Settings = settings;
            this.IsPreviewMode = isPreviewMode;
            MainWindow.MainWindowAccess = this;

            this.TransitionType = TransitionEffects[this.TransitionEffectIndex];
            this.ImageControls = new[] { Image1, Image2 };

            // Wire up the video player
            VideoPlayer.ControlsVisible         = false;   // screensaver defaults: AutoPlay=true, AutoRepeat=false
            VideoPlayer.FadeEffectBetweenVideos = true;
            VideoPlayer.IsMuted                 = true;    // muted during autoplay; unmuted only when paused
            VideoPlayer.MediaEnded             += VideoPlayer_MediaEnded;
            VideoPlayer.MediaFailed            += VideoPlayer_MediaFailed;
            VideoPlayer.PlayPauseClicked       += VideoPlayer_PlayPauseClicked;

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
                // Preview pane: small image-only set, load synchronously before showing.
                this.MediaPathsList = LoadMediaFolder(this.Settings.PhotoFolder)
                    .Where(p => !IsVideoFile(p)).ToList();
                this.DisplayText.FontSize = 8.0;
                this.DisplayText.StrokeThickness = .2;
                this.DisplayText.Margin = new Thickness(0);
                this.InitializeSlideShow();
            }
            else
            {
                this.DisplayText.FontSize = this.Settings.DisplayFontSize;
                this.DisplayText.StrokeThickness = this.DisplayText.FontSize / 26.0;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsPreviewMode) return;  // constructor already loaded and initialized

            string path = this.Settings.PhotoFolder;
            bool includeVideos = this.Settings.IncludeVideos;

            if (!Directory.Exists(path))
            {
                this.DisplayText.Text = "The specified folder does not exist: " + path;
                return;
            }

            this.DisplayText.Text = "Loading media…";

            this.MediaPathsList = await Task.Run(() =>
                Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith("jpg",  StringComparison.InvariantCultureIgnoreCase) ||
                                f.EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                                IsVideoFile(f))
                    .Where(f => includeVideos || !IsVideoFile(f))
                    .ToList()
            );

            this.InitializeSlideShow();
        }

        private void InitializeSlideShow()
        {
            if (!this.MediaPathsList.Any())
            {
                this.DisplayText.Text = "No images or videos found in: " + this.Settings.PhotoFolderKey;
                this.Stop();
            }
            else
            {
                this.Start();
            }
        }

        private IEnumerable<string> LoadMediaFolder(string path)
        {
            if (!Directory.Exists(path))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                    file.EndsWith("jpg",  StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                    IsVideoFile(file));
        }

        #region Slide show playing: Start / Stop / TogglePlay / Navigate

        private bool IsPlaying() => this.TimerChangeSlide?.IsEnabled == true;

        // Show video controls and disable the input-blocking overlay.
        // Called whenever _isPaused && _videoIsActive is true.
        private void ShowVideoControls()
        {
            VideoPlayer.ControlsFadeOutDuration = TimeSpan.FromHours(1);  // prevent auto-hide
            InputBlockingOverlay.Visibility = Visibility.Collapsed;
            DisplayTextLabel.IsHitTestVisible = false;
            VideoPlayer.ControlsVisible = true;
        }

        // Hide video controls and restore the input-blocking overlay.
        // Called when returning to normal (playing) video state.
        private void HideVideoControls()
        {
            VideoPlayer.ControlsFadeOutDuration = TimeSpan.FromSeconds(4);
            VideoPlayer.ControlsVisible = false;
            InputBlockingOverlay.Visibility = Visibility.Visible;
            DisplayTextLabel.IsHitTestVisible = true;
        }

        // Resume from a Space-bar pause.
        // Video: resumes the current video and restarts the clip timer.
        // Image: advances immediately to the next slide.
        private void Start()
        {
            _isPaused = false;
            pauseCountDown = resumePauseDuration;
            TimerResumeAfterPause.Stop();
            VideoPlayer.IsMuted = true;
            if (_videoIsActive)
            {
                HideVideoControls();
                _ = VideoPlayer.Play();
                StartClipTimer();
            }
            else
            {
                TimerChangeSlide.Start();
                ShowSlide();
            }
        }

        // Pause the slideshow (Space bar pressed).
        // Pauses the current video and shows its controls if a video is active.
        private void Stop()
        {
            _isPaused = true;
            pauseCountDown = resumePauseDuration;
            TimerChangeSlide.Stop();
            TimerResumeAfterPause.Start();
            StopClipTimer();
            VideoPlayer.IsMuted = false;
            if (_videoIsActive)
            {
                _ = VideoPlayer.Pause();
                ShowVideoControls();
            }
        }

        private void TogglePlay()
        {
            if (_isPaused)
                Start();
            else
                Stop();
        }

        private void Navigate(DirectionEnum direction)
        {
            bool wasPlaying = IsPlaying();
            TimerChangeSlide.Stop();
            TimerResumeAfterPause.Stop();
            pauseCountDown = resumePauseDuration;
            ShowSlide(true, direction);
            if (wasPlaying)
                TimerChangeSlide.Start();
            // Restart the auto-resume countdown after each navigation while paused.
            if (_isPaused)
                TimerResumeAfterPause.Start();
        }

        public void ShowSlide() => ShowSlide(false, DirectionEnum.None);

        public void ShowSlide(bool fromHistoryList, DirectionEnum direction)
        {
            StopClipTimer();

            if (!this.MediaPathsList.Any())
            {
                this.DisplayText.Text = "No images or videos found in: " + this.Settings.PhotoFolderKey;
                this.Stop();
                return;
            }

            try
            {
                int oldCtrlIndex = this.CurrentCtrlIndex;
                int historyListCount = this.HistoryList.Count;

                this.CurrentCtrlIndex = (this.CurrentCtrlIndex + 1) % 2;
                Image imgOld = this.ImageControls[oldCtrlIndex];
                Image imgNew = this.ImageControls[this.CurrentCtrlIndex];

                // Navigate history to determine CurrentSlidePath
                if (fromHistoryList && direction == DirectionEnum.Next)
                {
                    if (this.HistoryIndex + 1 >= this.HistoryList.Count)
                        fromHistoryList = false;
                    else
                        this.HistoryIndex++;
                }
                else if (fromHistoryList && direction == DirectionEnum.Previous)
                {
                    if (this.HistoryIndex > 0)
                        this.HistoryIndex--;
                }

                if (fromHistoryList && this.HistoryIndex < historyListCount)
                {
                    if (this.HistoryIndex < 0 || this.HistoryIndex >= historyListCount)
                        return;
                    this.CurrentSlidePath = this.HistoryList[this.HistoryIndex];
                }
                else
                {
                    this.CurrentSlidePath = this.MediaPathsList[Random.Next(0, this.MediaPathsList.Count)];
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

                if (IsVideoFile(this.CurrentSlidePath))
                {
                    this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, !_isPaused);
                    ShowVideoSlide(this.CurrentSlidePath, imgOld);
                }
                else
                {
                    ImageSource newSource = CreateImageSource(this.CurrentSlidePath, true);
                    imgNew.Source = newSource;

                    if (_videoIsActive)
                    {
                        // Video → Image: reset all video control state, then cross-dissolve.
                        _videoIsActive = false;
                        VideoPlayer.ControlsFadeOutDuration = TimeSpan.FromSeconds(4);
                        VideoPlayer.ControlsVisible = false;
                        DisplayTextLabel.IsHitTestVisible = true;
                        imgNew.Opacity = 0;
                        imgNew.Visibility = Visibility.Visible;

                        this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, !_isPaused);

                        var duration = new Duration(TimeSpan.FromSeconds(0.75));
                        var fadeIn  = new DoubleAnimation(0, 1, duration);
                        var fadeOut = new DoubleAnimation(1, 0, duration);

                        fadeOut.Completed += (s, e) =>
                        {
                            if (_videoIsActive) return;
                            VideoPlayer.BeginAnimation(UIElement.OpacityProperty, null);
                            VideoPlayer.Opacity = 1;
                            VideoPlayer.Visibility = Visibility.Collapsed;
                            InputBlockingOverlay.Visibility = Visibility.Collapsed;
                            _ = VideoPlayer.Stop();
                            if (!_isPaused)
                                TimerChangeSlide.Start();
                        };

                        imgNew.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                        VideoPlayer.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    }
                    else
                    {
                        // Image → Image: storyboard cross-dissolve.
                        // imgNew may be Collapsed if a prior image→video transition collapsed both
                        // images; make it Visible so the FadeIn storyboard is actually rendered.
                        this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, !_isPaused);
                        imgNew.Visibility = Visibility.Visible;

                        Storyboard? StboardFadeOut = (Resources[$"{TransitionType}Out"] as Storyboard)?.Clone();
                        Storyboard? StboardFadeIn  = Resources[$"{TransitionType}In"]  as Storyboard;
                        StboardFadeOut?.Begin(imgOld);
                        StboardFadeIn?.Begin(imgNew);
                    }
                }
            }
            catch (Exception ex)
            {
                this.DisplayText.Text = ex.Message;
            }
        }

        private void StartClipTimer()
        {
            StopClipTimer();
            if (!Settings.ClipVideos || _isPaused) return;
            _clipTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Settings.ClipDuration) };
            _clipTimer.Tick += (s, e) => { StopClipTimer(); ShowSlide(); };
            _clipTimer.Start();
        }

        private void StopClipTimer()
        {
            _clipTimer?.Stop();
            _clipTimer = null;
        }

        // imgOld is the currently displayed image control, passed so it can be cross-dissolved out.
        private void ShowVideoSlide(string path, Image imgOld)
        {
            TimerChangeSlide.Stop();
            TimerResumeAfterPause.Stop();

            bool wasShowingVideo = _videoIsActive;
            _videoIsActive = true;

            if (!wasShowingVideo)
            {
                // Image → Video: render at opacity 0, start the cross-dissolve only after the first
                // frame is decoded (MediaOpened) so the image never fades into a blank player.
                VideoPlayer.Opacity = 0;
                VideoPlayer.Visibility = Visibility.Visible;
                InputBlockingOverlay.Visibility = Visibility.Visible;

                string capturedPath = path;
                EventHandler<Unosquare.FFME.Common.MediaOpenedEventArgs>? onOpened = null;
                onOpened = (s, e) =>
                {
                    VideoPlayer.MediaOpened -= onOpened;
                    if (!_videoIsActive || VideoPlayer.Source != capturedPath) return;

                    var dur = new Duration(TimeSpan.FromSeconds(0.75));
                    var fadeIn  = new DoubleAnimation(0, 1, dur);
                    var fadeOut = new DoubleAnimation(1, 0, dur);

                    fadeOut.Completed += (_, _) =>
                    {
                        if (!_videoIsActive) return;
                        Image1.BeginAnimation(UIElement.OpacityProperty, null);
                        Image2.BeginAnimation(UIElement.OpacityProperty, null);
                        Image1.Visibility = Visibility.Collapsed;
                        Image2.Visibility = Visibility.Collapsed;
                        Image1.Opacity = 1;
                        Image2.Opacity = 1;
                        // Transition complete: show controls if paused, else start clip timer.
                        if (_isPaused)
                            ShowVideoControls();
                        else
                            StartClipTimer();
                    };

                    imgOld.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    VideoPlayer.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };
                VideoPlayer.MediaOpened += onOpened;
                _ = VideoPlayer.Open(path);
            }
            else if (VideoPlayer.UseDualBuffer && !_isPaused)
            {
                // Video → Video, playing: seamless dual-buffer swap with internal cross-dissolve.
                _ = VideoPlayer.OpenDualBuffer(path);
                StartClipTimer();
            }
            else
            {
                // Video → Video, paused (or no dual buffer): regular open.
                // Hide controls during load, then restore after MediaOpened.
                if (_isPaused) HideVideoControls();

                string capturedPath = path;
                EventHandler<Unosquare.FFME.Common.MediaOpenedEventArgs>? onOpened = null;
                onOpened = (s, e) =>
                {
                    VideoPlayer.MediaOpened -= onOpened;
                    if (!_videoIsActive || VideoPlayer.Source != capturedPath) return;
                    if (_isPaused)
                        ShowVideoControls();
                    else
                        StartClipTimer();
                };
                VideoPlayer.MediaOpened += onOpened;
                _ = VideoPlayer.Open(path);
            }
        }

        #endregion

        #region Video player event handlers

        private void VideoPlayer_MediaEnded(object? sender, EventArgs e)
        {
            StopClipTimer();
            if (_isPaused) return;  // req 1g: never auto-transition while paused
            ShowSlide();
        }

        private void VideoPlayer_MediaFailed(object? sender, Unosquare.FFME.Common.MediaFailedEventArgs e)
        {
            ShowSlide();  // always skip past a broken video
        }

        private async void VideoPlayer_PlayPauseClicked(object? sender, EventArgs e)
        {
            if (VideoPlayer.IsPlaying)
                await VideoPlayer.Pause();
            else
                await VideoPlayer.Play();
        }

        #endregion

        #region UI Callbacks

        private void TimerChangeSlide_Tick(object? sender, EventArgs e)
        {
            this.ShowSlide();
        }

        private void TimerResumeAfterPause_Tick(object? sender, EventArgs e)
        {
            if (pauseCountDown > 0)
            {
                pauseCountDown -= 2;
            }
            else
            {
                // Countdown expired: always advance to the next slide regardless of what is showing.
                _isPaused = false;
                TimerResumeAfterPause.Stop();
                VideoPlayer.IsMuted = true;
                if (_videoIsActive)
                    HideVideoControls();
                TimerChangeSlide.Start();
                ShowSlide();
                return;
            }
            this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, !_isPaused);
        }

        public void Window_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (this.IsPreviewMode) return;
            if (_isPaused && _videoIsActive) return;  // clicks reach video controls, not screensaver exit
            Application.Current.Shutdown();
        }

        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.IsPreviewMode) return;

            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
                return;
            }

            switch (e.Key)
            {
                case Key.Space:
                    TogglePlay();
                    break;
                case Key.Left:
                    Navigate(DirectionEnum.Previous);
                    break;
                case Key.Right:
                    Navigate(DirectionEnum.Next);
                    break;
                default:
                    Application.Current.Shutdown();
                    return;
            }
            e.Handled = true;  // prevent children (e.g. focused buttons, library frame-stepper) from also acting
            this.DisplayText.Text = DisplayTextBasedOnSettings(this.Settings, this.CurrentSlidePath, !_isPaused);
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_isClosing) return;
            e.Cancel = true;
            _isClosing = true;
            try { await VideoPlayer.Close(); } catch { }
            Application.Current.Shutdown();
        }

        #endregion

        #region Utilities

        private static ImageSource CreateImageSource(string file, bool forcePreLoad)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            BitmapSource result = ApplyExifOrientation(frame);
            result.Freeze();
            return result;
        }

        private static BitmapSource ApplyExifOrientation(BitmapFrame frame)
        {
            const string orientationQuery = "/app1/ifd/{ushort=274}";
            if (frame.Metadata is not BitmapMetadata metadata ||
                !metadata.ContainsQuery(orientationQuery) ||
                metadata.GetQuery(orientationQuery) is not ushort orientation ||
                orientation <= 1)
            {
                return frame;
            }

            return orientation switch
            {
                2 => new TransformedBitmap(frame, new ScaleTransform(-1, 1)),
                3 => new TransformedBitmap(frame, new RotateTransform(180)),
                4 => new TransformedBitmap(frame, new ScaleTransform(1, -1)),
                5 => ChainTransforms(frame, new RotateTransform(90), new ScaleTransform(-1, 1)),
                6 => new TransformedBitmap(frame, new RotateTransform(90)),
                7 => ChainTransforms(frame, new RotateTransform(270), new ScaleTransform(-1, 1)),
                8 => new TransformedBitmap(frame, new RotateTransform(270)),
                _ => frame
            };
        }

        private static TransformedBitmap ChainTransforms(BitmapSource src, Transform first, Transform second)
        {
            var step1 = new TransformedBitmap(src, first);
            step1.Freeze();
            return new TransformedBitmap(step1, second);
        }

        private static string GetPauseString()
        {
            return $" - paused {new string('.', pauseCountDown <= 0 ? 1 : pauseCountDown / 2)}";
        }

        private static string DisplayTextBasedOnSettings(Settings settings, string? path, bool isPlaying)
        {
            if (path == null) return string.Empty;

            string paused = isPlaying ? string.Empty : GetPauseString();

            if (settings.ShowFileName == false)
                return string.Empty + paused;

            if (settings.DisplayByFileName)
                return Path.GetFileName(path) + paused;

            if (settings.DisplayByFolderName)
            {
                string? foldername = Path.GetFileName(Path.GetDirectoryName(path));
                string filename = Path.GetFileName(path);
                return foldername != null ? foldername + paused : filename + paused;
            }

            if (settings.DisplayByFolderFileName)
            {
                string? foldername = Path.GetFileName(Path.GetDirectoryName(path));
                string filename = Path.GetFileName(path);
                return foldername == null
                    ? filename + paused
                    : Path.Combine(foldername, filename) + paused;
            }

            return path + paused;
        }

        #endregion
    }
}
