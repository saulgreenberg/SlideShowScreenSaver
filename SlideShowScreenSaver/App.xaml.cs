using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace SlideShowScreenSaver
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		private HwndSource winWPFContent;
        public static Screen PrimaryScreen;

		private void ApplicationStartup(object sender, StartupEventArgs e)
		{
            Settings settings = new Settings();
			if (e.Args.Length == 0 || e.Args[0].ToLower().StartsWith("/s"))
			{
				foreach (Screen s in Screen.AllScreens)
				{
					if (s.Equals(Screen.PrimaryScreen) == false)
					{
						Blackout window = new Blackout
                        {
                            Left = s.WorkingArea.Left,
                            Top = s.WorkingArea.Top,
                            Width = s.WorkingArea.Width,
                            Height = s.WorkingArea.Height
                        };
                        window.Show();
					}
					else
					{
						MainWindow window = new MainWindow(settings, false, new Point(0,0));
                        App.PrimaryScreen = s;
                        window.Show();
                    }
				}
			}
			else if (e.Args[0].ToLower().StartsWith("/p"))
			{
                
                Int32 previewHandle = Convert.ToInt32(e.Args[1]);
                IntPtr pPreviewHnd = new IntPtr(previewHandle);
                RECT lpRect = new RECT();
                // ReSharper disable once UnusedVariable
                bool bGetRect = Win32API.GetClientRect(pPreviewHnd, ref lpRect);

                HwndSourceParameters sourceParams = new HwndSourceParameters("sourceParams")
                {
                    PositionX = 0,
                    PositionY = 0,
                    Height = lpRect.Bottom - lpRect.Top,
                    Width = lpRect.Right - lpRect.Left,
                    ParentWindow = pPreviewHnd,
                    WindowStyle = (int)(WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | WindowStyles.WS_CLIPCHILDREN)
                };

                winWPFContent = new HwndSource(sourceParams);
                MainWindow window = new MainWindow(settings, true, new Point(sourceParams.Width, sourceParams.Height));


                winWPFContent.Disposed += (o, args) => window.Close();
                winWPFContent.RootVisual = window.RootCanvas;
            } 
            else if (e.Args[0].ToLower().StartsWith("/c"))
            {
				SettingsDialog dialog = new SettingsDialog(settings);
				dialog.ShowDialog(); // We don't bother with a result, as settings are changed dynamically.
            }
        }

        public static Point RealPixelsToWpf(Window w, Point p)
        {
            PresentationSource ps = PresentationSource.FromVisual(w);
            return (ps?.CompositionTarget != null)
            ? ps.CompositionTarget.TransformFromDevice.Transform(p)
            : new Point(3840, 2088);
        }
    }
}
