using Timeline.Beans;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace Timeline.Pages {
    public sealed partial class ContributeDlg : ContentDialog {
        public ContributeDlg() {
            this.InitializeComponent();
        }

        public ContributeApiReq GetContent() {
            return new ContributeApiReq {
                Url = BoxUrl.Text.Trim(),
                Title = BoxTitle.Text.Trim(),
                Story = BoxStory.Text.Trim(),
                Contact = BoxContact.Text.Trim()
            };
        }

        private void BoxUrl_TextChanged(object sender, TextChangedEventArgs e) {
            this.IsPrimaryButtonEnabled = BoxUrl.Text.Trim().Length > 0;
        }
    }
}
