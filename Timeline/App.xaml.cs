using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Timeline.Utils;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Timeline {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            ChangeTheme();

            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // 上传崩溃日志
            this.UnhandledException += OnUnhandledException;
            //TaskScheduler.UnobservedTaskException += OnUnobservedException;
            //AppDomain.CurrentDomain.UnhandledException += OnBgUnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null) {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false) {
                if (rootFrame.Content == null) {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();

                // 首次启动设置最佳窗口比例：16:10
                OptimizeSize();
                // 将内容扩展到标题栏，并使标题栏半透明
                TransTitleBar();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void ChangeTheme() {
            // 注意：ElementTheme.Default 指向该值，非系统主题
            switch (IniUtil.GetIni().Theme) {
                case "light":
                    this.RequestedTheme = ApplicationTheme.Light;
                    break;
                case "dark":
                    this.RequestedTheme = ApplicationTheme.Dark;
                    break;
            }
        }

        private void OptimizeSize() {
            // 保存显示器分辨率
            System.Drawing.Size screen = SysUtil.GetMonitorPhysicalPixels();
            ApplicationData.Current.LocalSettings.Values["Screen"] = (long)((screen.Width << 16) + screen.Height);
            // 调整窗口尺寸
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("OptimizeSize")) {
                ApplicationData.Current.LocalSettings.Values["OptimizeSize"] = true;
                ApplicationView.PreferredLaunchViewSize = new Size(960, 600);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }
        }

        private void TransTitleBar() {
            // https://docs.microsoft.com/zh-cn/windows/apps/design/style/acrylic#extend-acrylic-into-the-title-bar
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        private void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) {
            e.Handled = true;
            _ = Api.CrashAsync(e.Exception);
            LogUtil.E("OnUnhandledException() " + e.Exception.ToString());
        }

        //private void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e) {
        //    e.SetObserved();
        //    _ = Api.CrashAsync(e.Exception);
        //    LogUtil.E("OnUnobservedException() " + e.Exception.ToString());
        //}

        //private void OnBgUnhandledException(object sender, System.UnhandledExceptionEventArgs e) {
        //    Api.Crash((Exception)e.ExceptionObject);
        //}
    }
}
