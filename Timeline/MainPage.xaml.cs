using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI.Shell;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Timeline {
    public delegate void BtnInfoHandler();

    public sealed partial class MainPage : Page {
        private event BtnInfoHandler InfoHandler;

        private readonly ResourceLoader resLoader;
        private readonly ApplicationDataContainer localSettings;
        private CancellationTokenSource ctsLoad = new CancellationTokenSource();

        private Ini ini;
        private BaseProvider provider;
        private Meta meta;
        private long imgAnimStart = DateTime.Now.Ticks;
        private long imgLoadStart = DateTime.Now.Ticks;
        private bool providerLspHintOn = true; // LSP图源提示
        private bool providerLspR22On = false; // LSP图源贤者模式开启
        private bool autoStoryPos = false; // TODO：自动调整图文区贴靠位置（需检测图像人脸位置）
        private Go go = new Go(null); // 加载参数

        private DispatcherTimer resizeTimer1;
        private DispatcherTimer resizeTimer2;
        private DispatcherTimer pageTimer;
        private PageAction pageTimerAction = PageAction.Focus;
        private Meta markTimerMeta = null;
        private string markTimerAction = null;
        private Exception exception;

        private const string BG_TASK_NAME = "PushTask";
        private const string BG_TASK_NAME_TIMER = "PushTaskTimer";
        private enum PageAction {
            Focus, Yesterday, Tomorrow
        }
        private enum LoadStatus {
            Empty, Error, NoInternet
        }

        public MainPage() {
            this.InitializeComponent();

            resLoader = ResourceLoader.GetForCurrentView();
            localSettings = ApplicationData.Current.LocalSettings;
            Init();
            _ = LaunchAsync();
            _ = CheckLaunchAsync();
        }

        private void Init() {
            // 启动时页面获得焦点，使快捷键一开始即可用
            this.IsTabStop = true;

            TextTitle.Text = resLoader.GetString("AppDesc");

            resizeTimer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1400) };
            resizeTimer1.Tick += (sender2, e2) => {
                resizeTimer1.Stop();
                ReDecodeImg();
            };
            resizeTimer2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            resizeTimer2.Tick += (sender2, e2) => {
                resizeTimer2.Stop();
                ReDecodeImg();
            };
            pageTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
            pageTimer.Tick += (sender2, e2) => {
                pageTimer.Stop();
                ctsLoad = new CancellationTokenSource();
                _ = LoadDataAsync(ctsLoad.Token, go, pageTimerAction);
            };

            // 前者会在应用启动时触发多次，后者仅一次
            // this.SizeChanged += Current_SizeChanged;
            // Window.Current.SizeChanged += Current_SizeChanged;
            // ViewMain.SizeChanged += ViewMain_SizeChanged;

            Task.Run(async () => {
                ini = await IniUtil.GetIniAsync();
            }).Wait();
            InitProvider();

            App.Current.UnhandledException += (s, e) => {
                e.Handled = true;
                exception = e.Exception;
                LogUtil.E(exception.ToString());
                ShowToastE(exception.Message, "UnhandledException", resLoader.GetString("ActionReport"), async () => {
                    await Api.CrashAsync(exception);
                });
            };
            TaskScheduler.UnobservedTaskException += (s, e) => {
                e.SetObserved();
                exception = e.Exception;
                LogUtil.E(exception.ToString());
                ShowToastE(exception.Message, "UnobservedTaskException", resLoader.GetString("ActionReport"), async () => {
                    await Api.CrashAsync(exception);
                });
            };
        }

        private async Task LaunchAsync() {
            // 上传统计
            Dictionary<string, int> dosage = await FileUtil.ReadDosage();
            await Api.StatsAsync(ini, dosage.GetValueOrDefault("all", 0), dosage.GetValueOrDefault(ini.Provider, 0));
            // 在缓存可能被使用之前清理缓存
            await FileUtil.ClearCache(ini);
            // 初始化任务栏右键菜单
            //await InitJumpList();
            // 确保推送服务一直运行
            await RegServiceAsync();
            // 开始加载数据
            await LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
        }

        private async Task CheckLaunchAsync() {
            // 检查R18提示
            int actions = (int)(localSettings.Values["Actions"] ?? 0);
            localSettings.Values["Actions"] = ++actions;
            if (ini.Provider.Equals(MenuProviderLsp.Tag)) {
                ShowToastW(resLoader.GetString("MsgLsp"), resLoader.GetString("Provider_" + MenuProviderLsp.Tag),
                    resLoader.GetString("ActionContinue"), async () => {
                        providerLspHintOn = false;
                        ctsLoad.Cancel();
                        StatusLoading();
                        ctsLoad = new CancellationTokenSource();
                        await LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
                    });
                return;
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
                } else if (action == ContentDialogResult.Secondary) {
                    localSettings.Values["ReqReview"] = true;
                } else { // 下次一定
                    localSettings.Values.Remove("Actions");
                }
                return;
            }
        }

        private async Task LoadDataAsync(CancellationToken token, Go go, PageAction action) {
            await FileUtil.WriteDosage();
            bool res = true;
            if (NetworkInterface.GetIsNetworkAvailable()) { // 加载
                if (action == PageAction.Focus) {
                    if (go.Index > 0) {
                        if (go.Index >= provider.GetCount() - 1) {
                            if (go.Index > provider.GetCount()) { // 立即加载
                                res = await provider.LoadData(token, ini.GetIni(), go);
                            } else { // 预加载
                                _ = provider.LoadData(token, ini.GetIni(), go);
                            }
                        }
                    } else { // 立即重新加载
                        provider.ClearMetas();
                        res = await provider.LoadData(token, ini.GetIni(), go);
                    }
                } else if (action == PageAction.Yesterday) {
                    if (provider.GetCountNext(meta) <= 2) {
                        if (provider.GetCountNext(meta) == 0) { // 立即加载
                            res = await provider.LoadData(token, ini.GetIni(), go);
                        } else { // 预加载
                            _ = provider.LoadData(token, ini.GetIni(), go);
                        }
                    }
                } else if (action == PageAction.Tomorrow) {
                    // 不加载
                }
            }
            if (token.IsCancellationRequested) {
                return;
            }
            if (action == PageAction.Focus) {
                meta = provider.GetMeta(go.Index - 1);
            } else if (action == PageAction.Yesterday) {
                meta = provider.GetNextMeta(meta);
            } else if (action == PageAction.Tomorrow) {
                meta = provider.GetPrevMeta(meta);
            } else {
                meta = null;
            }
            LogUtil.D("LoadDataAsync() " + meta);
            if (meta == null) {
                StatusError(res ? LoadStatus.Empty : (NetworkInterface.GetIsNetworkAvailable() ? LoadStatus.Error : LoadStatus.NoInternet));
                return;
            }
            ShowText(meta);
            Meta metaCache = await provider.CacheAsync(meta, autoStoryPos, token);
            if (token.IsCancellationRequested) {
                return;
            }
            if (!token.IsCancellationRequested && metaCache != null && metaCache.Id.Equals(meta.Id)) {
                await ShowImg(meta, token);
            }
        }

        private async Task Refresh(bool reloadIni) {
            if (reloadIni) {
                providerLspR22On = (ini.GetIni(LspIni.ID) as LspIni).R22; // 重置前保存状态
                ini = await IniUtil.GetIniAsync();
                (ini.GetIni(LspIni.ID) as LspIni).R22 = providerLspR22On; // 重置后恢复状态
            }
            InitProvider();
            if (ini.Provider.Equals(MenuProviderLsp.Tag) && providerLspHintOn) { // 防社死
                ShowToastW(resLoader.GetString("MsgLsp"), resLoader.GetString("Provider_" + MenuProviderLsp.Tag),
                    resLoader.GetString("ActionContinue"), async () => {
                        providerLspHintOn = false;
                        ctsLoad.Cancel();
                        StatusLoading();
                        ctsLoad = new CancellationTokenSource();
                        await LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
                    });
            } else {
                ShowToastI(string.Format(resLoader.GetString("MsgRefresh"), resLoader.GetString("Provider_" + provider.Id)));
            }
            go = new Go(null);

            ctsLoad.Cancel();
            StatusLoading();
            ctsLoad = new CancellationTokenSource();
            await LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
        }

        private void InitProvider() {
            provider = ini.GenerateProvider();

            // 图源为“本地图库”时禁用标记功能
            MenuMark.IsEnabled = !LocalIni.ID.Equals(ini.Provider);

            // 未启用“r18”且未强制启用“LSP”时禁用图源列表中的“LSP”
            MenuProviderLsp.Visibility = ini.R18 == 1 || MenuProviderLsp.Tag.Equals(ini.Provider)
                ? Visibility.Visible : Visibility.Collapsed;

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

            //RadioMenuFlyoutItem item = FlyoutProvider.Items.Cast<RadioMenuFlyoutItem>().FirstOrDefault(c => ini.Provider.Equals(c?.Tag?.ToString()));
            foreach (RadioMenuFlyoutItem item in FlyoutProvider.Items.Cast<RadioMenuFlyoutItem>()) {
                item.Text = String.Format("{0} - {1}", resLoader.GetString("Provider_" + item.Tag),
                    resLoader.GetString("Slogan_" + item.Tag));
                item.IsChecked = item.Tag.Equals(ini.Provider);
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
            // 图文故事
            TextDetailStory.Text = meta.Story ?? "";
            TextDetailStory.Visibility = TextDetailStory.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 版权所有 / 作者
            TextDetailCopyright.Text = meta.Copyright ?? "";
            TextDetailCopyright.Text += TextDetailCopyright.Text.Length > 0 && !string.IsNullOrEmpty(meta.Src) ? " 🌐" : "";
            TextDetailCopyright.Visibility = TextDetailCopyright.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 日期
            TextDetailDate.Text = meta.Date.Ticks > 0 ? meta.Date.ToLongDateString() : "";
            TextDetailDate.Visibility = TextDetailDate.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            // 文件属性（保持可见）
            TextDetailProperties.Text = resLoader.GetString("Provider_" + provider.Id)
                + (meta.Cate != null ? (" · " + meta.Cate) : "");
            TextDetailProperties.Visibility = Visibility.Visible;

            // 准备缩略图
            _ = PrepareThumb(meta.Thumb);
        }

        private async Task ShowImg(Meta meta, CancellationToken token) {
            LogUtil.D("ShowImg() {0}", meta?.Id);
            if (meta == null) {
                return;
            }

            if (meta.CacheUhd != null) {
                // 显示与图片文件相关的信息
                string source = resLoader.GetString("Provider_" + provider.Id) + (meta.Cate != null ? (" · " + meta.Cate) : "");
                string fileSize = FileUtil.ConvertFileSize((long)((await meta.CacheUhd.GetBasicPropertiesAsync()).Size));
                TextDetailProperties.Text = string.Format("{0} / {1}x{2}, {3}",
                    source, (int)meta.Dimen.Width, (int)meta.Dimen.Height, fileSize);
                TextDetailProperties.Visibility = Visibility.Visible;
                // 根据人脸识别优化组件放置位置
                if (autoStoryPos) {
                    AdjustStoryPos(meta.ExistsFaceAndAllLeft());
                }
            }

            // 等待图片消失动画完成，保持连贯
            await Task.Delay(Math.Max(0, 400 - (int)((DateTime.Now.Ticks - imgAnimStart) / 10000)));
            if (token.IsCancellationRequested) {
                return;
            }
            imgLoadStart = DateTime.Now.Ticks;

            // 显示图片
            BitmapImage biUhd = new BitmapImage();
            ImgUhd.Source = biUhd;
            ImgUhd.Tag = meta.Id;
            int[] resize = ImgUtil.Resize(ViewMain.ActualWidth, ViewMain.ActualHeight, meta.Dimen.Width, meta.Dimen.Height, ImgUhd.Stretch);
            biUhd.DecodePixelType = DecodePixelType.Logical; // 按逻辑像素
            biUhd.DecodePixelWidth = resize[0];
            biUhd.DecodePixelHeight = resize[1];
            LogUtil.D("ShowImg() {0}x{1}, view logical: {2}x{3}, scale logical: {4}x{5}",
                Math.Round(meta.Dimen.Width), Math.Round(meta.Dimen.Height),
                Math.Round(ViewMain.ActualWidth), Math.Round(ViewMain.ActualHeight), resize[0], resize[1]);
            if (ini.Provider.Equals(MenuProviderLsp.Tag) && providerLspHintOn) {
                biUhd.UriSource = new Uri("ms-appx:///Assets/Images/default.png", UriKind.RelativeOrAbsolute);
            } else if (meta.CacheUhd != null) {
                biUhd.UriSource = new Uri(meta.CacheUhd.Path, UriKind.RelativeOrAbsolute);
            } else {
                biUhd.UriSource = new Uri("ms-appx:///Assets/Images/default.png", UriKind.RelativeOrAbsolute);
            }
        }

        private void ReDecodeImg() {
            if (meta == null || meta.Dimen.Width == 0 || ImgUhd.Source == null || !ImgUhd.Tag.Equals(meta.Id)) {
                return;
            }
            BitmapImage bi = ImgUhd.Source as BitmapImage;
            if (bi.PixelHeight == 0) {
                LogUtil.D("ReDecodeImg() bi.PixelWidth 0");
                return;
            }
            bi.DecodePixelWidth = 0; // 先重置，否则可能出现图像闪烁
            bi.DecodePixelHeight = 0;
            int[] resize = ImgUtil.Resize(ViewMain.ActualWidth, ViewMain.ActualHeight, meta.Dimen.Width, meta.Dimen.Height, ImgUhd.Stretch);
            bi.DecodePixelWidth = resize[0];
            bi.DecodePixelHeight = resize[1];
            LogUtil.D("ReDecodeImg() {0}x{1}, view logical: {2}x{3}, scale logical: {4}x{5}",
                Math.Round(meta.Dimen.Width), Math.Round(meta.Dimen.Height),
                Math.Round(ViewMain.ActualWidth), Math.Round(ViewMain.ActualHeight), resize[0], resize[1]);
        }

        private void AdjustStoryPos(bool right) {
            RelativePanel.SetAlignLeftWithPanel(ViewBarPointer, !right);
            RelativePanel.SetAlignRightWithPanel(ViewBarPointer, right);
            // 调整顶部信息条，与图文块同侧
            RelativePanel.SetAlignLeftWithPanel(Info, !right);
            RelativePanel.SetAlignRightWithPanel(Info, right);
            // 调整底部跳转浮窗至对侧
            RelativePanel.SetAlignLeftWithPanel(AnchorGo, right);
            RelativePanel.SetAlignRightWithPanel(AnchorGo, !right);
            // 调整底部标记类别浮窗至对侧
            RelativePanel.SetAlignLeftWithPanel(AnchorCate, right);
            RelativePanel.SetAlignRightWithPanel(AnchorCate, !right);
            // 调整底部缩略图浮窗至对侧
            TipThumb.PreferredPlacement = right ? TeachingTipPlacementMode.BottomLeft : TeachingTipPlacementMode.BottomRight;
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

            MenuSetDesktop.IsEnabled = false;
            MenuSetLock.IsEnabled = false;
            MenuSave.IsEnabled = false;
            MenuFillOn.IsEnabled = false;
            MenuFillOff.IsEnabled = false;
        }

        private void StatusEnjoy() {
            if (imgLoadStart < imgAnimStart || ImgUhd.Opacity == 1) { // 下一波进行中或下一波提前结束
                return;
            }
            
            MenuSetDesktop.IsEnabled = true;
            MenuSetLock.IsEnabled = true;
            MenuSave.IsEnabled = !LocalIni.ID.Equals(ini.Provider); // 图源为“本地图库”时禁用收藏功能
            MenuFillOn.IsEnabled = true;
            MenuFillOff.IsEnabled = true;

            ImgUhd.Opacity = 1;
            ImgUhd.Scale = new Vector3(1, 1, 1);

            ProgressLoading.ShowPaused = true;
            ProgressLoading.ShowError = false;
            ProgressLoading.Visibility = ViewStory.Visibility;
        }

        private void StatusError(LoadStatus status) {
            ImgUhd.Opacity = 0;

            TextTitle.Text = resLoader.GetString("AppDesc");
            TextDetailCaption.Visibility = Visibility.Collapsed;
            TextDetailStory.Visibility = Visibility.Collapsed;
            TextDetailCopyright.Visibility = Visibility.Collapsed;
            TextDetailDate.Visibility = Visibility.Collapsed;
            TextDetailProperties.Visibility = Visibility.Collapsed;

            ProgressLoading.ShowError = true;
            ProgressLoading.Visibility = Visibility.Visible;

            MenuSetDesktop.IsEnabled = false;
            MenuSetLock.IsEnabled = false;
            MenuSave.IsEnabled = false;
            MenuFillOn.IsEnabled = false;
            MenuFillOff.IsEnabled = false;

            switch (status) {
                case LoadStatus.Empty:
                    ShowToastW(resLoader.GetString("MsgProviderEmpty"), resLoader.GetString("Provider_" + provider.Id),
                        resLoader.GetString("ActionTry"), () => {
                        ctsLoad.Cancel();
                        StatusLoading();
                        ctsLoad = new CancellationTokenSource();
                        _ = LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
                    });
                    break;
                case LoadStatus.Error:
                    ShowToastE(resLoader.GetString("MsgLostProvider"), resLoader.GetString("Provider_" + provider.Id),
                        resLoader.GetString("ActionTry"), () => {
                        ctsLoad.Cancel();
                        StatusLoading();
                        ctsLoad = new CancellationTokenSource();
                        _ = LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
                    });
                    break;
                case LoadStatus.NoInternet:
                    ShowToastE(resLoader.GetString("MsgNoInternet"), null, resLoader.GetString("ActionTry"), () => {
                        ctsLoad.Cancel();
                        StatusLoading();
                        ctsLoad = new CancellationTokenSource();
                        _ = LoadDataAsync(ctsLoad.Token, go, PageAction.Focus);
                    });
                    break;
            }
        }

        private async Task PrepareThumb(string urlThumb) {
            if (string.IsNullOrEmpty(urlThumb)) {
                ImgThumb.Source = null;
                return;
            }
            BitmapImage bi = new BitmapImage();
            Uri uri = new Uri(urlThumb);
            if (uri.IsFile) {
                bi.DecodePixelType = DecodePixelType.Logical; // 按逻辑像素
                bi.DecodePixelWidth = 320;
                bi.DecodePixelHeight = 0;
                StorageFile file = await StorageFile.GetFileFromPathAsync(urlThumb);
                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read)) {
                    await bi.SetSourceAsync(fileStream);
                }
            } else {
                bi.UriSource = uri;
            }
            ImgThumb.Source = bi;
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
                    StorageFile file = await meta.CacheUhd.CopyAsync(await FileUtil.GetWallpaperFolderAsync(),
                        string.Format("desktop-{0}.jpg", DateTime.Now.ToString("yyyyMMddHH00")), NameCollisionOption.ReplaceExisting);
                    bool wallpaperSet = await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(file);
                    if (wallpaperSet) {
                        ShowToastS(resLoader.GetString("MsgSetDesktop1"));
                    } else {
                        ShowToastE(resLoader.GetString("MsgSetDesktop0"));
                    }
                } else {
                    StorageFile file = await meta.CacheUhd.CopyAsync(await FileUtil.GetWallpaperFolderAsync(),
                        string.Format("lock-{0}.jpg", DateTime.Now.ToString("yyyyMMddHH00")), NameCollisionOption.ReplaceExisting);
                    bool wallpaperSet = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(file);
                    if (wallpaperSet) {
                        ShowToastS(resLoader.GetString("MsgSetLock1"));
                    } else {
                        ShowToastE(resLoader.GetString("MsgSetLock0"));
                    }
                }
            } catch (Exception e) {
                LogUtil.E("SetWallpaperAsync() " + e.Message);
            }
        }

        private async Task DownloadAsync() {
            ShowToastI(resLoader.GetString("MsgSave"));
            StorageFile file = await provider.DownloadAsync(meta, resLoader.GetString("Provider_" + provider.Id));
            if (file != null) {
                meta.Favorite = true;
                ShowToastS(resLoader.GetString("MsgSave1"), null, resLoader.GetString("ActionView"), async () => {
                    await FileUtil.LaunchFolderAsync(await KnownFolders.PicturesLibrary.CreateFolderAsync(AppInfo.Current.DisplayInfo.DisplayName,
                        CreationCollisionOption.OpenIfExists), file);
                });
            } else {
                ShowToastE(resLoader.GetString("MsgSave0"));
            }
        }

        private void ToggleFullscreenMode() {
            ToggleFullscreenMode(!ApplicationView.GetForCurrentView().IsFullScreenMode, false);
        }

        private void ToggleFullscreenMode(bool fullScreen, bool optimizeSize) {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (fullScreen) {
                if (view.TryEnterFullScreenMode()) {
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
                }
            } else {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                if (optimizeSize) {
                    Windows.Foundation.Size monitorLogic = SysUtil.GetMonitorPixels(true); // 显示器逻辑尺寸
                    Windows.Foundation.Size winLogic = Window.Current.Content.ActualSize.ToSize(); // 窗口逻辑尺寸
                    LogUtil.I("ToggleFullscreenMode() monitor logic: " + monitorLogic);
                    if (monitorLogic.Width > 0) {
                        if (monitorLogic.Width > monitorLogic.Height) { // 横屏
                            if ((winLogic.Width < winLogic.Height && Math.Abs(winLogic.Height / winLogic.Width - 1.6) >= 0.01) || Math.Abs(winLogic.Width / winLogic.Height - 1.6) < 0.01) { // 非完美竖窗或完美横窗
                                double h = monitorLogic.Height * 4 / 5; // 4/5高度
                                bool res = ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(h * 10 / 16, h)); // 10:16
                                LogUtil.I("ToggleFullscreenMode() " + res);
                            } else { // 竖窗
                                double h = monitorLogic.Height * 2 / 3; // 2/3高度
                                bool res = ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(h * 16 / 10, h)); // 16:10
                                LogUtil.I("ToggleFullscreenMode() " + res);
                            }
                        } else { // 竖屏
                            if ((winLogic.Width < winLogic.Height && Math.Abs(winLogic.Height / winLogic.Width - 1.6) >= 0.01) || Math.Abs(winLogic.Width / winLogic.Height - 1.6) < 0.01) { // 非完美竖窗或完美横窗
                                double w = monitorLogic.Width * 2 / 3; // 2/3宽度
                                bool res = ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(w, w * 16 / 10)); // 10:16
                                LogUtil.I("ToggleFullscreenMode() " + res);
                            } else { // 竖窗
                                double w = monitorLogic.Width * 4 / 5; // 4/5宽度
                                bool res = ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(w, w * 10 / 16)); // 16:10
                                LogUtil.I("ToggleFullscreenMode() " + res);
                            }
                        }
                    }
                }
            }
        }

        private void ShowToast(InfoBarSeverity severity, string msg, string title, string action, BtnInfoHandler handler) {
            if (string.IsNullOrEmpty(msg)) {
                return;
            }
            Info.Severity = severity;
            Info.Title = title ?? "";
            Info.Message = msg;
            InfoHandler = handler;
            BtnInfo.Content = action ?? resLoader.GetString("ActionGo");
            BtnInfo.Visibility = handler != null ? Visibility.Visible : Visibility.Collapsed;
            Info.IsOpen = true;
        }

        private void ShowToastI(string msg, string title = null, string action = null, BtnInfoHandler handler = null) {
            ShowToast(InfoBarSeverity.Informational, msg, title, action, handler);
        }

        private void ShowToastS(string msg, string title = null, string action = null, BtnInfoHandler handler = null) {
            ShowToast(InfoBarSeverity.Success, msg, title, action, handler);
        }

        private void ShowToastW(string msg, string title = null, string action = null, BtnInfoHandler handler = null) {
            ShowToast(InfoBarSeverity.Warning, msg, title, action, handler);
        }

        private void ShowToastE(string msg, string title = null, string action = null, BtnInfoHandler handler = null) {
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

        private void ShowFlyoutGo() {
            if (FlyoutGo.IsOpen) {
                FlyoutGo.Hide();
                return;
            }
            HideFlyouts();
            BoxGo.PlaceholderText = meta == null ? ""
                : Go.Generate(provider.GetCount(), provider.GetIndex(meta) + 1, meta.No, meta.Date, meta.Score);
            BoxGo.SelectAll();
            //BoxGo.Text = "";
            FlyoutGo.Placement = RelativePanel.GetAlignRightWithPanel(AnchorGo)
                ? FlyoutPlacementMode.LeftEdgeAlignedBottom : FlyoutPlacementMode.RightEdgeAlignedBottom;
            FlyoutBase.ShowAttachedFlyout(AnchorGo);
        }

        private async Task ShowFlyoutMarkCate() {
            if (FlyoutMarkCate.IsOpen) {
                FlyoutMarkCate.Hide();
                return;
            }
            HideFlyouts();
            BaseIni bi = ini.GetIni();
            if (bi.Cates.Count == 0) {
                List<CateMeta> cates = new List<CateMeta>();
                foreach (CateApiData item in await Api.CateAsync(bi.GetCateApi())) {
                    cates.Add(new CateMeta {
                        Id = item.Id,
                        Name = item.Name
                    });
                }
                bi.Cates = cates;
            }
            if (bi.Cates.Count == 0) {
                return;
            }
            MenuFlyoutItem item1 = FlyoutMarkCate.Items[0] as MenuFlyoutItem; // 标题行
            item1.Text = resLoader.GetString("MenuMarkCateHint");
            MenuFlyoutSeparator item2 = FlyoutMarkCate.Items[1] as MenuFlyoutSeparator; // 分隔线
            FlyoutMarkCate.Items.Clear();
            FlyoutMarkCate.Items.Add(item1);
            FlyoutMarkCate.Items.Add(item2);
            foreach (CateMeta cate in bi.Cates) {
                MenuFlyoutItem item = new MenuFlyoutItem {
                    Text = cate.Name,
                    Tag = cate.Id,
                    IsEnabled = string.IsNullOrEmpty(cate.Name) || !cate.Name.Equals(meta.Cate)
                };
                item.Click += MenuMarkCate_Click;
                FlyoutMarkCate.Items.Add(item);
            }
            FlyoutMarkCate.Placement = RelativePanel.GetAlignRightWithPanel(AnchorCate)
                ? FlyoutPlacementMode.LeftEdgeAlignedBottom : FlyoutPlacementMode.RightEdgeAlignedBottom;
            FlyoutBase.ShowAttachedFlyout(AnchorCate);
        }

        private async Task ShowFlyoutMarkTag() {
            if (FlyoutMarkCate.IsOpen) {
                FlyoutMarkCate.Hide();
                return;
            }
            HideFlyouts();
            BaseIni bi = ini.GetIni();
            if (bi.Tags.Count == 0) {
                foreach (CateApiData item in await Api.CateAsync(bi.GetCateApi())) {
                    if (item.Id.Equals(bi.Cate) && !string.IsNullOrEmpty(item.Tag)) {
                        bi.Tags = item.Tag.Split(",").ToList();
                        break;
                    }
                }
            }
            if (bi.Tags.Count == 0) {
                return;
            }
            MenuFlyoutItem item1 = FlyoutMarkCate.Items[0] as MenuFlyoutItem; // 标题行
            item1.Text = resLoader.GetString("MenuMarkTagHint");
            MenuFlyoutSeparator item2 = FlyoutMarkCate.Items[1] as MenuFlyoutSeparator; // 分隔线
            FlyoutMarkCate.Items.Clear();
            FlyoutMarkCate.Items.Add(item1);
            FlyoutMarkCate.Items.Add(item2);
            foreach (string tag in bi.Tags) {
                MenuFlyoutItem item = new MenuFlyoutItem {
                    Text = tag,
                    Tag = tag,
                    IsEnabled = true
                };
                item.Click += MenuMarkTag_Click;
                FlyoutMarkCate.Items.Add(item);
            }
            FlyoutMarkCate.Placement = RelativePanel.GetAlignRightWithPanel(AnchorCate)
                ? FlyoutPlacementMode.LeftEdgeAlignedBottom : FlyoutPlacementMode.RightEdgeAlignedBottom;
            FlyoutBase.ShowAttachedFlyout(AnchorCate);
        }

        private void ShowThumbAsync() {
            if (TipThumb.IsOpen) {
                TipThumb.IsOpen = false;
                return;
            }
            HideFlyouts();
            TipThumb.IsOpen = true;
        }

        private void HideFlyouts() {
            CloseToast();
            if (FlyoutGo.IsOpen) {
                FlyoutGo.Hide();
            }
            if (FlyoutMarkCate.IsOpen) {
                FlyoutMarkCate.Hide();
            }
            TipThumb.IsOpen = false;
        }

        private async Task<bool> RegServiceAsync() {
            if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(BG_TASK_NAME_TIMER))) {
                LogUtil.W("RegServiceAsync() exists True");
                return true;
            }
            BackgroundAccessStatus reqStatus = await BackgroundExecutionManager.RequestAccessAsync();
            LogUtil.D("RegServiceAsync() RequestAccessAsync " + reqStatus);
            if (reqStatus != BackgroundAccessStatus.AlwaysAllowed
                && reqStatus != BackgroundAccessStatus.AllowedSubjectToSystemPolicy) {
                ShowToastE(resLoader.GetString("MsgErrService"));
                return false;
            }
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder {
                Name = BG_TASK_NAME_TIMER,
                TaskEntryPoint = typeof(PushService).FullName
            };
            // 触发任务的事件
            builder.SetTrigger(new TimeTrigger(15, false)); // 周期执行（不低于15min）
            // 触发任务的先决条件
            builder.AddCondition(new SystemCondition(SystemConditionType.SessionConnected)); // Internet 必须连接
            _ = builder.Register();
            return true;
        }

        //private void UnregService() {
        //    foreach (var ta in BackgroundTaskRegistration.AllTasks) {
        //        if (ta.Value.Name == BG_TASK_NAME_TIMER) {
        //            ta.Value.Unregister(true);
        //            LogUtil.D("UnregService() service BG_TASK_NAME_TIMER unregistered");
        //        } else if (ta.Value.Name == BG_TASK_NAME) {
        //            ta.Value.Unregister(true);
        //            LogUtil.D("UnregService() service BG_TASK_NAME unregistered");
        //        }
        //    }
        //}

        private async Task RunServiceNowAsync() {
            ApplicationTrigger trigger = null;
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == BG_TASK_NAME) { // 已注册
                    trigger = (task.Value as BackgroundTaskRegistration).Trigger as ApplicationTrigger;
                    break;
                }
            }
            LogUtil.I("RunServiceNowAsync() exists " + (trigger != null));
            if (trigger == null) { // 后台任务从未注册过
                trigger = new ApplicationTrigger();

                BackgroundTaskBuilder builder = new BackgroundTaskBuilder {
                    Name = BG_TASK_NAME,
                    TaskEntryPoint = typeof(PushService).FullName
                };
                builder.SetTrigger(trigger);
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                _ = builder.Register();
            }
            await trigger.RequestAsync();
        }

        //private async Task InitJumpList() {
        //    if (!JumpList.IsSupported()) {
        //        return;
        //    }
        //    JumpList list = await JumpList.LoadCurrentAsync();
        //    if (list.Items.Count > 0) { // 已初始化
        //        return;
        //    }
        //    //list.SystemGroupKind = JumpListSystemGroupKind.None;
        //    //Debug.WriteLine(list.Items.Count);
        //    //list.Items.Clear();
        //    JumpListItem itemPushDesktop = JumpListItem.CreateWithArguments("pushdesktop", resLoader.GetString("JumpPushDesktop"));
        //    itemPushDesktop.Logo = new Uri("ms-appx://Assets/Icons/Square44x44Logo.png");
        //    //itemPushDesktop.GroupName = "push";
        //    list.Items.Add(itemPushDesktop);
        //    JumpListItem itemPushLock = JumpListItem.CreateWithArguments("pushlock", resLoader.GetString("JumpPushLock"));
        //    //itemPushLock.GroupName = "push";
        //    list.Items.Add(itemPushLock);
        //    await list.SaveAsync();
        //    LogUtil.I("InitJumpList() done");
        //}

        private void SwitchProvider(bool forward) {
            for (int i = 0; i < FlyoutProvider.Items.Count; i++) {
                if ((FlyoutProvider.Items[i] as RadioMenuFlyoutItem).IsChecked) {
                    int next;
                    do {
                        next = forward ? (++i % FlyoutProvider.Items.Count)
                            : (FlyoutProvider.Items.Count + --i) % FlyoutProvider.Items.Count;
                    } while (!FlyoutProvider.Items[next].IsEnabled || FlyoutProvider.Items[next].Visibility == Visibility.Collapsed);
                    MenuProvider_Click(FlyoutProvider.Items[next], null);
                    break;
                }
            }
        }

        private async Task Mark(string action, string desc) {
            markTimerMeta = meta;
            markTimerAction = action;
            await Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction);
            ShowToastS(string.Format(resLoader.GetString("MsgMarked"), desc), null, resLoader.GetString("ActionUndo"), async () => {
                await Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, null, true);
            });
        }

        //protected override void OnNavigatedTo(NavigationEventArgs e) {
        //    base.OnNavigatedTo(e);

        //    // TODO
        //}

        private void MenuYesterday_Click(object sender, RoutedEventArgs e) {
            pageTimerAction = PageAction.Yesterday;
            
            AnimeYesterday1.Begin();

            if (!localSettings.Values.ContainsKey("YesterdayLearned")) {
                localSettings.Values["YesterdayLearned"] = true;
                ShowToastI(resLoader.GetString("MsgWelcome"), resLoader.GetString("MsgYesterday"));
            }

            ctsLoad.Cancel();
            StatusLoading();
            pageTimer.Stop();
            pageTimer.Start();
        }

        private async void MenuSetDesktop_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            if (ini.Provider.Equals(MenuProviderLsp.Tag)) { // 防社死
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
            if (ini.Provider.Equals(MenuProviderLsp.Tag)) { // 防社死
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
            resizeTimer2.Stop();
            resizeTimer2.Start();
        }

        private async void MenuSave_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            await DownloadAsync();
            _ = Api.RankAsync(ini?.Provider, meta, "save");
            localSettings.Values["Actions"] = (int)(localSettings.Values["Actions"] ?? 0) + 1;
        }

        private async void MenuMark_Click(object sender, RoutedEventArgs e) {
            await Mark((sender as MenuFlyoutItem).Tag as string, (sender as MenuFlyoutItem).Text);
        }

        private async void MenuMarkCate_Click(object sender, RoutedEventArgs e) {
            markTimerMeta = meta;
            markTimerAction = "cate";
            ShowToastS(string.Format(resLoader.GetString("MsgMarked"), (sender as MenuFlyoutItem).Text), null,
                resLoader.GetString("ActionUndo"), async () => {
                    await Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, null, true);
                });
            await Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, (sender as MenuFlyoutItem).Tag as string);
        }

        private async void MenuMarkTag_Click(object sender, RoutedEventArgs e) {
            markTimerMeta = meta;
            markTimerAction = "tag";
            ShowToastS(string.Format(resLoader.GetString("MsgMarked"), (sender as MenuFlyoutItem).Text), null,
                resLoader.GetString("ActionUndo"), async () => {
                    await Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, null, true);
                });
            await Api.RankAsync(ini?.Provider, markTimerMeta, markTimerAction, (sender as MenuFlyoutItem).Tag as string);
        }

        private void FlyoutMark_Closed(object sender, object e) {
            // 该子菜单隐藏时不会自动隐藏菜单，因此手动关联
            FlyoutMenu.Hide();
        }

        private async void MenuPush_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ViewSplit.IsPaneOpen = false;
            // 刷新推送菜单项
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
            // 保存推送设置
            if (MenuCurDesktopIcon.Visibility == Visibility.Collapsed) {
                ini.DesktopProvider = MenuPushDesktopIcon.Visibility == Visibility.Visible ? provider.Id : "";
                await IniUtil.SaveDesktopProviderAsync(ini.DesktopProvider);
            }
            if (MenuCurLockIcon.Visibility == Visibility.Collapsed) {
                ini.LockProvider = MenuPushLockIcon.Visibility == Visibility.Visible ? provider.Id : "";
                await IniUtil.SaveLockProviderAsync(ini.LockProvider);
            }
            // 立即推送一次
            if ((MenuPushDesktop.Tag.Equals(menuCheck.Tag) && MenuPushDesktopIcon.Visibility == Visibility.Visible)
                || (MenuPushLock.Tag.Equals(menuCheck.Tag) && MenuPushLockIcon.Visibility == Visibility.Visible)) {
                await RunServiceNowAsync();
            }
            // 显示推送状态
            if (MenuPushDesktop.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushDesktopIcon.Visibility == Visibility.Visible) {
                    ShowToastS(resLoader.GetString("MsgPushDesktopOn"));
                } else {
                    ShowToastW(resLoader.GetString("MsgPushDesktopOff"));
                }
            } else if (MenuCurDesktop.Tag.Equals(menuCheck.Tag)) {
                ShowToastW(resLoader.GetString("MsgPushDesktopOff"));
            } else if (MenuPushLock.Tag.Equals(menuCheck.Tag)) {
                if (MenuPushLockIcon.Visibility == Visibility.Visible) {
                    ShowToastS(resLoader.GetString("MsgPushLockOn"));
                } else {
                    ShowToastW(resLoader.GetString("MsgPushLockOff"));
                }
            } else if (MenuCurLock.Tag.Equals(menuCheck.Tag)) {
                ShowToastW(resLoader.GetString("MsgPushLockOff"));
            }
        }

        private async void MenuProvider_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            ViewSplit.IsPaneOpen = false;

            ini.Provider = (sender as RadioMenuFlyoutItem).Tag.ToString();
            await IniUtil.SaveProviderAsync(ini.Provider);
            await Refresh(false);
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) {
            FlyoutMenu.Hide();
            //ViewSettings.BeforePaneOpen(ini);
            ViewSplit.IsPaneOpen = true;
        }

        private void ViewSplit_PaneOpened(SplitView sender, object args) {
            ViewSettings.NotifyPaneOpened(ini);
        }

        private void ImgUhd_ImageOpened(object sender, RoutedEventArgs e) {
            LogUtil.D("ImgUhd_ImageOpened() " + meta?.Id);
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

        private void BtnInfo_Click(object sender, RoutedEventArgs e) {
            InfoHandler?.Invoke();
        }

        private void ViewBarPointer_PointerEntered(object sender, PointerRoutedEventArgs e) {
            ProgressLoading.Visibility = Visibility.Visible;
            ViewStory.Visibility = Visibility.Visible;
            //CloseToast();
        }

        private void ViewBarPointer_PointerExited(object sender, PointerRoutedEventArgs e) {
            if (ProgressLoading.ShowPaused && !ProgressLoading.ShowError) {
                ProgressLoading.Visibility = Visibility.Collapsed;
            }
            ViewStory.Visibility = Visibility.Collapsed;
            //CloseToast();
        }

        private void ViewBarPointer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) {
            e.Handled = true;
            //ViewBarPointer.PointerEntered -= ViewBarPointer_PointerEntered;
            ViewBarPointer.PointerExited -= ViewBarPointer_PointerExited;
        }

        private void ViewBarPointer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
            e.Handled = true;
            // 响应拖动，暗示可调整图文区位置
            if (!(ViewBarPointer.RenderTransform is TranslateTransform transfrom)) {
                transfrom = new TranslateTransform();
                ViewBarPointer.RenderTransform = transfrom;
            }
            double threshold = 12;
            double factor = 4;
            if (transfrom.X + e.Delta.Translation.X / factor > threshold) {
                transfrom.X = threshold;
            } else if (transfrom.X + e.Delta.Translation.X / factor < -threshold) {
                transfrom.X = -threshold;
            } else {
                transfrom.X += e.Delta.Translation.X / factor;
            }
        }

        private void ViewBarPointer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            e.Handled = true;
            // 响应UI复位
            //ViewBarPointer.PointerEntered += ViewBarPointer_PointerEntered;
            ViewBarPointer.PointerExited += ViewBarPointer_PointerExited;
            if (ViewBarPointer.RenderTransform is TranslateTransform transfrom) {
                transfrom.X = 0;
            }
            // 调整组件放置位置
            if (Math.Abs(e.Cumulative.Translation.X) > 100) {
                autoStoryPos = false; // 关闭自动调位（同时不再检测图像人脸位置）
                AdjustStoryPos(e.Cumulative.Translation.X > 100);
            }
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
            go = Go.Parse(BoxGo.Text.Trim());
            LogUtil.I("BoxGo_KeyDown() " + go);
            pageTimerAction = PageAction.Focus;
            ctsLoad.Cancel();
            StatusLoading();
            ctsLoad = new CancellationTokenSource();
            await LoadDataAsync(ctsLoad.Token, go, pageTimerAction);
        }

        private void BoxGo_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args) {
            sender.Tag = sender.Text;
        }

        private void BoxGo_TextChanged(object sender, TextChangedEventArgs e) {
            if (BoxGo.Tag == null || BoxGo.Text.Length <= BoxGo.Tag.ToString().Length) {
                return;
            }
            // 输入引号时补全引号
            if (BoxGo.Text.EndsWith("\"") && !BoxGo.Tag.ToString().EndsWith("\"")) {
                BoxGo.Text += "\"";
                BoxGo.SelectionStart = BoxGo.Text.Length - 1;
            } else if (BoxGo.Text.EndsWith("'") && !BoxGo.Tag.ToString().EndsWith("'")) {
                BoxGo.Text += "'";
                BoxGo.SelectionStart = BoxGo.Text.Length - 1;
            }
        }

        private async void LinkGoDesc_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args) {
            await FileUtil.LaunchFileAsync(await FileUtil.GetDocGoFile());
        }

        private void FlyoutMenu_Opened(object sender, object e) {
            IconSave.Glyph = meta != null && meta.Favorite ? "\uE735" : "\uE734";
            localSettings.Values["MenuLearned"] = true;
            CloseToast();
        }

        private void ViewMain_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (ImgUhd.Source != null) { // 避免图片首次加载之前启动
                resizeTimer1.Stop();
                resizeTimer2.Start();
            }
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

        private void ViewMain_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
            // 响应滑动，暗示可滑动翻页
            if (!(ImgUhdPointer.RenderTransform is TranslateTransform transfrom)) {
                transfrom = new TranslateTransform();
                ImgUhdPointer.RenderTransform = transfrom;
            }
            double threshold = 12;
            double factor = 4;
            if (transfrom.X + e.Delta.Translation.X / factor > threshold) {
                transfrom.X = threshold;
            } else if (transfrom.X + e.Delta.Translation.X / factor < -threshold) {
                transfrom.X = -threshold;
            } else {
                transfrom.X += e.Delta.Translation.X / factor;
            }
        }

        private void ViewMain_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            //e.Handled = true;
            // 响应UI复位
            if (ImgUhdPointer.RenderTransform is TranslateTransform transfrom) {
                transfrom.X = 0;
                transfrom.Y = 0;
            }
            // 翻页
            if (Math.Abs(e.Cumulative.Translation.X) > 100) {
                pageTimerAction = e.Cumulative.Translation.X < -100 ? PageAction.Yesterday : PageAction.Tomorrow;

                CloseToast();

                ctsLoad.Cancel();
                StatusLoading();
                pageTimer.Stop();
                pageTimer.Start();
            }
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
                case VirtualKey.F1: // F1
                    //await FileUtil.LaunchUriAsync(new Uri(resLoader.GetString("LinkOpenSource/NavigateUri")));
                    await FileUtil.LaunchFileAsync(await FileUtil.GetDocShortcutFile());
                    break;
                case VirtualKey.F3: // F3
                case VirtualKey.F: // Ctrl + F
                case VirtualKey.G: // Ctrl + G
                    ShowFlyoutGo();
                    break;
                case VirtualKey.F4: // Ctrl + F4
                case VirtualKey.W: // Ctrl + W
                    Application.Current.Exit();
                    break;
                case VirtualKey.F5: // F5
                case VirtualKey.R: // Ctrl + R
                    FlyoutMenu.Hide();
                    await Refresh(true);
                    break;
                case VirtualKey.F8: // F8
                    ToggleFullscreenMode(false, true);
                    break;
                case VirtualKey.F10: // F10
                    if (ViewSplit.IsPaneOpen) {
                        ViewSplit.IsPaneOpen = false;
                    } else {
                        MenuSettings_Click(null, null);
                    }
                    break;
                case VirtualKey.F11: // F11
                case VirtualKey.Escape: // Escape
                case VirtualKey.Enter: // Enter
                    ToggleFullscreenMode();
                    break;
                case VirtualKey.F12: // F12
                    await FileUtil.LaunchFolderAsync(await FileUtil.GetLogFolderAsync());
                    break;
                case VirtualKey.Number6: // Ctrl + 6
                    await Mark("audited", resLoader.GetString("MarkAudited"));
                    break;
                case VirtualKey.Number7: // Ctrl + 7
                    await Mark("journal", resLoader.GetString("MarkJournal"));
                    break;
                case VirtualKey.Number9: // Ctrl + 9
                    await ShowFlyoutMarkTag();
                    break;
                case VirtualKey.Number0: // 0 / Ctrl + 0
                    await ShowFlyoutMarkCate();
                    break;
                case VirtualKey.Tab:
                    if (sender.Modifiers == VirtualKeyModifiers.Control) { // Ctrl + Tab
                        SwitchProvider(true); // 切换下个图源
                    } else { // Shift + Ctrl + Tab
                        SwitchProvider(false); // 切换上个图源
                    }
                    break;
                case VirtualKey.T: // Ctrl + T
                    ShowThumbAsync();
                    break;
                case VirtualKey.I: // Ctrl + I
                    await FileUtil.LaunchFileAsync(await IniUtil.GetIniPath());
                    break;
                case VirtualKey.O: // Ctrl + O
                    await FileUtil.LaunchFolderAsync(await KnownFolders.PicturesLibrary.CreateFolderAsync(AppInfo.Current.DisplayInfo.DisplayName,
                        CreationCollisionOption.OpenIfExists));
                    break;
                case VirtualKey.S: // Ctrl + S
                case VirtualKey.D: // Ctrl + D
                    MenuSave_Click(null, null);
                    break;
                case VirtualKey.L: // Ctrl + L
                    MenuSetLock_Click(null, null);
                    break;
                case VirtualKey.C:
                    if ((sender.Modifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift) { // Shift + Ctrl + C
                        if (meta != null) {
                            TextUtil.Copy(JsonConvert.SerializeObject(meta, Formatting.Indented));
                            ShowToastS(resLoader.GetString("MsgCopiedMeta"));
                        }
                    } else if ((sender.Modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu) { // Alt + Control + C
                        if (meta != null) {
                            TextUtil.Copy(meta.Uhd);
                            ShowToastS(resLoader.GetString("MsgCopiedUrl"));
                        }
                    } else { // Control + C
                        if (TextUtil.Copy(meta?.CacheUhd)) {
                            ShowToastS(resLoader.GetString("MsgCopiedImg"));
                            _ = Api.RankAsync(ini?.Provider, meta, "copy");
                        }
                    }
                    break;
                case VirtualKey.B: // Ctrl + B
                    MenuSetDesktop_Click(null, null);
                    break;
                case VirtualKey.Space: // Space
                    MenuFill_Click(null, null);
                    break;
                case VirtualKey.Application: // Application
                    // TODO
                    break;
                case VirtualKey.Left: // Left
                case VirtualKey.Up: // Up
                    if (FlyoutMarkCate.IsOpen) { // 避免误操作
                        break;
                    }
                    pageTimerAction = PageAction.Yesterday;
                    ctsLoad.Cancel();
                    StatusLoading();
                    pageTimer.Stop();
                    pageTimer.Start();
                    break;
                case VirtualKey.Right: // Right
                case VirtualKey.Down: // Down
                    if (FlyoutMarkCate.IsOpen) { // 避免误操作
                        break;
                    }
                    pageTimerAction = PageAction.Tomorrow;
                    ctsLoad.Cancel();
                    StatusLoading();
                    pageTimer.Stop();
                    pageTimer.Start();
                    break;
            }
        }

        private async void ViewSettings_SettingsChanged(object sender, SettingsEventArgs e) {
            //if (e.ProviderChanged) {
            //    await Refresh(false);
            //} else if (e.ProviderConfigChanged) {
            //    await Refresh(false);
            //    await ViewSettings.NotifyPaneOpened(ini);
            //}
            if (e.VersionChanged != null) {
                ShowToastI(resLoader.GetString("MsgUpdate"), null, resLoader.GetString("ActionGo"), async () => {
                    await FileUtil.LaunchUriAsync(new Uri(e.VersionChanged.Url));
                });
            }
            if (e.ProviderChanged || e.ProviderConfigChanged) {
                await Refresh(false);
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

        private async void ViewSettings_DlgChanged(object sender, DlgEventArgs e) {
            if (e.TimelineContributeChanged) {
                ContributeDlg dlg = new ContributeDlg {
                    RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
                };
                var res = await dlg.ShowAsync();
                if (res == ContentDialogResult.Primary) {
                    ShowToastI(resLoader.GetString("MsgContribute"));
                }
            } else if (e.LspR22Changed != null) {
                R22Dlg dlg = new R22Dlg(e.LspR22Changed.Comment, e.LspR22Changed.Remark) {
                    RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
                };
                var res = await dlg.ShowAsync();
                if (res == ContentDialogResult.Primary) {
                    ShowToastI(resLoader.GetString("MsgR22Auth"));
                }
            }
        }
    }
}
