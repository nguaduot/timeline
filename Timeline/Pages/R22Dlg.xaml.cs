using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Timeline.Utils;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Timeline.Pages {
    public sealed partial class R22Dlg : ContentDialog {
        public R22Dlg(string comment, string answer) {
            this.InitializeComponent();

            BoxR22Code.Text = comment ?? "";
            this.IsPrimaryButtonEnabled = BoxR22Code.Text.Trim().Length > 0;
            BoxR22Answer.Text = answer ?? "";
            BoxR22Answer.Visibility = BoxR22Answer.Text.Trim().Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BoxR22Code_TextChanged(object sender, TextChangedEventArgs e) {
            this.IsPrimaryButtonEnabled = BoxR22Code.Text.Trim().Length > 0;
        }

        private async void DlgR22_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            string comment = BoxR22Code.Text;
            await Api.LspR22AuthAsync(comment);
        }
    }
}
