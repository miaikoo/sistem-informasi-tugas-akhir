using System.Windows;

namespace SITA.App.Views;

public partial class MessageDialog : Window
{
    public MessageDialog(string title, string message, string confirmText, bool showCancel)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        if (!showCancel)
        {
            // Informational/error dialog: single neutral button, warning icon.
            CancelButton.Visibility = Visibility.Collapsed;
            ConfirmButton.IsCancel = true;
            ConfirmButton.Style = (Style)FindResource("PrimaryButton");
            IconGlyph.Text = "";
        }
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
