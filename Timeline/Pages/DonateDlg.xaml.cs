using System;
using Timeline.Utils;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Timeline.Pages {
    public sealed partial class DonateDlg : ContentDialog {
        private readonly ResourceLoader resLoader;
        private static Channel CAHNNEL = Channel.WeChat;

        private enum Channel {
            WeChat, Alipay, Ecny
        }

        public DonateDlg() {
            this.InitializeComponent();

            resLoader = ResourceLoader.GetForCurrentView();
            this.IsSecondaryButtonEnabled = false; // TODO
            ChangeCode(CAHNNEL);
        }

        private void ChangeCode(Channel channel) {
            CAHNNEL = channel;
            switch (channel) {
                case Channel.WeChat:
                    ImgDonate.Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/donate_wechat.png"));
                    this.PrimaryButtonText = resLoader.GetString("DonateChannelAlipay");
                    this.SecondaryButtonText = resLoader.GetString("DonateChannelEcny");
                    break;
                case Channel.Alipay:
                    ImgDonate.Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/donate_alipay.png"));
                    this.PrimaryButtonText = resLoader.GetString("DonateChannelWechat");
                    this.SecondaryButtonText = resLoader.GetString("DonateChannelEcny");
                    break;
                case Channel.Ecny:
                    ImgDonate.Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/donate_ecny.png"));
                    this.PrimaryButtonText = resLoader.GetString("DonateChannelWechat");
                    this.SecondaryButtonText = resLoader.GetString("DonateChannelAlipay");
                    break;
            }
        }

        private void Donate_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            switch (CAHNNEL) {
                case Channel.WeChat:
                    ChangeCode(Channel.Alipay);
                    break;
                case Channel.Alipay:
                    ChangeCode(Channel.WeChat);
                    break;
                case Channel.Ecny:
                    ChangeCode(Channel.WeChat);
                    break;
            }
            this.Hide();
            _ = new DonateDlg() {
                RequestedTheme = ThemeUtil.ParseTheme(IniUtil.GetIni().Theme) // 修复未响应主题切换的BUG
            }.ShowAsync();
        }

        private void Donate_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            switch (CAHNNEL) {
                case Channel.WeChat:
                    ChangeCode(Channel.Ecny);
                    break;
                case Channel.Alipay:
                    ChangeCode(Channel.Ecny);
                    break;
                case Channel.Ecny:
                    ChangeCode(Channel.Alipay);
                    break;
            }
            this.Hide();
            _ = new DonateDlg() {
                RequestedTheme = ThemeUtil.ParseTheme(IniUtil.GetIni().Theme) // 修复未响应主题切换的BUG
            }.ShowAsync();
        }
    }
}
