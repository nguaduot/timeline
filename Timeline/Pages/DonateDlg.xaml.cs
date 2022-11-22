using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace Timeline.Pages {
    public sealed partial class DonateDlg : ContentDialog {
        private readonly ResourceLoader resLoader;
        private Channel channel = Channel.WeChat;
        private bool doNotClose = false;

        private enum Channel {
            WeChat, Alipay, Ecny
        }

        public DonateDlg() {
            this.InitializeComponent();

            resLoader = ResourceLoader.GetForCurrentView();
            ChangeCode(channel);
        }

        private void ChangeCode(Channel channel) {
            this.channel = channel;
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
            doNotClose = true;
            switch (channel) {
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
        }

        private void Donate_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            doNotClose = true;
            switch (channel) {
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
        }

        private void Donate_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            doNotClose = false;
        }

        private void Donate_Closing(ContentDialog sender, ContentDialogClosingEventArgs args) {
            args.Cancel = doNotClose;
        }
    }
}
