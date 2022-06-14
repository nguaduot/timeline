using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Timeline.Beans;
using Timeline.Utils;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace Timeline.Pages {
    public sealed partial class SettingsView : UserControl {
        public event EventHandler<SettingsEventArgs> SettingsChanged;
        public event EventHandler<EventArgs> ContributeChanged;

        private Ini ini = new Ini();

        private readonly ResourceLoader resLoader;

        ObservableCollection<CateMeta> listBingLang = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listOneplusOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listTimelineCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listTimelineOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listOneOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listYmyouliCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listYmyouliOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhavenCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhavenOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listQingbzCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listQingbzOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhereCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhereOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listInfinityOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listLspCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listLspOrder = new ObservableCollection<CateMeta>();

        private List<string> glitters = new List<string>();

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
            foreach (string item in YmyouliIni.ORDERS) {
                listYmyouliOrder.Add(new CateMeta {
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
            foreach (string item in OneIni.ORDERS) {
                listOneOrder.Add(new CateMeta {
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

        public async Task NotifyPaneOpened(Ini ini) {
            this.ini = ini;
            // 控制图源“LSP”可见性
            ExpanderLsp.Visibility = ini.R18 == 1 || ExpanderLsp.Tag.Equals(ini.Provider)
                ? Visibility.Visible : Visibility.Collapsed;
            // 刷新“图源”组设置项
            BoxBingLang.SelectedIndex = listBingLang.Select(t => t.Id).ToList().IndexOf(((BingIni)ini.GetIni(BingIni.ID)).Lang);
            ToggleNasaMirror.IsOn = "bjp".Equals(((NasaIni)ini.GetIni(NasaIni.ID)).Mirror);
            BoxOneplusOrder.SelectedIndex = listOneplusOrder.Select(t => t.Id).ToList().IndexOf(((OneplusIni)ini.GetIni(OneplusIni.ID)).Order);
            BoxTimelineOrder.SelectedIndex = listTimelineOrder.Select(t => t.Id).ToList().IndexOf(((TimelineIni)ini.GetIni(TimelineIni.ID)).Order);
            BoxOneOrder.SelectedIndex = listOneOrder.Select(t => t.Id).ToList().IndexOf(((OneIni)ini.GetIni(OneIni.ID)).Order);
            BoxHimawari8Offset.Value = ((Himawari8Ini)ini.GetIni(Himawari8Ini.ID)).Offset;
            BoxHimawari8Ratio.Value = ((Himawari8Ini)ini.GetIni(Himawari8Ini.ID)).Ratio;
            BoxYmyouliOrder.SelectedIndex = listYmyouliOrder.Select(t => t.Id).ToList().IndexOf(((YmyouliIni)ini.GetIni(YmyouliIni.ID)).Order);
            BoxWallhavenOrder.SelectedIndex = listWallhavenOrder.Select(t => t.Id).ToList().IndexOf(((WallhavenIni)ini.GetIni(WallhavenIni.ID)).Order);
            BoxQingbzOrder.SelectedIndex = listQingbzOrder.Select(t => t.Id).ToList().IndexOf(((QingbzIni)ini.GetIni(QingbzIni.ID)).Order);
            BoxWallhereOrder.SelectedIndex = listWallhereOrder.Select(t => t.Id).ToList().IndexOf(((WallhereIni)ini.GetIni(WallhereIni.ID)).Order);
            BoxInfinityOrder.SelectedIndex = listInfinityOrder.Select(t => t.Id).ToList().IndexOf(((InfinityIni)ini.GetIni(InfinityIni.ID)).Order);
            BoxLspOrder.SelectedIndex = listLspOrder.Select(t => t.Id).ToList().IndexOf(((LspIni)ini.GetIni(LspIni.ID)).Order);
            ToggleLspR22.IsOn = ((LspIni)ini.GetIni(LspIni.ID)).R22 == 1;
            // 刷新主题设置
            RadioButton rbTheme = RbTheme.Items.Cast<RadioButton>().FirstOrDefault(rb => ini.Theme.Equals(rb.Tag));
            rbTheme.IsChecked = true;
            TextThemeCur.Text = rbTheme.Content.ToString();
            // 刷新“其他”组 Expander 随机一文
            await RandomGlitter();
            // 展开当前图源 Expander
            foreach (var item in ViewSettings.Children) {
                if (item is Expander expander && expander.Tag != null && expander.Tag.Equals(ini.Provider)) {
                    expander.IsExpanded = true;
                }
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
                bi.Cates = await Api.CateAsync(bi.GetCateApi());
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
                    FontIcon icon = ((expander.Header as Grid).Children[0] as FontIcon);
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
            await Task.Delay(Math.Max(400 - (int)(DateTime.Now.Ticks - start) / 10000, 0));
            Point position = expanderTarget.TransformToVisual(ViewSettings).TransformPoint(new Point(0, 0));
            ScrollSettings.ChangeView(0, position.Y, 1, false);
            if (taskCate != null) {
                await taskCate;
            }
        }

        private async Task RandomGlitter() {
            if (glitters.Count == 0) {
                glitters.AddRange(await FileUtil.GetGlitterAsync());
            }
            LogUtil.I("RandomGlitter() " + glitters.Count);
            List<string> glittersRandom = new List<string>();
            for (int i = 0; i < glitters.Count && i < 3; i++) {
                string target = glitters[new Random().Next(glitters.Count)];
                glitters.Remove(target);
                glittersRandom.Add(target);
            }
            glittersRandom.Sort((a, b) => a.Length.CompareTo(b.Length));
            SettingsReviewDesc.Text = glittersRandom[0];
            SettingsThankDesc.Text = glittersRandom[1];
            SettingsCdnDesc.Text = glittersRandom[2];
        }

        private async void ExpanderStaticProvider_Expanding(Expander sender, ExpanderExpandingEventArgs args) {
            await RefreshExpander(sender, false);
        }

        private async void ExpanderDynamicProvider_Expanding(Expander sender, ExpanderExpandingEventArgs args) {
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
            await FileUtil.LaunchUriAsync(new Uri(resLoader.GetStringForUri(new Uri("ms-resource:///Resources/LinkReview/NavigateUri"))));
        }

        private async void BoxBingLang_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string lang = (e.AddedItems[0] as CateMeta).Id;
            BingIni bi = (BingIni)ini.GetIni(BingIni.ID);
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

        private async void ToggleNasaMirror_Toggled(object sender, RoutedEventArgs e) {
            string mirror = ((ToggleSwitch)sender).IsOn ? "bjp" : "";
            NasaIni bi = (NasaIni)ini.GetIni(NasaIni.ID);
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
            ContributeChanged?.Invoke(this, new EventArgs());
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
                    Himawari8Ini bi = (Himawari8Ini)ini.GetIni(Himawari8Ini.ID);
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
                    Himawari8Ini bi = (Himawari8Ini)ini.GetIni(Himawari8Ini.ID);
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
            _ = Api.RankAsync(YmyouliIni.ID, null, "donate");
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
            _ = Api.RankAsync(QingbzIni.ID, null, "donate");
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
                ProviderConfigChanged = true,
                DoNotToastLsp = true
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
                ProviderConfigChanged = true,
                DoNotToastLsp = true
            });
        }

        private async void ToggleLspR22_Toggled(object sender, RoutedEventArgs e) {
            int r22 = (sender as ToggleSwitch).IsOn ? 1 : 0;
            string mirror = ((ToggleSwitch)sender).IsOn ? "bjp" : "";
            LspIni bi = (LspIni)ini.GetIni(LspIni.ID);
            if (bi.R22 == r22) {
                return;
            }
            bi.R22 = r22;
            await IniUtil.SaveLspR22Async(bi.R22);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true,
                DoNotToastLsp = bi.R22 == 0
            });
        }

        public string GenerateProviderTitle(object tag) {
            // 生成图源 Expander 标题
            return resLoader.GetString("Provider_" + tag);
        }

        public string GenerateProviderDesc(object tag) {
            // 生成图源 Expander 描述
            return resLoader.GetString("Slogan_" + tag);
        }

        public string GenerateProviderIcon(bool expanded) {
            // 无聊：根据图源 Expander 展开状态显示不同的图标
            // 注意，XAML：&#xE899; C#：\uE899
            return expanded ? "\uE899" : "\uE76E";
        }
    }

    public class SettingsEventArgs : EventArgs {
        public bool ProviderChanged { get; set; }

        public bool ProviderConfigChanged { get; set; }

        // 刷新时默认检测LSP图源并提示
        public bool DoNotToastLsp { get; set; }

        public bool ThemeChanged { get; set; }
    }
}
