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
using Windows.UI.Xaml.Documents;
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

        private Ini ini;
        private BaseProvider provider;
        private Meta meta = null;
        private bool r18 = false;
        private ReleaseApi release = null;
        private long imgAnimStart = DateTime.Now.Ticks;
        private long imgLoadStart = DateTime.Now.Ticks;

        private DispatcherTimer resizeTimer = null;
        private DispatcherTimer pageTimer;
        private PageAction pageTimerAction = PageAction.Yesterday;
        private Meta markTimerMeta = null;
        private string markTimerAction = null;

        private const string BG_TASK_NAME = "PushTask";
        private const string BG_TASK_NAME_TIMER = "PushTaskTimer";
        private enum PageAction {
            Focus,
            Yesterday,
            Tomorrow,
            Target
        }

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

            pageTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
            pageTimer.Tick += (sender2, e2) => {
                pageTimer.Stop();
                ctsLoad = new CancellationTokenSource();
                if (pageTimerAction == PageAction.Yesterday) {
                    _ = LoadYesterdayAsync(ctsLoad.Token);
                } else if (pageTimerAction == PageAction.Tomorrow) {
                    _ = LoadTomorrowAsync(ctsLoad.Token);
                } else if (pageTimerAction == PageAction.Target) {
                    _ = LoadFocusAsync(ctsLoad.Token);
                }
            };

            // 前者会在应用启动时触发多次，后者仅一次
            //this.SizeChanged += Current_SizeChanged;
            Window.Current.SizeChanged += Current_SizeChanged;

            Task.Run(async () => {
                ini = await IniUtil.GetIniAsync();
            }).Wait();
            InitProvider();
        }

        private async Task LoadFocusAsync(CancellationToken token) {
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
            if (!token.IsCancellationRequested && metaCache != null && metaCache.Id.Equals(meta.Id)) {
                await ShowImg(meta, token);
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
                await ShowImg(meta, token);
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
                await ShowImg(meta, token);
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
            if (meta == null) { // 跳转至最早
                meta = provider.Index(99999);
            }
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
                await ShowImg(meta, token);
            }
        }

        private async Task LoadTargetAsync(int index, CancellationToken token) {
            bool res = await provider.LoadData(token, ini.GetIni());
            if (token.IsCancellationRequested) {
                return;
            }
            if (!res) {
                LogUtil.E("LoadEndAsync() failed to load data");
                StatusError();
                return;
            }

            meta = provider.Index(index);
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
                await ShowImg(meta, token);
            }
        }

        private async Task Refresh() {
            ini = await IniUtil.GetIniAsync();
            InitProvider();
            ShowToastI(string.Format(resLoader.GetString("MsgRefresh"), resLoader.GetString("Provider_" + provider.Id)));

            ctsLoad.Cancel();
            StatusLoading();
            ctsLoad = new CancellationTokenSource();
            await LoadFocusAsync(ctsLoad.Token);
        }

        private void InitProvider() {
            provider = ini.GenerateProvider();

            MenuProviderLsp.Visibility = ini.R18 == 1 ? Visibility.Visible : Visibility.Collapsed;
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
            } else if (!ini.Provider.Equals(MenuProviderLsp.Tag)) {
                _ = RegServiceAsync();
                if (ini.DesktopProvider.Equals(ini.Provider) || ini.LockProvider.Equals(ini.Provider)) {
                    _ = RunServiceNowAsync(); // 用户浏览图源与推送图源一致，立即推送一次
                }
            }

            RadioMenuFlyoutItem item = FlyoutProvider.Items.Cast<RadioMenuFlyoutItem>().FirstOrDefault(c => ini.Provider.Equals(c?.Tag?.ToString()));
            if (item != null) {
                item.IsChecked = true;
            }
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
            // 版权所有 / 作者
            TextDetailCopyright.Text = meta.Copyright ?? "";
            TextDetailCopyright.Visibility = TextDetailCopyright.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 日期
            TextDetailDate.Text = meta.Date.Ticks > 0 ? meta.Date.ToLongDateString() : "";
            TextDetailDate.Visibility = TextDetailDate.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 文件属性（保持可见）
            TextDetailProperties.Text = resLoader.GetString("Provider_" + provider.Id)
                + (meta.Cate != null ? (" · " + meta.Cate) : "");
            TextDetailProperties.Visibility = Visibility.Visible;
        }

        private async Task ShowImg(Meta meta, CancellationToken token) {
            LogUtil.D("ShowImg() {0}", meta?.Id);
            if (meta == null) {
                return;
            }

            if (meta.CacheUhd != null) {
                // 显示与图片文件相关的信息
                string source = resLoader.GetString("Provider_" + provider.Id) + (meta.Cate != null ? (" · " + meta.Cate) : "");
                string fileSize = FileUtil.ConvertFileSize(File.Exists(meta.CacheUhd.Path)
                    ? new FileInfo(meta.CacheUhd.Path).Length : 0);
                TextDetailProperties.Text = string.Format("{0} / {1}x{2}, {3}",
                    source, meta.Dimen.Width, meta.Dimen.Height, fileSize);
                TextDetailProperties.Visibility = Visibility.Visible;
                // 根据人脸识别优化组件放置位置
                bool faceLeft = meta.ExistsFaceAndAllLeft();
                RelativePanel.SetAlignLeftWithPanel(ViewBarPointer, !faceLeft);
                RelativePanel.SetAlignRightWithPanel(ViewBarPointer, faceLeft);
                RelativePanel.SetAlignLeftWithPanel(Info, !faceLeft);
                RelativePanel.SetAlignRightWithPanel(Info, faceLeft);
                RelativePanel.SetAlignLeftWithPanel(AnchorGo, faceLeft);
                RelativePanel.SetAlignRightWithPanel(AnchorGo, !faceLeft);
                RelativePanel.SetAlignLeftWithPanel(AnchorCate, faceLeft);
                RelativePanel.SetAlignRightWithPanel(AnchorCate, !faceLeft);
            }

            // 等待图片消失动画完成，保持连贯
            await Task.Delay(Math.Max(0, 400 - (int)((DateTime.Now.Ticks - imgAnimStart) / 10000)));
            if (token.IsCancellationRequested) {
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
            if (ini.Provider.Equals(MenuProviderLsp.Tag) && !r18) {
                biUhd.UriSource = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
            } else if (meta.CacheUhd != null) {
                biUhd.UriSource = new Uri(meta.CacheUhd.Path, UriKind.Absolute);
            } else {
                biUhd.UriSource = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
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
            imgAnimStart = DateTime.Now.Ticks;
            
            ImgUhd.Opacity = 0;
            ImgUhd.Scale = new Vector3(1.014f, 1.014f, 1.014f);

            if (ProgressLoading.ShowPaused || ProgressLoading.ShowError) {
                ProgressLoading.ShowPaused = false;
                ProgressLoading.ShowError = false;
                ProgressLoading.Visibility = Visibility.Visible;
            }
        }

        private void StatusEnjoy() {
            if (imgLoadStart < imgAnimStart || ImgUhd.Opacity == 1) { // 下一波进行中或下一波提前结束
                return;
            }
            
            MenuSetDesktop.IsEnabled = true;
            MenuSetLock.IsEnabled = true;
            MenuSave.IsEnabled = true;
            MenuMark.IsEnabled = true;
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
            MenuMark.IsEnabled = false;
            MenuFillOn.IsEnabled = false;
            MenuFillOff.IsEnabled = false;

            string title = null;
            string msg = resLoader.GetString("MsgNoInternet");
            if (NetworkInterface.GetIsNetworkAvailable()) {
                title = resLoader.GetString("Provider_" + provider.Id);
                msg = resLoader.GetString("MsgLostProvider");
            }
            ShowToastE(msg, title, resLoader.GetString("ActionTry"), () => {
                ctsLoad.Cancel();
                StatusLoading();
                ctsLoad = new CancellationTokenSource();
                _ = LoadFocusAsync(ctsLoad.Token);
            });
        }

        private async Task SetWallpaperAsync(Meta meta, bool setDesktopOrLock) {
            if (meta?.CacheUhd == null) {
                return;
            }

            if (!UserProfilePersonalizationSettings.IsSupported()) {
                ShowToastE(resLoader.GetString("MsgWallpaper0"));
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
                        ShowToastS(resLoader.GetString("MsgSetDesktop1"));
                    } else {
                        ShowToastE(resLoader.GetString("MsgSetDesktop0"));
                    }
                } else {
                    StorageFile fileWallpaper = await meta.CacheUhd.CopyAsync(ApplicationData.Current.LocalFolder,
                        "lock", NameCollisionOption.ReplaceExisting);
                    bool wallpaperSet = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileWallpaper);
                    if (wallpaperSet) {
                        ShowToastS(resLoader.GetString("MsgSetLock1"));
                    } else {
                        ShowToastE(resLoader.GetString("MsgSetLock0"));
                    }
                }
            } catch (Exception e) {
                LogUtil.E("SetWallpaper() " + e.Message);
            }
        }

        private async Task DownloadAsync() {
            ShowToastI(resLoader.GetString("MsgSave"));
            StorageFile file = await provider.DownloadAsync(meta, resLoader.GetString("AppNameShort"),
                resLoader.GetString("Provider_" + provider.Id));
            if (file != null) {
                ShowToastS(resLoader.GetString("MsgSave1"), null, resLoader.GetString("ActionGo"), async () => {
                    CloseToast();
                    await FileUtil.LaunchFolderAsync(await KnownFolders.PicturesLibrary.CreateFolderAsync(resLoader.GetString("AppNameShort"),
                        CreationCollisionOption.OpenIfExists), file);
                });
            } else {
                ShowToastE(resLoader.GetString("MsgSave0"));
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

        private void ShowToast(InfoBarSeverity severity, string msg, string title, string action, BtnInfoLinkHandler handler) {
            if (string.IsNullOrEmpty(msg)) {
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

        private void ShowToastI(string msg, string title = null, string action = null, BtnInfoLinkHandler handler = null) {
            ShowToast(InfoBarSeverity.Informational, msg, title, action, handler);
        }

        private void ShowToastS(string msg, string title = null, string action = null, BtnInfoLinkHandler handler = null) {
            ShowToast(InfoBarSeverity.Success, msg, title, action, handler);
        }

        private void ShowToastW(string msg, string title = null, string action = null, BtnInfoLinkHandler handler = null) {
            ShowToast(InfoBarSeverity.Warning, msg, title, action, handler);
        }

        private void ShowToastE(string msg, string title = null, string action = null, BtnInfoLinkHandler handler = null) {
            ShowToast(InfoBarSeverity.Error, msg, title, action, handler);
        }

        private void CloseToast() {
            Info.IsOpen = false;
        }

        private void ToggleImgMode(bool fillOn) {
            ImgUhd.Stretch = fillOn ? Stretch.UniformToFill : Stretch.Uniform;
            MenuFillOn.Visibility = fillOn ? Visibility.Collapsed : Visibility.Visible;
            MenuFillOff.Visibility = fillOn ? Visibility.Visible : Visibility.Collapsed;

            ShowToastI(fillOn ? resLoader.GetString("MsgUniformToFill") : resLoader.GetString("MsgUniform"));
        }

        private async Task CheckLaunchAsync() {
            int actions = (int)(localSettings.Values["Actions"] ?? 0);
            localSettings.Values["Actions"] = ++actions;
            // 检查R18提示
            if (ini.Provider.Equals(MenuProviderLsp.Tag)) {
                ShowToastW(resLoader.GetString("MsgLsp"), resLoader.GetString("Provider_" + MenuProviderLsp.Tag),
                    resLoader.GetString("ActionContinue"), async () => {
                        r18 = true;
                        ctsLoad.Cancel();
                        StatusLoading();
                        ctsLoad = new CancellationTokenSource();
                        await LoadFocusAsync(ctsLoad.Token);
                    });
                return;
            } else {
                r18 = true;
            }
            // 检查菜单提示
            if (!localSettings.Values.ContainsKey("MenuLearned")) {
                await Task.Delay(1000);
                ShowToastI(resLoader.GetString("MsgWelcome"));
                return;
            }
            // 检查任务栏固定提示
            if (TaskbarManager.GetDefault().IsPinningAllowed && !await TaskbarManager.GetDefault().IsCurrentAppPinnedAsync()) {
                int times = (int)(localSettings.Values["CheckPinTimes"] ?? 0);
                localSettings.Values["CheckPinTimes"] = ++times;
                if (times == 2) {
                    await Task.Delay(1000);
                    ShowToastI(resLoader.GetString("MsgPin"), null, resLoader.GetString("ActionPin"), async () => {
                        await TaskbarManager.GetDefault().RequestPinCurrentAppAsync();
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
                    await Launcher.LaunchUriAsync(new Uri(resLoader.GetStringForUri(new Uri("ms-resource:///Resources/LinkReview/NavigateUri"))));
                } else { // 下次一定
                    localSettings.Values.Remove("Actions");
                }
                return;
            }
            // 检查更新
            release = await Api.CheckUpdateAsync();
            if (!string.IsNullOrEmpty(release.Url)) {
                await Task.Delay(1000);
                ShowToastI(resLoader.GetString("MsgUpdate"), null, resLoader.GetString("ActionGo"), async () => {
                    CloseToast();
                    await FileUtil.LaunchUriAsync(new Uri(release?.Url));
                });
            }
        }

        private void ShowFlyoutGo() {
            if (FlyoutGo.IsOpen) {
                FlyoutGo.Hide();
                return;
            }
            if (ini.GetIni().IsSequential()) {
                BoxGo.PlaceholderText = string.Format(resLoader.GetString("CurDate"),
                    meta != null && meta.Date.Ticks > 0 ? meta.Date.ToString("MMdd") : "MMdd");
            } else {
                BoxGo.PlaceholderText = string.Format(resLoader.GetString("CurIndex"), provider.GetIndexFocus());
            }
            BoxGo.Text = "";
            FlyoutGo.Placement = RelativePanel.GetAlignRightWithPanel(AnchorGo)
                ? FlyoutPlacementMode.LeftEdgeAlignedBottom : FlyoutPlacementMode.RightEdgeAlignedBottom;
            FlyoutBase.ShowAttachedFlyout(AnchorGo);
        }

        private async Task ShowFlyoutMarkCate() {
            if (FlyoutMarkCate.IsOpen) {
                FlyoutMarkCate.Hide();
                return;
            }
            BaseIni bi = ini.GetIni();
            if (bi.Cates.Count == 0) {
                bi.Cates = await Api.CateAsync(bi.GetCateApi());
            }
            if (bi.Cates.Count == 0) {
                return;
            }
            MenuFlyoutItemBase item1 = FlyoutMarkCate.Items[0];
            MenuFlyoutItemBase item2 = FlyoutMarkCate.Items[1];
            FlyoutMarkCate.Items.Clear();
            FlyoutMarkCate.Items.Add(item1);
            FlyoutMarkCate.Items.Add(item2);
            foreach (CateMeta cate in bi.Cates) {
                MenuFlyoutItem item = new MenuFlyoutItem {
                    Text = cate.Name,
                    Tag = cate.Id
                };
                item.Click += MenuMarkCate_Click;
                FlyoutMarkCate.Items.Add(item);
            }
            FlyoutMarkCate.Placement = RelativePanel.GetAlignRightWithPanel(AnchorCate)
                ? FlyoutPlacementMode.LeftEdgeAlignedBottom : FlyoutPlacementMode.RightEdgeAlignedBottom;
            FlyoutBase.ShowAttachedFlyout(AnchorCate);
        }

        private async Task<bool> RegServiceAsync() {
            BackgroundAccessStatus reqStatus = await BackgroundExecutionManager.RequestAccessAsync();
            LogUtil.D("RegService() RequestAccessAsync " + reqStatus);
            if (reqStatus != BackgroundAccessStatus.AlwaysAllowed
                && reqStatus != BackgroundAccessStatus.AllowedSubjectToSystemPolicy) {
                ShowToastE(resLoader.GetString("TitleErrPush"));
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
            await _AppTrigger.RequestAsync();
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
            pageTimerAction = PageAction.Yesterday;
            
            AnimeYesterday1.Begin();

            if (!localSettings.Values.ContainsKey("YesterdayLearned")) {
                localSettings.Values["YesterdayLearned"] = true;
                ShowToastI(resLoader.GetString("MsgYesterdayDesc"), resLoader.GetString("MsgYesterday"));
            }

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private async void MenuSetDesktop_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            if (ini.Provider.Equals(MenuProviderLsp.Tag)) {
                ShowToastW(resLoader.GetString("MsgLsp"), resLoader.GetString("Provider_" + MenuProviderLsp.Tag),
                    resLoader.GetString("ActionContinue"), async () => {
                    await SetWallpaperAsync(meta, true);
                    _ = Api.RankAsync(ini?.Provider, meta, "desktop");
                    localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
                });
            } else {
                await SetWallpaperAsync(meta, true);
                _ = Api.RankAsync(ini?.Provider, meta, "desktop");
                localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
            }
        }

        private async void MenuSetLock_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            if (ini.Provider.Equals(MenuProviderLsp.Tag)) {
                ShowToastW(resLoader.GetString("MsgLsp"), resLoader.GetString("Provider_" + MenuProviderLsp.Tag),
                    resLoader.GetString("ActionContinue"), async () => {
                        await SetWallpaperAsync(meta, false);
                    _ = Api.RankAsync(ini?.Provider, meta, "lock");
                    localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
                });
            } else {
                await SetWallpaperAsync(meta, false);
                _ = Api.RankAsync(ini?.Provider, meta, "lock");
                localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
            }
        }

        private void MenuFill_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ToggleImgMode(ImgUhd.Stretch != Stretch.UniformToFill);
        }

        private async void MenuSave_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            await DownloadAsync();
            _ = Api.RankAsync(ini?.Provider, meta, "save");
            localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
        }

        private void MenuMark_Click(object sender, RoutedEventArgs e) {
            markTimerMeta = meta;
            markTimerAction = (sender as MenuFlyoutItem).Tag as string;
            ShowToastS(string.Format(resLoader.GetString("MsgMarked"), (sender as MenuFlyoutItem).Text), null,
                resLoader.GetString("ActionUndo"), () => {
                CloseToast();
                _ = Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, null, true);
            });
            _ = Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction);
        }

        private void MenuMarkCate_Click(object sender, RoutedEventArgs e) {
            markTimerMeta = meta;
            markTimerAction = "cate";
            ShowToastS(string.Format(resLoader.GetString("MsgMarked"), (sender as MenuFlyoutItem).Text), null,
                resLoader.GetString("ActionUndo"), () => {
                    CloseToast();
                    _ = Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, null, true);
                });
            _ = Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, (sender as MenuFlyoutItem).Tag as string);
        }

        private void FlyoutMark_Closed(object sender, object e) {
            // 该子菜单隐藏时不会自动隐藏菜单，因此手动关联
            FlyoutMenu.Hide();
        }

        private async void MenuPush_Click(object sender, RoutedEventArgs e) {
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
                await IniUtil.SaveDesktopProviderAsync(ini.DesktopProvider);
            }
            if (MenuCurLockIcon.Visibility == Visibility.Collapsed) {
                ini.LockProvider = MenuPushLockIcon.Visibility == Visibility.Visible ? provider.Id : "";
                await IniUtil.SaveLockProviderAsync(ini.LockProvider);
            }

            if (string.IsNullOrEmpty(ini.DesktopProvider) && string.IsNullOrEmpty(ini.LockProvider)) {
                UnregService();
            } else {
                await RegServiceAsync();
                if ((MenuPushDesktop.Tag.Equals(menuCheck.Tag) && MenuPushDesktopIcon.Visibility == Visibility.Visible)
                    || (MenuPushLock.Tag.Equals(menuCheck.Tag) && MenuPushLockIcon.Visibility == Visibility.Visible)) {
                    await RunServiceNowAsync(); // 用户浏览图源与推送图源一致，立即推送一次
                }
            }

            if (MenuPushDesktop.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushDesktopIcon.Visibility == Visibility.Visible) {
                    ShowToastS(resLoader.GetString("MsgPushDesktopOn"));
                } else {
                    ShowToastW(resLoader.GetString("MsgPushDesktopOff"));
                }
            }
            if (MenuCurDesktop.Tag.Equals(menuCheck.Tag)) {
                ShowToastW(resLoader.GetString("MsgPushDesktopOff"));
            }
            if (MenuPushLock.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushLockIcon.Visibility == Visibility.Visible) {
                    ShowToastS(resLoader.GetString("MsgPushLockOn"));
                } else {
                    ShowToastW(resLoader.GetString("MsgPushLockOff"));
                }
            }
            if (MenuCurLock.Tag.Equals(menuCheck.Tag)) {
                ShowToastW(resLoader.GetString("MsgPushLockOff"));
            }
        }

        private async void MenuProvider_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ViewSplit.IsPaneOpen = false;

            string providerIdNew = ((RadioMenuFlyoutItem)sender).Tag.ToString();
            await IniUtil.SaveProviderAsync(providerIdNew);
            await Refresh();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            //ViewSettings.BeforePaneOpen(ini);
            ViewSplit.IsPaneOpen = true;
        }

        private void ViewSplit_PaneOpened(SplitView sender, object args) {
            ViewSettings.PaneOpened(ini);
        }

        private void ViewSplit_PaneClosed(SplitView sender, object args) {
            ViewSettings.PaneClosed();
        }

        private void ImgUhd_ImageOpened(object sender, RoutedEventArgs e) {
            LogUtil.D("ImgUhd_ImageOpened() {0}", meta?.Id);
            StatusEnjoy();
        }

        private void ImgUhd_ImageFailed(object sender, ExceptionRoutedEventArgs e) {
            LogUtil.E("ImgUhd_ImageFailed() " + meta?.Id);
            StatusEnjoy();
        }

        private async void TextDetailCopyright_Tapped(object sender, TappedRoutedEventArgs e) {
            if (!string.IsNullOrEmpty(meta?.Src)) {
                await FileUtil.LaunchUriAsync(new Uri(meta?.Src));
            }
        }

        private void TextDetailCopyright_PointerEntered(object sender, PointerRoutedEventArgs e) {
            if (!string.IsNullOrEmpty(meta?.Src) && !TextDetailCopyright.Text.EndsWith(" 🌐")) {
                TextDetailCopyright.Text += " 🌐";
            }
        }

        private void TextDetailCopyright_PointerExited(object sender, PointerRoutedEventArgs e) {
            if (TextDetailCopyright.Text.EndsWith(" 🌐")) {
                TextDetailCopyright.Text = TextDetailCopyright.Text.Replace(" 🌐", "");
            }
        }

        private void BtnInfoLink_Click(object sender, RoutedEventArgs e) {
            InfoLink?.Invoke();
        }

        private void ViewBarPointer_PointerEntered(object sender, PointerRoutedEventArgs e) {
            ProgressLoading.Visibility = Visibility.Visible;
            ViewStory.Visibility = Visibility.Visible;
            CloseToast();
        }

        private void ViewBarPointer_PointerExited(object sender, PointerRoutedEventArgs e) {
            if (ProgressLoading.ShowPaused && !ProgressLoading.ShowError) {
                ProgressLoading.Visibility = Visibility.Collapsed;
            }
            ViewStory.Visibility = Visibility.Collapsed;
            CloseToast();
        }

        private void ViewBarPointer_ContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            // 阻止在标题区弹出菜单
            args.Handled = true;
        }

        private async void BoxGo_KeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key != VirtualKey.Enter) {
                return;
            }
            FlyoutGo.Hide();
            if (ini.GetIni().IsSequential()) {
                DateTime? date = DateUtil.ParseDate(BoxGo.Text);
                if (date != null) {
                    ctsLoad.Cancel();
                    StatusLoading();
                    ctsLoad = new CancellationTokenSource();
                    await LoadTargetAsync(date.Value, ctsLoad.Token);
                }
            } else {
                if (int.TryParse(BoxGo.Text, out int index)) {
                    ctsLoad.Cancel();
                    StatusLoading();
                    ctsLoad = new CancellationTokenSource();
                    await LoadTargetAsync(index, ctsLoad.Token);
                }
            }
        }

        private void FlyoutMenu_Opened(object sender, object e) {
            localSettings.Values["MenuLearned"] = true;
            CloseToast();
        }

        private void ViewMain_Tapped(object sender, TappedRoutedEventArgs e) {
            ViewSplit.IsPaneOpen = false;
            CloseToast();
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
            pageTimerAction = e.Cumulative.Translation.X < -100 || e.Cumulative.Translation.Y < -100 ? PageAction.Yesterday : PageAction.Tomorrow;
            e.Handled = true;

            CloseToast();

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private void ViewMain_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
            pageTimerAction = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta > 0 ? PageAction.Yesterday : PageAction.Tomorrow;

            CloseToast();

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private async void KeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) {
            args.Handled = true;
            CloseToast();
            switch (sender.Key) {
                case VirtualKey.Left: // Left
                case VirtualKey.Up: // Up
                    pageTimerAction = PageAction.Yesterday;
                    ctsLoad.Cancel();
                    StatusLoading();
                    pageTimer.Stop();
                    pageTimer.Start();
                    break;
                case VirtualKey.Right: // Right
                case VirtualKey.Down: // Down
                    pageTimerAction = PageAction.Tomorrow;
                    ctsLoad.Cancel();
                    StatusLoading();
                    pageTimer.Stop();
                    pageTimer.Start();
                    break;
                case VirtualKey.F11: // F11
                case VirtualKey.Escape: // Escape
                case VirtualKey.Enter: // Enter
                    ToggleFullscreenMode();
                    break;
                case VirtualKey.Space: // Space
                    MenuFill_Click(null, null);
                    break;
                case VirtualKey.B: // Ctrl + B
                    MenuSetDesktop_Click(null, null);
                    break;
                case VirtualKey.L: // Ctrl + L
                    MenuSetLock_Click(null, null);
                    break;
                case VirtualKey.D: // Ctrl + D
                case VirtualKey.S: // Ctrl + S
                    MenuSave_Click(null, null);
                    break;
                case VirtualKey.C:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) { // Ctrl + C
                        if (TextUtil.Copy(meta?.CacheUhd)) {
                            ShowToastS(resLoader.GetString("MsgCopiedImg"));
                            _ = Api.RankAsync(ini?.Provider, meta, "copy");
                        }
                    } else { // Shift + Control + C
                        if (meta != null) {
                            TextUtil.Copy(JsonConvert.SerializeObject(meta, Formatting.Indented));
                            ShowToastS(resLoader.GetString("MsgCopiedMeta"));
                        }
                    }
                    break;
                case VirtualKey.F5: // F5
                case VirtualKey.R: // Ctrl + R
                    FlyoutMenu.Hide();
                    await Refresh();
                    break;
                case VirtualKey.F3: // F3
                case VirtualKey.F: // Ctrl + F
                case VirtualKey.G: // Ctrl + G
                    ShowFlyoutGo();
                    break;
                case VirtualKey.Number5: // Ctrl + 5
                    await ShowFlyoutMarkCate();
                    break;
                case VirtualKey.F10:
                    if (sender.Modifiers == VirtualKeyModifiers.Shift) { // Shift + F10
                        // TODO
                    } else { // F10
                        if (ViewSplit.IsPaneOpen) {
                            ViewSplit.IsPaneOpen = false;
                        } else {
                            MenuSettings_Click(null, null);
                        }
                    }
                    break;
                case VirtualKey.I: // Ctrl + I
                    await FileUtil.LaunchFileAsync(await IniUtil.GetIniPath());
                    break;
                case VirtualKey.O: // Ctrl + O
                    await FileUtil.LaunchFolderAsync(await KnownFolders.PicturesLibrary.CreateFolderAsync(resLoader.GetString("AppNameShort"),
                        CreationCollisionOption.OpenIfExists));
                    break;
                case VirtualKey.F12: // F12
                    await FileUtil.LaunchFolderAsync(await FileUtil.GetLogFolder());
                    break;
            }
        }

        private void AnimeYesterday1_Completed(object sender, object e) {
            AnimeYesterday2.Begin();
        }

        private void MenuSave_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeSave.Begin();
        }

        private void MenuMark_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeMark.Begin();
        }

        private void MenuFillOff_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeFillOff.Begin();
        }

        private void MenuSettings_PointerEntered(object sender, PointerRoutedEventArgs e) {
            AnimeSettings.Begin();
        }

        private void ViewSettings_SettingsChanged(object sender, SettingsEventArgs e) {
            if (e.ProviderChanged || e.ProviderConfigChanged) {
                _ = Refresh();
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
                //foreach (RadioMenuFlyoutItem item in FlyoutProvider.Items.Cast<RadioMenuFlyoutItem>()) {
                //    item.RequestedTheme = theme;
                //}
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
                    ShowToastS(resLoader.GetString("MsgContribute1"));
                } else {
                    ShowToastW(resLoader.GetString("MsgContribute0"));
                }
            }
        }
    }
}
