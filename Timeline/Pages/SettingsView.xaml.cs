﻿using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timeline.Beans;
using Timeline.Providers;
using Timeline.Utils;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace Timeline.Pages {
    public sealed partial class SettingsView : UserControl {
        public event EventHandler<SettingsEventArgs> SettingsChanged;
        public event EventHandler<DlgEventArgs> DlgChanged;

        private Ini ini = new Ini();

        private readonly ResourceLoader resLoader;

        //Dictionary<string, FontIcon> dicPushDesktop;
        //Dictionary<string, FontIcon> dicPushLock;

        ObservableCollection<CateMeta> listBingLang = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listNasaOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listOneplusOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listTimelineCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listTimelineOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listOneOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listYmyouliCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listYmyouliOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listQingbzCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listQingbzOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhavenCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhavenOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhereCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhereOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallpaperupCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallpaperupOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listToopicCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listToopicOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listInfinityOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listGluttonAlbum = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listGluttonOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listLspCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listLspOrder = new ObservableCollection<CateMeta>();

        private ReleaseApiData release = null;

        private DispatcherTimer himawari8OffsetTimer = null;
        private DispatcherTimer himawari8RatioTimer = null;

        public SettingsView() {
            this.InitializeComponent();

            resLoader = ResourceLoader.GetForCurrentView();
            Init();
        }

        private void Init() {
            TextApp.Text = resLoader.GetString("AppName") + " " + SysUtil.GetPkgVer(false);

            foreach (string item in BingIni.LANGS) {
                listBingLang.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("BingLang_" + item)
                });
            }
            foreach (string item in NasaIni.ORDERS) {
                listNasaOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in OneplusIni.ORDERS) {
                listOneplusOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in TimelineIni.ORDERS) {
                listTimelineOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in OneIni.ORDERS) {
                listOneOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in YmyouliIni.ORDERS) {
                listYmyouliOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in QingbzIni.ORDERS) {
                listQingbzOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in WallhavenIni.ORDERS) {
                listWallhavenOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in WallhereIni.ORDERS) {
                listWallhereOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in WallpaperupIni.ORDERS) {
                listWallpaperupOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in ToopicIni.ORDERS) {
                listToopicOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in InfinityIni.ORDERS) {
                listInfinityOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in GluttonIni.ALBUMS) {
                listGluttonAlbum.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Album_" + item)
                });
            }
            foreach (string item in GluttonIni.ORDERS) {
                listGluttonOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }
            foreach (string item in LspIni.ORDERS) {
                listLspOrder.Add(new CateMeta {
                    Id = item,
                    Name = resLoader.GetString("Order_" + item)
                });
            }

            BoxHimawari8Offset.NumberFormatter = new DecimalFormatter {
                IntegerDigits = 1,
                FractionDigits = 2,
                NumberRounder = new IncrementNumberRounder {
                    Increment = 0.01,
                    RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp
                }
            };
            BoxHimawari8Ratio.NumberFormatter = new DecimalFormatter {
                IntegerDigits = 1,
                FractionDigits = 2,
                NumberRounder = new IncrementNumberRounder {
                    Increment = 0.01,
                    RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp
                }
            };
        }

        public void NotifyPaneOpened(Ini ini) {
            this.ini = ini;
            // 控制图源“LSP”是否可用
            ExpanderLsp.Visibility = ini.R18 == 1 || ExpanderLsp.Tag.Equals(ini.Provider)
                ? Visibility.Visible : Visibility.Collapsed;
            // 刷新“图源”组设置项
            BoxBingLang.SelectedIndex = listBingLang.Select(t => t.Id).ToList().IndexOf(((BingIni)ini.GetIni(BingIni.ID)).Lang);
            BoxNasaOrder.SelectedIndex = listNasaOrder.Select(t => t.Id).ToList().IndexOf(((NasaIni)ini.GetIni(NasaIni.ID)).Order);
            ToggleNasaMirror.IsOn = "bjp".Equals(((NasaIni)ini.GetIni(NasaIni.ID)).Mirror);
            BoxOneplusOrder.SelectedIndex = listOneplusOrder.Select(t => t.Id).ToList().IndexOf(((OneplusIni)ini.GetIni(OneplusIni.ID)).Order);
            BoxTimelineOrder.SelectedIndex = listTimelineOrder.Select(t => t.Id).ToList().IndexOf(((TimelineIni)ini.GetIni(TimelineIni.ID)).Order);
            BoxOneOrder.SelectedIndex = listOneOrder.Select(t => t.Id).ToList().IndexOf(((OneIni)ini.GetIni(OneIni.ID)).Order);
            BoxHimawari8Offset.Value = ((Himawari8Ini)ini.GetIni(Himawari8Ini.ID)).Offset;
            BoxHimawari8Ratio.Value = ((Himawari8Ini)ini.GetIni(Himawari8Ini.ID)).Ratio;
            BoxYmyouliOrder.SelectedIndex = listYmyouliOrder.Select(t => t.Id).ToList().IndexOf(((YmyouliIni)ini.GetIni(YmyouliIni.ID)).Order);
            BoxQingbzOrder.SelectedIndex = listQingbzOrder.Select(t => t.Id).ToList().IndexOf(((QingbzIni)ini.GetIni(QingbzIni.ID)).Order);
            BoxWallhavenOrder.SelectedIndex = listWallhavenOrder.Select(t => t.Id).ToList().IndexOf(((WallhavenIni)ini.GetIni(WallhavenIni.ID)).Order);
            BoxWallhereOrder.SelectedIndex = listWallhereOrder.Select(t => t.Id).ToList().IndexOf(((WallhereIni)ini.GetIni(WallhereIni.ID)).Order);
            BoxWallpaperupOrder.SelectedIndex = listWallpaperupOrder.Select(t => t.Id).ToList().IndexOf(((WallpaperupIni)ini.GetIni(WallpaperupIni.ID)).Order);
            BoxToopicOrder.SelectedIndex = listToopicOrder.Select(t => t.Id).ToList().IndexOf(((ToopicIni)ini.GetIni(ToopicIni.ID)).Order);
            BoxInfinityOrder.SelectedIndex = listInfinityOrder.Select(t => t.Id).ToList().IndexOf(((InfinityIni)ini.GetIni(InfinityIni.ID)).Order);
            BoxGluttonAlbum.SelectedIndex = listGluttonAlbum.Select(t => t.Id).ToList().IndexOf(((GluttonIni)ini.GetIni(GluttonIni.ID)).Album);
            BoxGluttonOrder.SelectedIndex = listGluttonOrder.Select(t => t.Id).ToList().IndexOf(((GluttonIni)ini.GetIni(GluttonIni.ID)).Order);
            BoxLspOrder.SelectedIndex = listLspOrder.Select(t => t.Id).ToList().IndexOf(((LspIni)ini.GetIni(LspIni.ID)).Order);
            // 刷新推送指示图标
            //foreach (string id in dicPushDesktop.Keys) {
            //    dicPushDesktop[id].Visibility = id.Equals(ini.DesktopProvider) ? Visibility.Visible : Visibility.Collapsed;
            //}
            //foreach (string id in dicPushLock.Keys) {
            //    dicPushLock[id].Visibility = id.Equals(ini.LockProvider) ? Visibility.Visible : Visibility.Collapsed;
            //}
            // 刷新主题设置
            RadioButton rbTheme = RbTheme.Items.Cast<RadioButton>().FirstOrDefault(rb => ini.Theme.Equals(rb.Tag));
            rbTheme.IsChecked = true;
            TextThemeCur.Text = rbTheme.Content.ToString();
            // 刷新“其他”组 Expander 随机一文
            _ = RandomGlitter();
            // 展开当前图源 Expander
            Expander expanderFocus = null;
            foreach (var item in ViewSettings.Children) {
                if (item is Expander expander && expander.Tag != null) { // 图源 Expander
                    // 刷新指示图标
                    UIElementCollection icons = (expander.Header as Grid).Children;
                    (icons[1] as FontIcon).Visibility = expander.Tag.Equals(ini.Provider) ? Visibility.Visible : Visibility.Collapsed;
                    (icons[2] as FontIcon).Visibility = expander.Tag.Equals(ini.DesktopProvider) ? Visibility.Visible : Visibility.Collapsed;
                    (icons[3] as FontIcon).Visibility = expander.Tag.Equals(ini.LockProvider) ? Visibility.Visible : Visibility.Collapsed;
                    // 折叠非目标图源
                    if (expander.Tag.Equals(ini.Provider)) {
                        expanderFocus = expander;
                    } else {
                        expander.IsExpanded = false;
                    }
                }
            }
            if (expanderFocus != null) {
                expanderFocus.IsExpanded = true;
            }
        }

        private async Task RefreshCate(ComboBox boxCate, BaseIni bi) {
            ObservableCollection<CateMeta> boxCateList = boxCate.ItemsSource as ObservableCollection<CateMeta>;
            if (boxCateList.Count > 0) {
                return;
            }
            boxCateList.Add(new CateMeta {
                Id = "",
                Name = resLoader.GetString("Cate_all")
            });
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
            foreach (CateMeta meta in bi.Cates) {
                boxCateList.Add(meta);
            }
            boxCate.SelectedIndex = boxCateList.Select(t => t.Id).ToList().IndexOf(bi.Cate);
        }

        private async Task RefreshExpander(Expander expanderTarget, bool loadCate) {
            // 刷新标题（标记为当前图源）、折叠非目标图源 Expander
            foreach (var item in ViewSettings.Children) {
                if (item is Expander expander && expander.Tag != null) {
                    FontIcon icon = ((expander.Header as Grid).Children[1] as FontIcon);
                    icon.Visibility = expander == expanderTarget ? Visibility.Visible : Visibility.Collapsed;
                    if (expander != expanderTarget && expander.IsExpanded) {
                        expander.IsExpanded = false;
                    }
                }
            }
            // 刷新图源类别设置项
            long start = DateTime.Now.Ticks;
            string providerId = expanderTarget.Tag as string;
            Task taskCate = null;
            if (loadCate) {
                ComboBox boxCate = ((expanderTarget.Content as StackPanel).Children[0] as Grid).Children[2] as ComboBox;
                taskCate = RefreshCate(boxCate, ini.GetIni(providerId));
            }
            // 保存配置&回调
            if (!ini.Provider.Equals(providerId)) {
                ini.Provider = providerId;
                await IniUtil.SaveProviderAsync(providerId);
                SettingsChanged?.Invoke(this, new SettingsEventArgs {
                    ProviderChanged = true
                });
            }
            // 滚动至合适位置
            await Task.Delay(Math.Max(300 - (int)(DateTime.Now.Ticks - start) / 10000, 0));
            Point position = expanderTarget.TransformToVisual(ViewSettings).TransformPoint(new Point(0, 0));
            ScrollSettings.ChangeView(0, position.Y, 1, false);
            if (taskCate != null) {
                await taskCate;
            }
        }

        private async Task RandomGlitter() {
            if (release == null) {
                release = await Api.VersionAsync();
                if (SysUtil.CheckNewVer(release.Version)) {
                    SettingsChanged?.Invoke(this, new SettingsEventArgs {
                        VersionChanged = release
                    });
                }
            }
            // 随机替换“评分”按钮为“赞助”
            if (DateTime.Now.Ticks % 2 == 0) {
                BtnReview.Content = resLoader.GetString("ActionDonate");
            }
            // 刷新运营数据
            if (release.Life != null && release.Life.Past > 0) {
                SettingsReviewDesc.Text = string.Format(resLoader.GetString("Life"),
                    release.Life.Past, release.Life.DonateCount, release.Life.Remain);
                string[] users = release.Life.DonateRank?.Split(",");
                if (users != null && users.Length >= 2) {
                    TextDonators.Text = string.Format(resLoader.GetString("ThankDonate1"), users);
                    if (!string.IsNullOrEmpty(release.Life.DonateUser)) {
                        TextExpand.Text = resLoader.GetString("ActionExpand");
                        LinkDonators.Click += (s, e) => {
                            TextExpand.Text = "";
                            TextDonators.Text = string.Format(resLoader.GetString("ThankDonate2"),
                                release.Life.DonateUser.Replace(",", ", "));
                        };
                    }
                    TextThankDonate.Visibility = Visibility.Visible;
                }
            }
            // 随机一文
            if (release.Glitter != null && release.Glitter.Length > 0) {
                SettingsThankDesc.Text = release.Glitter[new Random().Next(release.Glitter.Length)];
            }
            // 刷新版本状态
            if (SysUtil.CheckNewVer(release.Version)) {
                TextRelease.Text = resLoader.GetString("NewRelease");
                LinkRelease.NavigateUri = new Uri(release.Url);
                ToolTipService.SetToolTip(LinkRelease, new ToolTip {
                    Content = release.Version
                });
            } else {
                TextRelease.Text = "";
            }
            // 刷新公告板
            if (release.Bbs != null && release.Bbs.Count > 0) {
                release.Bbs.Sort((a, b) => b.Start.CompareTo(a.Start)); // 日期降序
                RunBbsContent1.Text = release.Bbs[0].Comment;
                if (release.Bbs.Count > 1) {
                    RunBbsDate1.Text = string.Format(resLoader.GetString("BbsDate"),
                        DateTime.Parse(release.Bbs[0].Start).ToString("M"));
                    RunBbsContent2.Text = release.Bbs[1].Comment;
                    RunBbsDate2.Text = string.Format(resLoader.GetString("BbsDate"),
                        DateTime.Parse(release.Bbs[1].Start).ToString("M"));
                    TextBbsContent2.Visibility = Visibility.Visible;
                } else {
                    TextBbsContent2.Visibility = Visibility.Collapsed;
                }
                if (release.Bbs.Count > 2) {
                    RunBbsContent3.Text = release.Bbs[2].Comment;
                    RunBbsDate3.Text = string.Format(resLoader.GetString("BbsDate"),
                        DateTime.Parse(release.Bbs[2].Start).ToString("M"));
                    TextBbsContent3.Visibility = Visibility.Visible;
                } else {
                    TextBbsContent3.Visibility = Visibility.Collapsed;
                }
                ExpanderBbs.Visibility = Visibility.Visible;
            } else {
                ExpanderBbs.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ImportAsync() {
            BtnLocalImport.Content = resLoader.GetString("BtnLocalImport/Content");
            BtnLocalImport.IsEnabled = false;
            PbImport.IsIndeterminate = true;
            PbImport.ShowError = false;
            PbImport.ShowPaused = false;
            PbImport.Visibility = Visibility.Visible;
            GluttonProvider glutton = ini.GenerateProvider(GluttonIni.ID) as GluttonProvider;
            LocalIni localIni = ini.GetIni(LocalIni.ID) as LocalIni;
            await glutton.LoadData(new CancellationTokenSource().Token, new GluttonIni() {
                Album = "rank",
                Order = "score"
            }, 0);
            List<Meta> top = glutton.GetMetas(localIni.Appetite);
            Dictionary<string, double> topProgress = new Dictionary<string, double>();
            if (top.Count == 0) {
                BtnLocalImport.IsEnabled = true;
                PbImport.ShowError = true;
                return;
            }
            BtnLocalImport.Content = string.Format(resLoader.GetString("ImportProgress"), 0, top.Count);
            PbImport.Maximum = top.Count;
            PbImport.Value = 0;

            BackgroundDownloader downloader = new BackgroundDownloader();
            IReadOnlyList<DownloadOperation> historyDownloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            StorageFolder folderLocal = await FileUtil.GetGalleryFolder(localIni.Folder);
            if (folderLocal == null) {
                BtnLocalImport.IsEnabled = true;
                PbImport.ShowError = true;
                return;
            }
            foreach (Meta meta in top) { // 开始下载
                string localName = string.Format("{0}-{1}{2}", resLoader.GetString("Provider_" + glutton.Id), meta.Id, meta.Format);
                if (await folderLocal.TryGetItemAsync(localName) != null || meta.Uhd == null) { // 已导入
                    await Task.Delay(60);
                    topProgress[meta.Uhd] = 1;
                    PbImport.Value = SumProgress(topProgress);
                    PbImport.IsIndeterminate = false;
                    BtnLocalImport.Content = string.Format(resLoader.GetString("ImportProgress"), (int)PbImport.Value, top.Count);
                    continue;
                }
                StorageFile cacheFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                    string.Format("{0}-{1}{2}", glutton.Id, meta.Id, meta.Format), CreationCollisionOption.OpenIfExists);
                BasicProperties fileProperties = await cacheFile.GetBasicPropertiesAsync();
                if (fileProperties.Size > 0) { // 已缓存过
                    await cacheFile.CopyAsync(folderLocal, localName, NameCollisionOption.ReplaceExisting);
                    topProgress[meta.Uhd] = 1;
                    PbImport.Value = SumProgress(topProgress);
                    PbImport.IsIndeterminate = false;
                    BtnLocalImport.Content = string.Format(resLoader.GetString("ImportProgress"), (int)PbImport.Value, top.Count);
                    continue;
                }
                topProgress[meta.Uhd] = 0;
                foreach (DownloadOperation o in historyDownloads) { // 从历史中恢复任务
                    if (meta.Uhd.Equals(o.RequestedUri)) {
                        meta.Do = o;
                        break;
                    }
                }
                if (meta.Do == null) { // 新建下载任务
                    meta.Do = downloader.CreateDownload(new Uri(meta.Uhd), cacheFile);
                    _ = meta.Do.StartAsync();
                }
            }
            foreach (Meta meta in top) { // 等待下载
                if (meta.Do == null) {
                    continue;
                }
                try {
                    if (meta.Do.Progress.Status == BackgroundTransferStatus.PausedByApplication) {
                        meta.Do.Resume();
                    }
                    Progress<DownloadOperation> progress = new Progress<DownloadOperation>((op) => {
                        if (op.Progress.TotalBytesToReceive > 0 && op.Progress.BytesReceived > 0) {
                            ulong value = op.Progress.BytesReceived * 100 / op.Progress.TotalBytesToReceive;
                            Debug.WriteLine(op.ResultFile.Name + " progress: " + value + "%");
                            topProgress[meta.Uhd] = op.Progress.BytesReceived * 1.0 / op.Progress.TotalBytesToReceive;
                            PbImport.Value = SumProgress(topProgress);
                            PbImport.IsIndeterminate = false;
                        }
                    });
                    _ = await meta.Do.AttachAsync().AsTask(progress);
                    if (meta.Do.Progress.Status == BackgroundTransferStatus.Completed) {
                        Debug.WriteLine("ImportAsync() downloaded " + meta.Do.ResultFile.Name);
                        string localName = string.Format("{0}-{1}{2}", resLoader.GetString("Provider_" + glutton.Id), meta.Id, meta.Format);
                        await meta.Do.ResultFile.CopyAsync(folderLocal, localName, NameCollisionOption.ReplaceExisting);
                        topProgress[meta.Uhd] = 1;
                        PbImport.Value = SumProgress(topProgress);
                        PbImport.IsIndeterminate = false;
                        BtnLocalImport.Content = string.Format(resLoader.GetString("ImportProgress"), (int)PbImport.Value, top.Count);
                    }
                } catch (Exception e) {
                    meta.Do = null;
                    LogUtil.E("ImportAsync() " + e.Message);
                }
            }
            BtnLocalImport.IsEnabled = true;
            PbImport.ShowPaused = true;
        }

        private double SumProgress(Dictionary<string, double> dicProgress) {
            double sum = 0;
            foreach (double v in dicProgress.Values) {
                sum += v;
            }
            return sum;
        }

        private async void ExpanderStaticProvider_Expanding(Expander sender, ExpanderExpandingEventArgs args) {
            // 无需请求分类接口
            await RefreshExpander(sender, false);
        }

        private async void ExpanderDynamicProvider_Expanding(Expander sender, ExpanderExpandingEventArgs args) {
            // 需请求分类接口
            await RefreshExpander(sender, true);
        }

        private async void RbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is RadioButton selectItem) {
                TextThemeCur.Text = selectItem.Content.ToString();
                string theme = selectItem.Tag?.ToString();
                if (ini.Theme.Equals(theme)) {
                    return;
                }
                ini.Theme = theme;
                await IniUtil.SaveThemeAsync(ini.Theme);
                if (Window.Current.Content is FrameworkElement rootElement) {
                    rootElement.RequestedTheme = ThemeUtil.ParseTheme(theme);
                }
                SettingsChanged?.Invoke(this, new SettingsEventArgs {
                    ThemeChanged = true
                });
            }
        }

        private async void LinkDonate_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args) {
            await new DonateDlg {
                RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
            }.ShowAsync();
        }

        private async void BtnIni_Click(object sender, RoutedEventArgs e) {
            await FileUtil.LaunchFileAsync(await IniUtil.GetIniPath());
        }

        private async void BtnShowSave_Click(object sender, RoutedEventArgs e) {
            await FileUtil.LaunchFolderAsync(await KnownFolders.PicturesLibrary.CreateFolderAsync(resLoader.GetString("AppNameShort"),
                CreationCollisionOption.OpenIfExists));
        }

        private async void BtnShowCache_Click(object sender, RoutedEventArgs e) {
            await FileUtil.LaunchFolderAsync(ApplicationData.Current.TemporaryFolder);
        }

        private async void BtnReview_Click(object sender, RoutedEventArgs e) {
            if (BtnReview.Content.Equals(resLoader.GetString("ActionDonate"))) {
                await new DonateDlg {
                    RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
                }.ShowAsync();
            } else {
                await FileUtil.LaunchUriAsync(new Uri(resLoader.GetString("LinkReview/NavigateUri")));
            }
        }

        private async void BoxBingLang_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string lang = (e.AddedItems[0] as CateMeta).Id;
            BingIni bi = ini.GetIni(BingIni.ID) as BingIni;
            if (lang.Equals(bi.Lang)) {
                return;
            }
            bi.Lang = lang;
            await IniUtil.SaveBingLangAsync(bi.Lang);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxNasaOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(NasaIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveNasaOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void ToggleNasaMirror_Toggled(object sender, RoutedEventArgs e) {
            string mirror = ((ToggleSwitch)sender).IsOn ? "bjp" : "";
            NasaIni bi = ini.GetIni(NasaIni.ID) as NasaIni;
            if (mirror.Equals(bi.Mirror)) {
                return;
            }
            bi.Mirror = mirror;
            await IniUtil.SaveNasaMirrorAsync(bi.Mirror);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxOneplusOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(OneplusIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveOneplusOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxTimelineCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(TimelineIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveTimelineCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxTimelineOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(TimelineIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveTimelineOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private void BtnTimelineContribute_Click(object sender, RoutedEventArgs e) {
            DlgChanged?.Invoke(this, new DlgEventArgs {
                TimelineContributeChanged = true
            });
        }

        private async void BtnTimelineDonate_Click(object sender, RoutedEventArgs e) {
            await new DonateDlg {
                RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
            }.ShowAsync();
        }

        private void BoxHimawari8Offset_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) {
            if (himawari8OffsetTimer == null) {
                himawari8OffsetTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1000) };
                himawari8OffsetTimer.Tick += async (sender2, e2) => {
                    himawari8OffsetTimer.Stop();
                    float offset = (float)BoxHimawari8Offset.Value;
                    Himawari8Ini bi = ini.GetIni(Himawari8Ini.ID) as Himawari8Ini;
                    if (Math.Abs(offset - bi.Offset) < 0.01f) {
                        return;
                    }
                    bi.Offset = offset;
                    await IniUtil.SaveHimawari8OffsetAsync(offset);
                    await IniUtil.SaveProviderAsync(bi.Id);
                    SettingsChanged?.Invoke(this, new SettingsEventArgs {
                        ProviderConfigChanged = true
                    });
                };
            }
            himawari8OffsetTimer.Stop();
            himawari8OffsetTimer.Start();
        }

        private void BoxHimawari8Ratio_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) {
            if (himawari8RatioTimer == null) {
                himawari8RatioTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1000) };
                himawari8RatioTimer.Tick += async (sender2, e2) => {
                    himawari8RatioTimer.Stop();
                    float ratio = (float)BoxHimawari8Ratio.Value;
                    Himawari8Ini bi = ini.GetIni(Himawari8Ini.ID) as Himawari8Ini;
                    if (Math.Abs(ratio - bi.Ratio) < 0.01f) {
                        return;
                    }
                    bi.Ratio = ratio;
                    await IniUtil.SaveHimawari8RatioAsync(ratio);
                    await IniUtil.SaveProviderAsync(bi.Id);
                    SettingsChanged?.Invoke(this, new SettingsEventArgs {
                        ProviderConfigChanged = true
                    });
                };
            }
            himawari8RatioTimer.Stop();
            himawari8RatioTimer.Start();
        }

        private async void BoxYmyouliCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(YmyouliIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveYmyouliCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxYmyouliOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(YmyouliIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveYmyouliOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BtnYmyouliDonate_Click(object sender, RoutedEventArgs e) {
            await FileUtil.LaunchUriAsync(new Uri(resLoader.GetString("UrlYmyouli")));
        }

        private async void BoxInfinityOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(InfinityIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveInfinityOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxGluttonAlbum_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string album = (e.AddedItems[0] as CateMeta).Id;
            GluttonIni bi = ini.GetIni(GluttonIni.ID) as GluttonIni;
            if (album.Equals(bi.Album)) {
                return;
            }
            bi.Album = album;
            await IniUtil.SaveGluttonAlbumAsync(bi.Album);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxGluttonOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(GluttonIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveGluttonOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxOneOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(OneIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveOneOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxQingbzCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(QingbzIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveQingbzCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxQingbzOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(QingbzIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveQingbzOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BtnQingbzDonate_Click(object sender, RoutedEventArgs e) {
            await FileUtil.LaunchUriAsync(new Uri(resLoader.GetString("UrlQingbz")));
        }

        private async void BoxWallhavenCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(WallhavenIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveWallhavenCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxWallhavenOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(WallhavenIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveWallhavenOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxWallhereCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(WallhereIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveWallhereCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxWallhereOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(WallhereIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveWallhereOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxWallpaperupCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(WallpaperupIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveWallpaperupCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxWallpaperupOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(WallpaperupIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveWallpaperupOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxToopicCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(ToopicIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveToopicCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxToopicOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(ToopicIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveToopicOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BtnToopicDonate_Click(object sender, RoutedEventArgs e) {
            await FileUtil.LaunchUriAsync(new Uri(resLoader.GetString("UrlToopic")));
        }

        private async void BoxLspCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(LspIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveLspCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxLspOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(LspIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveLspOrderAsync(bi.Order);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void ToggleLspR22_Toggled(object sender, RoutedEventArgs e) {
            if (ToggleLspR22.IsOn) {
                R22AuthApiData data = await Api.LspR22AuthAsync();
                if (data.R22 == 0) { // 未获授权
                    ToggleLspR22.Toggled -= ToggleLspR22_Toggled;
                    ToggleLspR22.IsOn = false;
                    ToggleLspR22.Toggled += ToggleLspR22_Toggled;
                    DlgChanged?.Invoke(this, new DlgEventArgs {
                        LspR22Changed = data
                    });
                    return;
                }
            }

            LspIni bi = ini.GetIni(LspIni.ID) as LspIni;
            bi.R22 = ToggleLspR22.IsOn;
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BtnLocalFolder_Click(object sender, RoutedEventArgs e) {
            LocalIni bi = ini.GetIni(LocalIni.ID) as LocalIni;
            StorageFolder folder = await FileUtil.GetGalleryFolder(bi.Folder);
            if (folder == null) {
                return;
            }
            IReadOnlyList<StorageFile> imgFiles = await folder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate);
            StorageFile fileSelected = imgFiles.FirstOrDefault(f => f.ContentType.StartsWith("image"));
            await FileUtil.LaunchFolderAsync(folder, fileSelected);
        }

        private async void BtnLocalPick_Click(object sender, RoutedEventArgs e) {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null) {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                LocalIni bi = ini.GetIni(LocalIni.ID) as LocalIni;
                //if (folder.Path.Equals(bi.Folder)) {
                //    return;
                //}
                bi.Folder = folder.Path;
                await IniUtil.SaveLocalFolderAsync(bi.Folder);
                await IniUtil.SaveProviderAsync(bi.Id);
                SettingsChanged?.Invoke(this, new SettingsEventArgs {
                    ProviderConfigChanged = true
                });
            }
        }

        private async void BtnLocalImport_Click(object sender, RoutedEventArgs e) {
            await ImportAsync();
        }

        private string GenerateProviderTitle(object tag) {
            // 生成图源 Expander 标题
            return resLoader.GetString("Provider_" + tag);
        }

        private string GenerateProviderDesc(object tag) {
            // 生成图源 Expander 描述
            return resLoader.GetString("Slogan_" + tag);
        }

        //private string GenerateProviderIcon(bool expanded) {
        //    // 无聊：根据图源 Expander 展开状态显示不同的图标
        //    // 注意，XAML：&#xE899; C#：\uE899
        //    return expanded ? "\uE899" : "\uE76E";
        //}

        //private string GeneratePushDesktopIcon(object tag) {
        //    Debug.WriteLine("GeneratePushDesktopIcon() " + ini.DesktopProvider);
        //    return tag.Equals(ini.DesktopProvider) ? "\uE7F7" : "";
        //}

        //private string GeneratePushLockIcon(object tag) {
        //    Debug.WriteLine("GeneratePushLockIcon() " + ini.LockProvider);
        //    return tag.Equals(ini.LockProvider) ? "\uEE3F" : "";
        //}
    }

    public class SettingsEventArgs : EventArgs {
        public bool ProviderChanged { get; set; }

        public bool ProviderConfigChanged { get; set; }

        public bool ThemeChanged { get; set; }

        public ReleaseApiData VersionChanged { get; set; }
    }

    public class DlgEventArgs : EventArgs {
        public bool TimelineContributeChanged { get; set; }

        public R22AuthApiData LspR22Changed { get; set; }
    }
}
