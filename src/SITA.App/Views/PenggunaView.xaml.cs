using System.Windows;
using System.Windows.Controls;
using SITA.App.ViewModels;

namespace SITA.App.Views;

public partial class PenggunaView : UserControl
{
    public PenggunaView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is PenggunaViewModel old) old.PasswordResetRequested -= ClearPassword;
            if (e.NewValue is PenggunaViewModel vm) vm.PasswordResetRequested += ClearPassword;
        };
    }

    // PasswordBox.Password is not bindable, so it is pushed to the view model manually.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is PenggunaViewModel vm) vm.FormPassword = PasswordBox.Password;
    }

    private void ClearPassword(object? sender, EventArgs e) => PasswordBox.Clear();
}
