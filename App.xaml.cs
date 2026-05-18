using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.InteropServices;

namespace WinMensa
{
    public partial class App : Application
    {
        private Window window = Window.Current;
        private SUBCLASSPROC? _subclassProc;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();
            window.ExtendsContentIntoTitleBar = true;
            window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
            window.AppWindow.Title = "WinMensa";

            if (MicaController.IsSupported())
                window.SystemBackdrop = new MicaBackdrop();

            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
                presenter.Maximize();

            var titleBar = new Grid();
            titleBar.Children.Add(new TextBlock
            {
                Text = "WinMensa",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0),
                FontSize = 12,
            });

            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(titleBar, 0);
            Grid.SetRow(rootFrame, 1);
            rootGrid.Children.Add(titleBar);
            rootGrid.Children.Add(rootFrame);

            window.Content = rootGrid;
            window.SetTitleBar(titleBar);

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            window.Activate();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            _subclassProc = WindowSubclassProc;
            SetWindowSubclass(hwnd, _subclassProc, 0, 0);
        }

        private static IntPtr WindowSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, nuint dwRefData)
        {
            if (uMsg == 0x0024) // WM_GETMINMAXINFO
            {
                uint dpi = GetDpiForWindow(hWnd);
                int minW = (int)(900 * dpi / 96.0);
                int minH = (int)(500 * dpi / 96.0);
                var info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                info.ptMinTrackSize.x = Math.Max(info.ptMinTrackSize.x, minW);
                info.ptMinTrackSize.y = Math.Max(info.ptMinTrackSize.y, minH);
                Marshal.StructureToPtr(info, lParam, false);
                return IntPtr.Zero;
            }
            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        [DllImport("Comctl32.dll", SetLastError = true)]
        static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, nuint uIdSubclass, nuint dwRefData);

        [DllImport("Comctl32.dll", SetLastError = true)]
        static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, nuint dwRefData);

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }
    }
}
