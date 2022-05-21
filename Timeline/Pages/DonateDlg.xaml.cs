using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace Timeline.Pages {
    public sealed partial class DonateDlg : ContentDialog {
        private bool doNotClose = false;

        public DonateDlg() {
            this.InitializeComponent();

            ChangeCode();
        }

        private void ChangeCode(bool viaAlipay = false) {
            ImgDonate.Source = new BitmapImage(new Uri(viaAlipay ? "ms-appx:///Assets/Images/donate_alipay.png" : "ms-appx:///Assets/Images/donate_wechat.png"));
        }

        private void Donate_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            ChangeCode();
            doNotClose = true;
        }

        private void Donate_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            ChangeCode(true);
            doNotClose = true;
        }

        private void Donate_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            doNotClose = false;
        }

        private void Donate_Closing(ContentDialog sender, ContentDialogClosingEventArgs args) {
            args.Cancel = doNotClose;
        }
    }
}
