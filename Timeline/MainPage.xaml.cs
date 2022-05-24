using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Timeline.Beans;
using Timeline.Pages;
using Timeline.Providers;
using Timeline.Utils;
using TimelineService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI.Core;
using Windows.UI.Shell;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Timeline {
    public delegate void BtnInfoLinkHandler();

    public sealed partial class MainPage : Page {
        private event BtnInfoLinkHandler InfoLink;

        private readonly ResourceLoader resLoader;
        private readonly ApplicationDataContainer localSettings;
        private CancellationTokenSource ctsLoad = new CancellationTokenSource();
        private CancellationTokenSource ctsShow = new CancellationTokenSource();

        private Ini ini = null;
        private BaseProvider provider = null;
        private Meta meta = null;

        private ReleaseApi release = null;

        private DispatcherTimer resizeTimer = null;
        private DispatcherTimer dislikeTimer = null;
        private DispatcherTimer pageTimer = null;
        private bool pageTimerYesterdayOrTomorrow = true;
        private Meta dislikeTimerMeta = null;

        private long imgAnimStart = DateTime.Now.Ticks;
        private long imgLoadStart = DateTime.Now.Ticks;

        private const string BG_TASK_NAME = "PushTask";
        private const string BG_TASK_NAME_TIMER = "PushTaskTimer";

        public MainPage() {
            this.InitializeComponent();

            resLoader = ResourceLoader.GetForCurrentView();
            localSettings = ApplicationData.Current.LocalSettings;
            Init();
            _ = LoadFocusAsync(ctsLoad.Token);
            _ = CheckLaunchAsync();
        }

        private void Init() {
            // 启动时页面获得焦点，使快捷键一开始即可用
            this.IsTabStop = true;

            TextTitle.Text = resLoader.GetString("AppDesc");

            pageTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            pageTimer.Tick += (sender2, e2) => {
                pageTimer.Stop();
                ctsLoad = new CancellationTokenSource();
                if (pageTimerYesterdayOrTomorrow) {
                    _ = LoadYesterdayAsync(ctsLoad.Token);
                } else {
                    _ = LoadTomorrowAsync(ctsLoad.Token);
                }
            };

            // 前者会在应用启动时触发多次，后者仅一次
            //this.SizeChanged += Current_SizeChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private async Task LoadFocusAsync(CancellationToken token) {
            _ = await InitProviderAsync();
            bool res = await provider.LoadData(token, ini.GetIni());
            _ = Api.StatsAsync(ini, res);
            if (token.IsCancellationRequested) {
                return;
            }
            if (!res) {
                LogUtil.E("LoadFocusAsync() failed to load data");
                StatusError();
                return;
            }

            meta = await provider.Focus();
            LogUtil.D("LoadFocusAsync() " + meta);
            if (meta == null) {
                StatusError();
                return;
            }
            ShowText(meta);
            Meta metaCache = await provider.CacheAsync(meta, token);
            if (token.IsCancellationRequested) {
                return;
            }
            if (metaCache != null && metaCache.Id.Equals(meta.Id)) {
                ShowImg(meta);
            }
        }

        private async Task LoadYesterdayAsync(CancellationToken token) {
            bool res = await provider.LoadData(token, ini.GetIni());
            if (token.IsCancellationRequested) {
                return;
            }
            if (!res) {
                LogUtil.E("LoadYesterdayAsync() failed to load data");
                StatusError();
                return;
            }

            meta = await provider.Yesterday();
            LogUtil.D("LoadYesterdayAsync() " + meta);
            if (meta == null) {
                StatusError();
                return;
            }
            ShowText(meta);
            Meta metaCache = await provider.CacheAsync(meta, token);
            if (token.IsCancellationRequested) {
                LogUtil.W("LoadYesterdayAsync() IsCancellationRequested " + metaCache.Id);
                return;
            }
            if (!token.IsCancellationRequested && metaCache != null && metaCache.Id.Equals(meta.Id)) {
                ShowImg(meta);
            }
        }

        private async Task LoadTomorrowAsync(CancellationToken token) {
            bool res = await provider.LoadData(token, ini.GetIni());
            if (token.IsCancellationRequested) {
                return;
            }
            if (!res) {
                LogUtil.E("LoadTomorrowAsync() failed to load data");
                StatusError();
                return;
            }

            meta = provider.Tomorrow();
            LogUtil.D("LoadTomorrowAsync() " + meta);
            if (meta == null) {
                StatusError();
                return;
            }
            ShowText(meta);
            Meta metaCache = await provider.CacheAsync(meta, token);
            if (token.IsCancellationRequested) {
                return;
            }
            if (!token.IsCancellationRequested && metaCache != null && metaCache.Id.Equals(meta.Id)) {
                ShowImg(meta);
            }
        }

        private async Task LoadTargetAsync(DateTime date, CancellationToken token) {
            bool res = await provider.LoadData(token, ini.GetIni(), date);
            if (token.IsCancellationRequested) {
                return;
            }
            if (!res) {
                LogUtil.E("LoadTargetAsync() failed to load data");
                StatusError();
                return;
            }

            meta = provider.Target(date);
            LogUtil.D("LoadTargetAsync() " + meta);
            if (meta == null) {
                StatusError();
                return;
            }
            ShowText(meta);
            Meta metaCache = await provider.CacheAsync(meta, token);
            if (token.IsCancellationRequested) {
                return;
            }
            if (!token.IsCancellationRequested && metaCache != null && metaCache.Id.Equals(meta.Id)) {
                ShowImg(meta);
                _ = Api.StatsAsync(ini, true);
            }
        }

        private async Task LoadEndAsync(bool farthestOrLatest, CancellationToken token) {
            bool res = await provider.LoadData(token, ini.GetIni());
            if (token.IsCancellationRequested) {
                return;
            }
            if (!res) {
                LogUtil.E("LoadEndAsync() failed to load data");
                StatusError();
                return;
            }

            meta = farthestOrLatest ? provider.Farthest() : provider.Latest();
            LogUtil.D("LoadEndAsync() " + meta);
            if (meta == null) {
                StatusError();
                return;
            }
            ShowText(meta);
            Meta metaCache = await provider.CacheAsync(meta, token);
            if (token.IsCancellationRequested) {
                return;
            }
            if (!token.IsCancellationRequested && metaCache != null && metaCache.Id.Equals(meta.Id)) {
                ShowImg(meta);
            }
        }

        private async Task<bool> InitProviderAsync() {
            if (ini == null) {
                ini = await IniUtil.GetIniAsync();
            }
            if (provider != null && provider.Id.Equals(ini.Provider)) {
                return true;
            }
            provider = ini.GenerateProvider();

            MenuCurDesktop.Label = string.Format(resLoader.GetString("CurDesktop"), resLoader.GetString("Provider_" + ini.DesktopProvider));
            MenuCurLock.Label = string.Format(resLoader.GetString("CurLock"), resLoader.GetString("Provider_" + ini.LockProvider));
            if (string.IsNullOrEmpty(ini.DesktopProvider)) {
                MenuPushDesktopIcon.Visibility = Visibility.Collapsed;
                MenuCurDesktopIcon.Visibility = Visibility.Collapsed;
                MenuCurDesktop.Visibility = Visibility.Collapsed;
            } else if (ini.DesktopProvider.Equals(ini.Provider)) {
                MenuPushDesktopIcon.Visibility = Visibility.Visible;
                MenuCurDesktopIcon.Visibility = Visibility.Collapsed;
                MenuCurDesktop.Visibility = Visibility.Collapsed;
            } else {
                MenuPushDesktopIcon.Visibility = Visibility.Collapsed;
                MenuCurDesktopIcon.Visibility = Visibility.Visible;
                MenuCurDesktop.Visibility = Visibility.Visible;
            }
            if (string.IsNullOrEmpty(ini.LockProvider)) {
                MenuPushLockIcon.Visibility = Visibility.Collapsed;
                MenuCurLock.Visibility = Visibility.Collapsed;
            } else if (ini.LockProvider.Equals(ini.Provider)) {
                MenuPushLockIcon.Visibility = Visibility.Visible;
                MenuCurLock.Visibility = Visibility.Collapsed;
            } else {
                MenuPushLockIcon.Visibility = Visibility.Collapsed;
                MenuCurLockIcon.Visibility = Visibility.Visible;
                MenuCurLock.Visibility = Visibility.Visible;
            }
            if (string.IsNullOrEmpty(ini.DesktopProvider) && string.IsNullOrEmpty(ini.LockProvider)) {
                UnregService();
            } else {
                _ = RegServiceAsync();
                if (ini.DesktopProvider.Equals(ini.Provider) || ini.LockProvider.Equals(ini.Provider)) {
                    _ = RunServiceNowAsync(); // 用户浏览图源与推送图源一致，立即推送一次
                }
            }

            RadioMenuFlyoutItem item = SubmenuProvider.Items.Cast<RadioMenuFlyoutItem>().FirstOrDefault(c => ini.Provider.Equals(c?.Tag?.ToString()));
            if (item != null) {
                item.IsChecked = true;
            }

            return true;
        }

        private void ShowText(Meta meta) {
            LogUtil.D("ShowText() " + meta.Id);
            if (meta == null) {
                return;
            }

            // 标题按图片标题 > 图片副标题 > APP名称优先级显示，不会为空
            if (!string.IsNullOrEmpty(meta.Title)) {
                TextTitle.Text = meta.Title;
                TextDetailCaption.Text = meta.Caption ?? "";
            } else {
                TextTitle.Text = !string.IsNullOrEmpty(meta.Caption) ? meta.Caption
                    : resLoader.GetString("Slogan_" + provider.Id);
                TextDetailCaption.Text = "";
            }
            TextDetailCaption.Visibility = TextDetailCaption.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 位置
            TextDetailLocation.Text = meta.Location ?? "";
            TextDetailLocation.Visibility = TextDetailLocation.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 图文故事
            TextDetailStory.Text = meta.Story ?? "";
            TextDetailStory.Visibility = TextDetailStory.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 版权所有者
            TextDetailCopyright.Text = meta.Copyright ?? "";
            TextDetailCopyright.Visibility = TextDetailCopyright.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 日期（保持可见）
            TextDetailDate.Text = meta.Date?.ToLongDateString();
            TextDetailDate.Visibility = Visibility.Visible;
            // 文件属性（保持可见）
            TextDetailProperties.Text = resLoader.GetString("Provider_" + provider.Id)
                + (meta.Cate != null ? (" · " + meta.Cate) : "");
            TextDetailProperties.Visibility = Visibility.Visible;
        }

        private void ShowImg(Meta meta) {
            LogUtil.D("ShowImg() {0} {1}ms", meta?.Id, (int)((DateTime.Now.Ticks - imgAnimStart) / 10000));
            if (meta == null) {
                return;
            }
            imgLoadStart = DateTime.Now.Ticks;

            // 显示图片
            float winW = Window.Current.Content.ActualSize.X;
            float winH = Window.Current.Content.ActualSize.Y;
            BitmapImage biUhd = new BitmapImage();
            ImgUhd.Source = biUhd;
            biUhd.DecodePixelType = DecodePixelType.Logical;
            if (meta.Dimen.Width * 1.0f / meta.Dimen.Height > winW / winH) { // 图片比窗口宽，缩放至与窗口等高
                biUhd.DecodePixelWidth = (int)Math.Round(winH * meta.Dimen.Width / meta.Dimen.Height);
                biUhd.DecodePixelHeight = (int)Math.Round(winH);
            } else { // 图片比窗口窄，缩放至与窗口等宽
                biUhd.DecodePixelWidth = (int)Math.Round(winW);
                biUhd.DecodePixelHeight = (int)Math.Round(winW * meta.Dimen.Height / meta.Dimen.Width);
            }
            LogUtil.D("ShowImg() {0}x{1}, win logical: {2}x{3}, scale logical: {4}x{5}",
                meta.Dimen.Width, meta.Dimen.Height, winW, winH, biUhd.DecodePixelWidth, biUhd.DecodePixelHeight);
            biUhd.UriSource = new Uri(meta.CacheUhd != null ? meta.CacheUhd.Path : "ms-appx:///Assets/Images/default.png", UriKind.Absolute);

            if (meta.CacheUhd != null) {
                // 显示与图片文件相关的信息
                string source = resLoader.GetString("Provider_" + provider.Id) + (meta.Cate != null ? (" · " + meta.Cate) : "");
                string fileSize = FileUtil.ConvertFileSize(File.Exists(meta.CacheUhd.Path)
                    ? new FileInfo(meta.CacheUhd.Path).Length : 0);
                TextDetailProperties.Text = string.Format("{0} / {1}x{2}, {3}",
                    source, meta.Dimen.Width, meta.Dimen.Height, fileSize);
                TextDetailProperties.Visibility = Visibility.Visible;
                // 根据人脸识别优化组件放置位置
                bool faceLeft = meta.FaceOffset < 0.4;
                RelativePanel.SetAlignLeftWithPanel(ViewBarPointer, !faceLeft);
                RelativePanel.SetAlignRightWithPanel(ViewBarPointer, faceLeft);
                RelativePanel.SetAlignLeftWithPanel(Info, !faceLeft);
                RelativePanel.SetAlignRightWithPanel(Info, faceLeft);
                RelativePanel.SetAlignLeftWithPanel(AnchorGo, faceLeft);
                RelativePanel.SetAlignRightWithPanel(AnchorGo, !faceLeft);
            }
        }

        private void ReDecodeImg() {
            if (ImgUhd.Source == null) {
                return;
            }
            BitmapImage bi = ImgUhd.Source as BitmapImage;
            if (bi.PixelHeight == 0) {
                LogUtil.D("ReDecodeImg() bi.PixelWidth 0");
                return;
            }
            bi.DecodePixelType = DecodePixelType.Logical;
            float winW = Window.Current.Content.ActualSize.X;
            float winH = Window.Current.Content.ActualSize.Y;
            if (bi.PixelWidth * 1.0f / bi.PixelHeight > winW / winH) { // 图片比窗口宽，缩放至与窗口等高
                bi.DecodePixelWidth = (int)Math.Round(winH * bi.PixelWidth / bi.PixelHeight);
                bi.DecodePixelHeight = (int)Math.Round(winH);
            } else { // 图片比窗口窄，缩放至与窗口等宽
                bi.DecodePixelWidth = (int)Math.Round(winW);
                bi.DecodePixelHeight = (int)Math.Round(winW * bi.PixelHeight / bi.PixelWidth);
            }
            LogUtil.D("ReDecodeImg() {0}x{1}, win logical: {2}x{3}, scale logical: {4}x{5}",
                bi.PixelWidth, bi.PixelHeight, winW, winH, bi.DecodePixelWidth, bi.DecodePixelHeight);
        }

        private void StatusLoading() {
            ctsShow.Cancel();
            imgAnimStart = DateTime.Now.Ticks;
            
            ImgUhd.Opacity = 0;
            ImgUhd.Scale = new Vector3(1.014f, 1.014f, 1.014f);

            if (ProgressLoading.ShowPaused || ProgressLoading.ShowError) {
                ProgressLoading.ShowPaused = false;
                ProgressLoading.ShowError = false;
                ProgressLoading.Visibility = Visibility.Visible;
            }
        }

        private async Task StatusEnjoyAsync(CancellationToken token) {
            await Task.Delay(Math.Max(0, 500 - (int)((DateTime.Now.Ticks - imgAnimStart) / 10000)));
            if (token.IsCancellationRequested) {
                LogUtil.D("StatusEnjoyAsync() IsCancellationRequested");
                return;
            }

            MenuSetDesktop.IsEnabled = true;
            MenuSetLock.IsEnabled = true;
            MenuSave.IsEnabled = true;
            MenuDislike.IsEnabled = true;
            MenuFillOn.IsEnabled = true;
            MenuFillOff.IsEnabled = true;

            ImgUhd.Opacity = 1;
            ImgUhd.Scale = new Vector3(1, 1, 1);

            ProgressLoading.ShowPaused = true;
            ProgressLoading.ShowError = false;
            ProgressLoading.Visibility = ViewStory.Visibility;
        }

        private void StatusError() {
            ImgUhd.Opacity = 0;

            TextTitle.Text = resLoader.GetString("AppDesc");
            TextDetailCaption.Visibility = Visibility.Collapsed;
            TextDetailLocation.Visibility = Visibility.Collapsed;
            TextDetailStory.Visibility = Visibility.Collapsed;
            TextDetailCopyright.Visibility = Visibility.Collapsed;
            TextDetailDate.Visibility = Visibility.Collapsed;
            TextDetailProperties.Visibility = Visibility.Collapsed;

            ProgressLoading.ShowError = true;
            ProgressLoading.Visibility = Visibility.Visible;

            MenuSetDesktop.IsEnabled = false;
            MenuSetLock.IsEnabled = false;
            MenuSave.IsEnabled = false;
            MenuDislike.IsEnabled = false;
            MenuFillOn.IsEnabled = false;
            MenuFillOff.IsEnabled = false;

            ToggleInfo(null, !NetworkInterface.GetIsNetworkAvailable() ? resLoader.GetString("MsgNoInternet")
                : string.Format(resLoader.GetString("MsgLostProvider"), resLoader.GetString("Provider_" + provider.Id)));
        }

        private async Task SetWallpaperAsync(Meta meta, bool setDesktopOrLock) {
            if (meta?.CacheUhd == null) {
                return;
            }

            if (!UserProfilePersonalizationSettings.IsSupported()) {
                ToggleInfo(null, resLoader.GetString("MsgWallpaper0"));
                return;
            }
            try {
                if (setDesktopOrLock) {
                    // Your app can't set wallpapers from any folder.
                    // Copy file in ApplicationData.Current.LocalFolder and set wallpaper from there.
                    StorageFile fileWallpaper = await meta.CacheUhd.CopyAsync(ApplicationData.Current.LocalFolder,
                        "desktop", NameCollisionOption.ReplaceExisting);
                    bool wallpaperSet = await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(fileWallpaper);
                    if (wallpaperSet) {
                        ToggleInfo(null, resLoader.GetString("MsgSetDesktop1"), InfoBarSeverity.Success);
                    } else {
                        ToggleInfo(null, resLoader.GetString("MsgSetDesktop0"));
                    }
                } else {
                    StorageFile fileWallpaper = await meta.CacheUhd.CopyAsync(ApplicationData.Current.LocalFolder,
                        "lock", NameCollisionOption.ReplaceExisting);
                    bool wallpaperSet = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileWallpaper);
                    if (wallpaperSet) {
                        ToggleInfo(null, resLoader.GetString("MsgSetLock1"), InfoBarSeverity.Success);
                    } else {
                        ToggleInfo(null, resLoader.GetString("MsgSetLock0"));
                    }
                }
            } catch (Exception e) {
                LogUtil.E("SetWallpaper() " + e.Message);
            }
        }

        private async Task DownloadAsync() {
            ToggleInfo(null, resLoader.GetString("MsgSave"), InfoBarSeverity.Informational);
            StorageFile file = await provider.DownloadAsync(meta, resLoader.GetString("AppNameShort"),
                resLoader.GetString("Provider_" + provider.Id));
            if (file != null) {
                ToggleInfo(null, resLoader.GetString("MsgSave1"), InfoBarSeverity.Success, resLoader.GetString("ActionGo"), () => {
                    ToggleInfo(null, null);
                    _ = LaunchPicLibAsync(file);
                });
            } else {
                ToggleInfo(null, resLoader.GetString("MsgSave0"));
            }
        }

        private async Task LaunchPicLibAsync(StorageFile fileSelected) {
            try {
                var folder = await KnownFolders.PicturesLibrary.GetFolderAsync(resLoader.GetString("AppNameShort"));
                FolderLauncherOptions options = new FolderLauncherOptions();
                if (fileSelected != null) { // 打开文件夹同时选中目标文件
                    options.ItemsToSelect.Add(fileSelected);
                }
                _ = await Launcher.LaunchFolderAsync(folder, options);
            } catch (Exception e) {
                LogUtil.E("LaunchPicLib() " + e.Message);
            }
        }

        private async Task LaunchRelealseAsync() {
            try {
                _ = await Launcher.LaunchUriAsync(new Uri(release?.Url));
            } catch (Exception e) {
                LogUtil.E("LaunchRelealse() " + e.Message);
            }
        }

        private void ToggleFullscreenMode() {
            ToggleFullscreenMode(!ApplicationView.GetForCurrentView().IsFullScreenMode);
        }

        private void ToggleFullscreenMode(bool fullScreen) {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (fullScreen) {
                if (view.TryEnterFullScreenMode()) {
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
                }
            } else {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }
        }

        private void ToggleInfo(string title, string msg, InfoBarSeverity severity = InfoBarSeverity.Error,
            string action = null, BtnInfoLinkHandler handler = null) {
            if (string.IsNullOrEmpty(msg)) {
                Info.IsOpen = false;
                return;
            }
            Info.Severity = severity;
            Info.Title = title ?? "";
            Info.Message = msg;
            InfoLink = handler;
            BtnInfoLink.Content = action ?? resLoader.GetString("ActionGo");
            BtnInfoLink.Visibility = handler != null ? Visibility.Visible : Visibility.Collapsed;
            Info.IsOpen = true;
        }

        private void ToggleImgMode(bool fillOn) {
            ImgUhd.Stretch = fillOn ? Stretch.UniformToFill : Stretch.Uniform;
            MenuFillOn.Visibility = fillOn ? Visibility.Collapsed : Visibility.Visible;
            MenuFillOff.Visibility = fillOn ? Visibility.Visible : Visibility.Collapsed;

            ToggleInfo(null, fillOn ? resLoader.GetString("MsgUniformToFill") : resLoader.GetString("MsgUniform"),
                InfoBarSeverity.Informational);
        }

        private async Task CheckLaunchAsync() {
            int actions = (int)(localSettings.Values["Actions"] ?? 0);
            localSettings.Values["Actions"] = ++actions;
            // 检查菜单提示
            if (!localSettings.Values.ContainsKey("MenuLearned")) {
                await Task.Delay(1000);
                ToggleInfo(resLoader.GetString("MsgMenu"), resLoader.GetString("MsgMenuDesc"), InfoBarSeverity.Informational);
                return;
            }
            // 检查任务栏固定提示
            if (TaskbarManager.GetDefault().IsPinningAllowed && !await TaskbarManager.GetDefault().IsCurrentAppPinnedAsync()) {
                int times = (int)(localSettings.Values["CheckPinTimes"] ?? 0);
                localSettings.Values["CheckPinTimes"] = ++times;
                if (times == 2) {
                    await Task.Delay(1000);
                    ToggleInfo(null, resLoader.GetString("MsgPin"), InfoBarSeverity.Informational, resLoader.GetString("ActionPin"), async () => {
                        _ = await TaskbarManager.GetDefault().RequestPinCurrentAppAsync();
                    });
                    return;
                }
            }
            // 检查评分提示
            if (!localSettings.Values.ContainsKey("ReqReview") && actions >= 15) {
                await Task.Delay(1000);
                FlyoutMenu.Hide();
                var action = await new ReviewDlg {
                    RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
                }.ShowAsync();
                if (action == ContentDialogResult.Primary) {
                    localSettings.Values["ReqReview"] = true;
                    _ = Launcher.LaunchUriAsync(new Uri(resLoader.GetStringForUri(new Uri("ms-resource:///Resources/LinkReview/NavigateUri"))));
                } else { // 下次一定
                    localSettings.Values.Remove("Actions");
                }
                return;
            }
            // 检查更新
            release = await Api.CheckUpdateAsync();
            if (!string.IsNullOrEmpty(release.Url)) {
                await Task.Delay(1000);
                ToggleInfo(null, resLoader.GetString("MsgUpdate"), InfoBarSeverity.Informational, resLoader.GetString("ActionGo"), () => {
                    ToggleInfo(null, null);
                    _ = LaunchRelealseAsync();
                });
            }
        }

        private async Task<bool> RegServiceAsync() {
            BackgroundAccessStatus reqStatus = await BackgroundExecutionManager.RequestAccessAsync();
            LogUtil.D("RegService() RequestAccessAsync " + reqStatus);
            if (reqStatus != BackgroundAccessStatus.AlwaysAllowed
                && reqStatus != BackgroundAccessStatus.AllowedSubjectToSystemPolicy) {
                ToggleInfo(null, resLoader.GetString("TitleErrPush"));
                return false;
            }
            if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(BG_TASK_NAME_TIMER))) {
                LogUtil.W("RegService() service registered already");
                return true;
            }

            BackgroundTaskBuilder builder = new BackgroundTaskBuilder {
                Name = BG_TASK_NAME_TIMER,
                TaskEntryPoint = typeof(PushService).FullName
            };
            // 触发任务的事件
            builder.SetTrigger(new TimeTrigger(60, false)); // 周期执行（不低于15min）
            // 触发任务的先决条件
            builder.AddCondition(new SystemCondition(SystemConditionType.SessionConnected)); // Internet 必须连接
            _ = builder.Register();

            LogUtil.D("RegService() service registered");
            return true;
        }

        private void UnregService() {
            foreach (var ta in BackgroundTaskRegistration.AllTasks) {
                if (ta.Value.Name == BG_TASK_NAME_TIMER) {
                    ta.Value.Unregister(true);
                    LogUtil.D("UnregService() service BG_TASK_NAME_TIMER unregistered");
                } else if (ta.Value.Name == BG_TASK_NAME) {
                    ta.Value.Unregister(true);
                    LogUtil.D("UnregService() service BG_TASK_NAME unregistered");
                }
            }
        }

        private async Task RunServiceNowAsync() {
            LogUtil.D("RunServiceNow()");

            ApplicationTrigger _AppTrigger = null;
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == BG_TASK_NAME) { // 已注册
                    _AppTrigger = (task.Value as BackgroundTaskRegistration).Trigger as ApplicationTrigger;
                    break;
                }
            }
            if (_AppTrigger == null) { // 后台任务从未注册过
                _AppTrigger = new ApplicationTrigger();

                BackgroundTaskBuilder builder = new BackgroundTaskBuilder {
                    Name = BG_TASK_NAME,
                    TaskEntryPoint = typeof(PushService).FullName
                };
                builder.SetTrigger(_AppTrigger);
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                _ = builder.Register();
            }
            _ = await _AppTrigger.RequestAsync();
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e) {
            if (resizeTimer == null) {
                resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                resizeTimer.Tick += (sender2, e2) => {
                    resizeTimer.Stop();
                    ReDecodeImg();
                };
            }
            resizeTimer.Stop();
            resizeTimer.Start();
        }

        private void MenuYesterday_Click(object sender, RoutedEventArgs e) {
            pageTimerYesterdayOrTomorrow = true;
            
            AnimeYesterday1.Begin();

            if (!localSettings.Values.ContainsKey("YesterdayLearned")) {
                localSettings.Values["YesterdayLearned"] = true;
                ToggleInfo(resLoader.GetString("MsgYesterday"), resLoader.GetString("MsgYesterdayDesc"), InfoBarSeverity.Informational);
            }

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private void MenuSetDesktop_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            _ = SetWallpaperAsync(meta, true);
            _ = Api.RankAsync(ini, meta, "desktop");
            localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
        }

        private void MenuSetLock_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            _ = SetWallpaperAsync(meta, false);
            _ = Api.RankAsync(ini, meta, "lock");
            localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
        }

        private void MenuFill_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ToggleImgMode(ImgUhd.Stretch != Stretch.UniformToFill);
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            _ = DownloadAsync();
            _ = Api.RankAsync(ini, meta, "save");
            localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
        }

        private void MenuDislike_Click(object sender, RoutedEventArgs e) {
            dislikeTimerMeta = meta;

            FlyoutMenu.Hide();

            ToggleInfo(null, resLoader.GetString("MsgMarkDislike"), InfoBarSeverity.Success, resLoader.GetString("ActionUndo"), () => {
                ToggleInfo(null, null);
                _ = Api.RankAsync(ini, meta, "dislike", true);
            });

            if (dislikeTimer == null) {
                dislikeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                dislikeTimer.Tick += (sender2, e2) => {
                    dislikeTimer.Stop();
                    _ = Api.RankAsync(ini, dislikeTimerMeta, "dislike");
                };
            }
            dislikeTimer.Stop();
            dislikeTimer.Start();
        }

        private void MenuPush_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();

            AppBarButton menuCheck = sender as AppBarButton;
            if (MenuPushDesktop.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushDesktopIcon.Visibility == Visibility.Visible) {
                    MenuPushDesktopIcon.Visibility = Visibility.Collapsed;
                } else {
                    MenuPushDesktopIcon.Visibility = Visibility.Visible;
                    MenuCurDesktopIcon.Visibility = Visibility.Collapsed;
                    MenuCurDesktop.Visibility = Visibility.Collapsed;
                }
            } else if (MenuCurDesktop.Tag.Equals(menuCheck.Tag)) {
                MenuCurDesktopIcon.Visibility = Visibility.Collapsed;
                MenuCurDesktop.Visibility = Visibility.Collapsed;
            }
            if (MenuPushLock.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushLockIcon.Visibility == Visibility.Visible) {
                    MenuPushLockIcon.Visibility = Visibility.Collapsed;
                } else {
                    MenuPushLockIcon.Visibility = Visibility.Visible;
                    MenuCurLockIcon.Visibility = Visibility.Collapsed;
                    MenuCurLock.Visibility = Visibility.Collapsed;
                }
            } else if (MenuCurLock.Tag.Equals(menuCheck.Tag)) {
                MenuCurLockIcon.Visibility = Visibility.Collapsed;
                MenuCurLock.Visibility = Visibility.Collapsed;
            }

            if (MenuCurDesktopIcon.Visibility == Visibility.Collapsed) {
                ini.DesktopProvider = MenuPushDesktopIcon.Visibility == Visibility.Visible ? provider.Id : "";
                _ = IniUtil.SaveDesktopProviderAsync(ini.DesktopProvider);
            }
            if (MenuCurLockIcon.Visibility == Visibility.Collapsed) {
                ini.LockProvider = MenuPushLockIcon.Visibility == Visibility.Visible ? provider.Id : "";
                _ = IniUtil.SaveLockProviderAsync(ini.LockProvider);
            }

            if (string.IsNullOrEmpty(ini.DesktopProvider) && string.IsNullOrEmpty(ini.LockProvider)) {
                UnregService();
            } else {
                _ = RegServiceAsync();
                if ((MenuPushDesktop.Tag.Equals(menuCheck.Tag) && MenuPushDesktopIcon.Visibility == Visibility.Visible)
                    || (MenuPushLock.Tag.Equals(menuCheck.Tag) && MenuPushLockIcon.Visibility == Visibility.Visible)) {
                    _ = RunServiceNowAsync(); // 用户浏览图源与推送图源一致，立即推送一次
                }
            }

            if (MenuPushDesktop.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushDesktopIcon.Visibility == Visibility.Visible) {
                    ToggleInfo(null, resLoader.GetString("MsgPushDesktopOn"), InfoBarSeverity.Success);
                } else {
                    ToggleInfo(null, resLoader.GetString("MsgPushDesktopOff"), InfoBarSeverity.Warning);
                }
            }
            if (MenuCurDesktop.Tag.Equals(menuCheck.Tag)) {
                ToggleInfo(null, resLoader.GetString("MsgPushDesktopOff"), InfoBarSeverity.Warning);
            }
            if (MenuPushLock.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushLockIcon.Visibility == Visibility.Visible) {
                    ToggleInfo(null, resLoader.GetString("MsgPushLockOn"), InfoBarSeverity.Success);
                } else {
                    ToggleInfo(null, resLoader.GetString("MsgPushLockOff"), InfoBarSeverity.Warning);
                }
            }
            if (MenuCurLock.Tag.Equals(menuCheck.Tag)) {
                ToggleInfo(null, resLoader.GetString("MsgPushLockOff"), InfoBarSeverity.Warning);
            }
        }

        private void MenuProvider_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ViewSplit.IsPaneOpen = false;

            string providerIdNew = ((RadioMenuFlyoutItem)sender).Tag.ToString();
            _ = IniUtil.SaveProviderAsync(providerIdNew);
            ini = null;
            provider = null;
            StatusLoading();
            ctsLoad.Cancel();
            ctsLoad = new CancellationTokenSource();
            _ = LoadFocusAsync(ctsLoad.Token);

            ToggleInfo(null, string.Format(resLoader.GetString("MsgProvider"),
                resLoader.GetString("Provider_" + providerIdNew)), InfoBarSeverity.Informational);
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ViewSettings.BeforePaneOpen(ini);
            ViewSplit.IsPaneOpen = true;
        }

        private void ImgUhd_ImageOpened(object sender, RoutedEventArgs e) {
            LogUtil.D("ImgUhd_ImageOpened() {0} {1}ms", meta?.Id, (int)((DateTime.Now.Ticks - imgLoadStart) / 10000));
            ctsShow = new CancellationTokenSource();
            _ = StatusEnjoyAsync(ctsShow.Token);
        }

        private void ImgUhd_ImageFailed(object sender, ExceptionRoutedEventArgs e) {
            LogUtil.E("ImgUhd_ImageFailed() " + meta?.Id);
            ctsShow = new CancellationTokenSource();
            _ = StatusEnjoyAsync(ctsShow.Token);
        }

        private void BtnInfoLink_Click(object sender, RoutedEventArgs e) {
            InfoLink?.Invoke();
        }

        private void ViewBarPointer_PointerEntered(object sender, PointerRoutedEventArgs e) {
            ProgressLoading.Visibility = Visibility.Visible;
            ViewStory.Visibility = Visibility.Visible;
            ToggleInfo(null, null);
        }

        private void ViewBarPointer_PointerExited(object sender, PointerRoutedEventArgs e) {
            if (ProgressLoading.ShowPaused && !ProgressLoading.ShowError) {
                ProgressLoading.Visibility = Visibility.Collapsed;
            }
            ViewStory.Visibility = Visibility.Collapsed;
            ToggleInfo(null, null);
        }

        private void ViewBarPointer_ContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            // 阻止在标题区弹出菜单
            args.Handled = true;
        }

        private void BoxGo_KeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == VirtualKey.Enter) {
                FlyoutGo.Hide();
                DateTime date = DateUtil.ParseDate(BoxGo.Text);
                if (date.Date != meta?.Date?.Date) {
                    ctsLoad.Cancel();
                    StatusLoading();
                    ctsLoad = new CancellationTokenSource();
                    _ = LoadTargetAsync(date, ctsLoad.Token);
                }
            }
        }

        private void FlyoutMenu_Opened(object sender, object e) {
            localSettings.Values["MenuLearned"] = true;
            ToggleInfo(null, null);
        }

        private void ViewMain_Tapped(object sender, TappedRoutedEventArgs e) {
            ViewSplit.IsPaneOpen = false;
            ToggleInfo(null, null);
        }

        private void ViewMain_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            if (!(e.OriginalSource as FrameworkElement).Equals(ViewMain)
                && !(e.OriginalSource as FrameworkElement).Equals(ImgUhd)) {
                return;
            }
            ToggleFullscreenMode();
        }

        private void ViewMain_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            if (Math.Abs(e.Cumulative.Translation.X) <= 100 && Math.Abs(e.Cumulative.Translation.Y) <= 100) {
                return;
            }
            pageTimerYesterdayOrTomorrow = e.Cumulative.Translation.X < -100 || e.Cumulative.Translation.Y < -100;
            e.Handled = true;

            ToggleInfo(null, null);

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private void ViewMain_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
            pageTimerYesterdayOrTomorrow = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta > 0;

            ToggleInfo(null, null);

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private void KeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) {
            ToggleInfo(null, null);
            switch (sender.Key) {
                case VirtualKey.Left:
                case VirtualKey.Up:
                    pageTimerYesterdayOrTomorrow = true;
                    ctsLoad.Cancel();
                    StatusLoading();
                    pageTimer.Stop();
                    pageTimer.Start();
                    break;
                case VirtualKey.Right:
                case VirtualKey.Down:
                    pageTimerYesterdayOrTomorrow = false;
                    ctsLoad.Cancel();
                    StatusLoading();
                    pageTimer.Stop();
                    pageTimer.Start();
                    break;
                case VirtualKey.Escape:
                case VirtualKey.Enter:
                    ToggleFullscreenMode();
                    break;
                case VirtualKey.Home:
                case VirtualKey.PageUp:
                    ctsLoad.Cancel();
                    StatusLoading();
                    ctsLoad = new CancellationTokenSource();
                    _ = LoadEndAsync(true, ctsLoad.Token);
                    break;
                case VirtualKey.End:
                case VirtualKey.PageDown:
                    ctsLoad.Cancel();
                    StatusLoading();
                    ctsLoad = new CancellationTokenSource();
                    _ = LoadEndAsync(false, ctsLoad.Token);
                    break;
                case VirtualKey.Space:
                    ToggleImgMode(ImgUhd.Stretch != Stretch.UniformToFill);
                    break;
                case VirtualKey.Back:
                case VirtualKey.Delete:
                    MenuDislike_Click(null, null);
                    break;
                case VirtualKey.Number1:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        _ = Api.RankAsync(ini, meta, "dislike");
                        ToggleInfo(null, resLoader.GetString("MsgMarkDislike"), InfoBarSeverity.Success, resLoader.GetString("ActionUndo"), () => {
                            ToggleInfo(null, null);
                            _ = Api.RankAsync(ini, meta, "dislike", true);
                        });
                    }
                    break;
                case VirtualKey.Number2:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        _ = Api.RankAsync(ini, meta, "r18");
                        ToggleInfo(null, resLoader.GetString("MsgMarkR18"), InfoBarSeverity.Success, resLoader.GetString("ActionUndo"), () => {
                            ToggleInfo(null, null);
                            _ = Api.RankAsync(ini, meta, "r18", true);
                        });
                    }
                    break;
                case VirtualKey.Number3:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        _ = Api.RankAsync(ini, meta, "acg");
                        ToggleInfo(null, resLoader.GetString("MsgMarkAcg"), InfoBarSeverity.Success, resLoader.GetString("ActionUndo"), () => {
                            ToggleInfo(null, null);
                            _ = Api.RankAsync(ini, meta, "acg", true);
                        });
                    }
                    break;
                case VirtualKey.Number4:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        _ = Api.RankAsync(ini, meta, "photograph");
                        ToggleInfo(null, resLoader.GetString("MsgMarkPhotograph"), InfoBarSeverity.Success, resLoader.GetString("ActionUndo"), () => {
                            ToggleInfo(null, null);
                            _ = Api.RankAsync(ini, meta, "photograph", true);
                        });
                    }
                    break;
                case VirtualKey.B:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        MenuSetDesktop_Click(null, null);
                    }
                    break;
                case VirtualKey.L:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        MenuSetLock_Click(null, null);
                    }
                    break;
                case VirtualKey.D:
                case VirtualKey.S:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        MenuSave_Click(null, null);
                    }
                    break;
                case VirtualKey.C:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        if (TextUtil.Copy(meta?.CacheUhd)) {
                            ToggleInfo(null, resLoader.GetString("MsgCopiedImg"), InfoBarSeverity.Success);
                            _ = Api.RankAsync(ini, meta, "copy");
                        }
                    } else { // Shift + Control
                        if (meta != null) {
                            TextUtil.Copy(JsonConvert.SerializeObject(meta, Formatting.Indented));
                            ToggleInfo(null, resLoader.GetString("MsgCopiedMeta"), InfoBarSeverity.Success);
                        }
                    }
                    break;
                case VirtualKey.R:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) {
                        FlyoutMenu.Hide();
                        Refresh();
                    }
                    break;
                case VirtualKey.F5:
                    FlyoutMenu.Hide();
                    Refresh();
                    break;
                case VirtualKey.F:
                case VirtualKey.G:
                    if (ini.GetIni().IsSequential() && !FlyoutGo.IsOpen) {
                        BoxGo.PlaceholderText = string.Format(resLoader.GetString("CurDate"), meta?.Date?.ToString("M") ?? "MMdd");
                        BoxGo.Text = "";
                        FlyoutBase.ShowAttachedFlyout(AnchorGo);
                    } else {
                        FlyoutGo.Hide();
                    }
                    break;
            }
            args.Handled = true;
        }

        private void Refresh() {
            ToggleInfo(null, string.Format(resLoader.GetString("MsgRefresh"),
                resLoader.GetString("Provider_" + provider.Id)), InfoBarSeverity.Informational);

            ini = null;
            provider = null;
            StatusLoading();
            ctsLoad.Cancel();
            ctsLoad = new CancellationTokenSource();
            _ = LoadFocusAsync(ctsLoad.Token);
        }

        private void AnimeYesterday1_Completed(object sender, object e) {
            AnimeYesterday2.Begin();
        }

        private void MenuSettings_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeSettings.Begin();
        }

        private void MenuDislike_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeDislike.Begin();
        }

        private void MenuFillOff_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeFillOff.Begin();
        }

        private void ViewSettings_SettingsChanged(object sender, SettingsEventArgs e) {
            if (e.Provider != null) {
                _ = IniUtil.SaveProviderAsync(e.Provider);
                ini = null;
                provider = null;
                StatusLoading();
                ctsLoad.Cancel();
                ctsLoad = new CancellationTokenSource();
                _ = LoadFocusAsync(ctsLoad.Token);

                ToggleInfo(null, string.Format(resLoader.GetString("MsgProvider"),
                    resLoader.GetString("Provider_" + e.Provider)), InfoBarSeverity.Informational);
            } else if (e.ProviderConfigChanged) {
                ini = null;
                provider = null;
                StatusLoading();
                ctsLoad.Cancel();
                ctsLoad = new CancellationTokenSource();
                _ = LoadFocusAsync(ctsLoad.Token);
            }
            if (e.ThemeChanged) { // 修复 muxc:CommandBarFlyout.SecondaryCommands 子元素无法响应随主题改变的BUG
                ElementTheme theme = ThemeUtil.ParseTheme(ini.Theme);
                MenuProvider.RequestedTheme = theme;
                MenuSetDesktop.RequestedTheme = theme;
                MenuSetLock.RequestedTheme = theme;
                MenuPushDesktop.RequestedTheme = theme;
                MenuCurDesktop.RequestedTheme = theme;
                MenuPushLock.RequestedTheme = theme;
                MenuCurLock.RequestedTheme = theme;
                Separator1.RequestedTheme = theme;
                Separator2.RequestedTheme = theme;
                Separator3.RequestedTheme = theme;
                foreach (RadioMenuFlyoutItem item in SubmenuProvider.Items.Cast<RadioMenuFlyoutItem>()) {
                    item.RequestedTheme = theme;
                }
                // 刷新状态颜色
                ProgressLoading.ShowError = !ProgressLoading.ShowError;
                ProgressLoading.ShowError = !ProgressLoading.ShowError;
            }
        }

        private async void ViewSettings_ContributeChanged(object sender, EventArgs e) {
            ContributeDlg dlg = new ContributeDlg {
                RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
            };
            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary) {
                ContributeApiReq req = dlg.GetContent();
                bool status = await Api.ContributeAsync(req);
                if (status) {
                    ToggleInfo(null, resLoader.GetString("MsgContribute1"), InfoBarSeverity.Success);
                } else {
                    ToggleInfo(null, resLoader.GetString("MsgContribute0"), InfoBarSeverity.Warning);
                }
            }
        }
    }
}
