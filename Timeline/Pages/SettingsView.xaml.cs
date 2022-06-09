using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Timeline.Beans;
using Timeline.Providers;
using Timeline.Utils;
using Windows.ApplicationModel.Resources;
using Windows.Globalization.NumberFormatting;
using Windows.Storage;
using Windows.System;
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
        ObservableCollection<CateMeta> listYmyouliCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listYmyouliOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listInfinityOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listOneOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listQingbzCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listQingbzOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listObzhiCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listObzhiOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhereCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listWallhereOrder = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listLspCate = new ObservableCollection<CateMeta>();
        ObservableCollection<CateMeta> listLspOrder = new ObservableCollection<CateMeta>();

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
            foreach (string item in ObzhiIni.ORDERS) {
                listObzhiOrder.Add(new CateMeta {
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

        public void PaneOpened(Ini ini) {
            this.ini = ini;

            RefreshProviderExpander(ini.Provider);

            BoxBingLang.SelectedIndex = listBingLang.Select(t => t.Id).ToList().IndexOf(((BingIni)ini.GetIni(BingIni.ID)).Lang);
            ToggleNasaMirror.IsOn = "bjp".Equals(((NasaIni)ini.GetIni(NasaIni.ID)).Mirror);
            BoxOneplusOrder.SelectedIndex = listOneplusOrder.Select(t => t.Id).ToList().IndexOf(((OneplusIni)ini.GetIni(OneplusIni.ID)).Order);
            //BoxTimelineCate.SelectedIndex = listTimelineCate.Select(t => t.Id).ToList().IndexOf(((TimelineIni)ini.GetIni(TimelineIni.ID)).Cate);
            BoxTimelineOrder.SelectedIndex = listTimelineOrder.Select(t => t.Id).ToList().IndexOf(((TimelineIni)ini.GetIni(TimelineIni.ID)).Order);
            BoxHimawari8Offset.Value = ((Himawari8Ini)ini.GetIni(Himawari8Ini.ID)).Offset;
            BoxHimawari8Ratio.Value = ((Himawari8Ini)ini.GetIni(Himawari8Ini.ID)).Ratio;
            //BoxYmyouliCate.SelectedIndex = listYmyouliCate.Select(t => t.Id).ToList().IndexOf(((YmyouliIni)ini.GetIni(YmyouliIni.ID)).Cate);
            BoxYmyouliOrder.SelectedIndex = listYmyouliOrder.Select(t => t.Id).ToList().IndexOf(((YmyouliIni)ini.GetIni(YmyouliIni.ID)).Order);
            BoxInfinityOrder.SelectedIndex = listInfinityOrder.Select(t => t.Id).ToList().IndexOf(((InfinityIni)ini.GetIni(InfinityIni.ID)).Order);
            BoxOneOrder.SelectedIndex = listOneOrder.Select(t => t.Id).ToList().IndexOf(((OneIni)ini.GetIni(OneIni.ID)).Order);
            //BoxQingbzCate.SelectedIndex = listQingbzCate.Select(t => t.Id).ToList().IndexOf(((QingbzIni)ini.GetIni(QingbzIni.ID)).Cate);
            BoxQingbzOrder.SelectedIndex = listQingbzOrder.Select(t => t.Id).ToList().IndexOf(((QingbzIni)ini.GetIni(QingbzIni.ID)).Order);
            //BoxObzhiCate.SelectedIndex = listObzhiCate.Select(t => t.Id).ToList().IndexOf(((ObzhiIni)ini.GetIni(ObzhiIni.ID)).Cate);
            BoxObzhiOrder.SelectedIndex = listObzhiOrder.Select(t => t.Id).ToList().IndexOf(((ObzhiIni)ini.GetIni(ObzhiIni.ID)).Order);
            //BoxWallhereCate.SelectedIndex = listWallhereCate.Select(t => t.Id).ToList().IndexOf(((WallhereIni)ini.GetIni(WallhereIni.ID)).Cate);
            BoxWallhereOrder.SelectedIndex = listWallhereOrder.Select(t => t.Id).ToList().IndexOf(((WallhereIni)ini.GetIni(WallhereIni.ID)).Order);
            //BoxLspCate.SelectedIndex = listLspCate.Select(t => t.Id).ToList().IndexOf(((LspIni)ini.GetIni(LspIni.ID)).Cate);
            BoxLspOrder.SelectedIndex = listLspOrder.Select(t => t.Id).ToList().IndexOf(((LspIni)ini.GetIni(LspIni.ID)).Order);

            ExpanderLsp.Visibility = ini.R18 == 1 ? Visibility.Visible : Visibility.Collapsed;

            RadioButton rb = RbTheme.Items.Cast<RadioButton>().FirstOrDefault(c => ini.Theme.Equals(c?.Tag?.ToString()));
            rb.IsChecked = true;
            TextThemeCur.Text = rb.Content.ToString();

            _ = RandomGlitter();
        }

        public void PaneClosed() {
            RefreshProviderExpander();
        }

        private async Task RandomGlitter() {
            IList<string> glitter = await FileUtil.GetGlitterAsync();
            LogUtil.I("RandomGlitter() " + glitter.Count);
            if (glitter.Count >= 3) {
                string glitter1 = glitter[new Random().Next(glitter.Count)];
                glitter.Remove(glitter1);
                SettingsCdnDesc.Text = glitter1;
                string glitter2 = glitter[new Random().Next(glitter.Count)];
                SettingsReviewDesc.Text = glitter2;
                glitter.Remove(glitter2);
                string glitter3 = glitter[new Random().Next(glitter.Count)];
                SettingsThankDesc.Text = glitter3;
            }
        }

        private void RefreshProviderExpander(string providerId = null) {
            //string tagCheck = HttpUtility.HtmlDecode("&#128994;&#32;");
            string tagCheck = "● ";

            ExpanderBing.IsExpanded = BingIni.ID.Equals(providerId);
            SettingsBingTitle.Text = (BingIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + BingIni.ID);
            SettingsBingDesc.Text = resLoader.GetString("Slogan_" + BingIni.ID);

            ExpanderNasa.IsExpanded = NasaIni.ID.Equals(providerId);
            SettingsNasaTitle.Text = (NasaIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + NasaIni.ID);
            SettingsNasaDesc.Text = resLoader.GetString("Slogan_" + NasaIni.ID);

            ExpanderOneplus.IsExpanded = OneplusIni.ID.Equals(providerId);
            SettingsOneplusTitle.Text = (OneplusIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + OneplusIni.ID);
            SettingsOneplusDesc.Text = resLoader.GetString("Slogan_" + OneplusIni.ID);

            ExpanderTimeline.IsExpanded = TimelineIni.ID.Equals(providerId);
            SettingsTimelineTitle.Text = (TimelineIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + TimelineIni.ID);
            SettingsTimelineDesc.Text = resLoader.GetString("Slogan_" + TimelineIni.ID);

            ExpanderHimawari8.IsExpanded = Himawari8Ini.ID.Equals(providerId);
            SettingsHimawari8Title.Text = (Himawari8Ini.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + Himawari8Ini.ID);
            SettingsHimawari8Desc.Text = resLoader.GetString("Slogan_" + Himawari8Ini.ID);

            ExpanderYmyouli.IsExpanded = YmyouliIni.ID.Equals(providerId);
            SettingsYmyouliTitle.Text = (YmyouliIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + YmyouliIni.ID);
            SettingsYmyouliDesc.Text = resLoader.GetString("Slogan_" + YmyouliIni.ID);

            ExpanderInfinity.IsExpanded = InfinityIni.ID.Equals(providerId);
            SettingsInfinityTitle.Text = (InfinityIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + InfinityIni.ID);
            SettingsInfinityDesc.Text = resLoader.GetString("Slogan_" + InfinityIni.ID);

            ExpanderOne.IsExpanded = OneIni.ID.Equals(providerId);
            SettingsOneTitle.Text = (OneIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + OneIni.ID);
            SettingsOneDesc.Text = resLoader.GetString("Slogan_" + OneIni.ID);

            ExpanderQingbz.IsExpanded = QingbzIni.ID.Equals(providerId);
            SettingsQingbzTitle.Text = (QingbzIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + QingbzIni.ID);
            SettingsQingbzDesc.Text = resLoader.GetString("Slogan_" + QingbzIni.ID);

            ExpanderObzhi.IsExpanded = ObzhiIni.ID.Equals(providerId);
            SettingsObzhiTitle.Text = (ObzhiIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + ObzhiIni.ID);
            SettingsObzhiDesc.Text = resLoader.GetString("Slogan_" + ObzhiIni.ID);

            ExpanderWallhere.IsExpanded = WallhereIni.ID.Equals(providerId);
            SettingsWallhereTitle.Text = (WallhereIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + WallhereIni.ID);
            SettingsWallhereDesc.Text = resLoader.GetString("Slogan_" + WallhereIni.ID);

            ExpanderLsp.IsExpanded = LspIni.ID.Equals(providerId);
            SettingsLspTitle.Text = (LspIni.ID.Equals(providerId) ? tagCheck : "") + resLoader.GetString("Provider_" + LspIni.ID);
            SettingsLspDesc.Text = resLoader.GetString("Slogan_" + LspIni.ID);
        }

        private async Task RefreshProviderCate(ComboBox box, ObservableCollection<CateMeta> boxList, BaseIni bi) {
            if (boxList.Count > 0) {
                return;
            }
            boxList.Clear();
            boxList.Add(new CateMeta {
                Id = "",
                Name = resLoader.GetString("Cate_all")
            });
            if (bi.Cates.Count == 0) {
                bi.Cates = await Api.CateAsync(bi.GetCateApi());
            }
            foreach (CateMeta meta in bi.Cates) {
                boxList.Add(meta);
            }
            box.SelectedIndex = boxList.Select(t => t.Id).ToList().IndexOf(bi.Cate);
        }

        private async void ExpanderProvider_Expanding(Expander sender, ExpanderExpandingEventArgs args) {
            string providerId = sender.Tag as string;
            if (!ini.Provider.Equals(providerId)) {
                ini.Provider = providerId;
                await IniUtil.SaveProviderAsync(providerId);
                SettingsChanged?.Invoke(this, new SettingsEventArgs {
                    ProviderChanged = true
                });
            }

            RefreshProviderExpander(providerId);
            switch (providerId) {
                case TimelineIni.ID:
                    await RefreshProviderCate(BoxTimelineCate, listTimelineCate, ini.GetIni(providerId));
                    break;
                case YmyouliIni.ID:
                    await RefreshProviderCate(BoxYmyouliCate, listYmyouliCate, ini.GetIni(providerId));
                    break;
                case QingbzIni.ID:
                    await RefreshProviderCate(BoxQingbzCate, listQingbzCate, ini.GetIni(providerId));
                    break;
                case ObzhiIni.ID:
                    await RefreshProviderCate(BoxObzhiCate, listObzhiCate, ini.GetIni(providerId));
                    break;
                case WallhereIni.ID:
                    await RefreshProviderCate(BoxWallhereCate, listWallhereCate, ini.GetIni(providerId));
                    break;
                case LspIni.ID:
                    await RefreshProviderCate(BoxLspCate, listLspCate, ini.GetIni(providerId));
                    break;
            }
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

        private async void BoxObzhiCate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string cate = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(ObzhiIni.ID);
            if (cate.Equals(bi.Cate)) {
                return;
            }
            bi.Cate = cate;
            await IniUtil.SaveObzhiCateAsync(bi.Cate);
            await IniUtil.SaveProviderAsync(bi.Id);
            SettingsChanged?.Invoke(this, new SettingsEventArgs {
                ProviderConfigChanged = true
            });
        }

        private async void BoxObzhiOrder_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string order = (e.AddedItems[0] as CateMeta).Id;
            BaseIni bi = ini.GetIni(ObzhiIni.ID);
            if (order.Equals(bi.Order)) {
                return;
            }
            bi.Order = order;
            await IniUtil.SaveObzhiOrderAsync(bi.Order);
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
    }

    public class SettingsEventArgs : EventArgs {
        public bool ProviderChanged { get; set; }

        public bool ProviderConfigChanged { get; set; }

        public bool ThemeChanged { get; set; }
    }
}
