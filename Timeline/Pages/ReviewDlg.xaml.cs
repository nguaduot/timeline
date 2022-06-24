using System;
using Timeline.Utils;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace Timeline.Pages {
    public sealed partial class ReviewDlg : ContentDialog {
        public ReviewDlg() {
            this.InitializeComponent();

            //this.Title = ResourceLoader.GetForCurrentView().GetString("AppNameShort") + " " + SysUtil.GetPkgVer(true);
        }

        private async void LinkDonate_Click(object sender, RoutedEventArgs e) {
            this.Hide();
            Ini ini = await IniUtil.GetIniAsync();
            _ = new DonateDlg {
                RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
            }.ShowAsync();
        }

        private async void Dlg_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            await Launcher.LaunchUriAsync(new Uri(new ResourceLoader().GetString("LinkReview/NavigateUri")));
        }

        private async void Dlg_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            this.Hide();
            Ini ini = await IniUtil.GetIniAsync();
            _ = new DonateDlg {
                RequestedTheme = ThemeUtil.ParseTheme(ini.Theme) // 修复未响应主题切换的BUG
            }.ShowAsync();
        }
    }
}
