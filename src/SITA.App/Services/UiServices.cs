using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SITA.App.Views;

namespace SITA.App.Services;

public interface IDialogService
{
    bool Confirm(string title, string message, string confirmText = "Hapus");
    void Error(string message, string title = "Terjadi Kesalahan");
}

public class DialogService : IDialogService
{
    private static Window? Owner =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

    public bool Confirm(string title, string message, string confirmText = "Hapus")
    {
        var dialog = new MessageDialog(title, message, confirmText, showCancel: true) { Owner = Owner };
        return dialog.ShowDialog() == true;
    }

    public void Error(string message, string title = "Terjadi Kesalahan")
    {
        var dialog = new MessageDialog(title, message, "Tutup", showCancel: false) { Owner = Owner };
        dialog.ShowDialog();
    }
}

/// <summary>Small toast shown at the bottom of the main window for non-blocking feedback.</summary>
public partial class NotificationService : ObservableObject
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(3) };

    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _isVisible;

    public NotificationService()
    {
        _timer.Tick += (_, _) =>
        {
            IsVisible = false;
            _timer.Stop();
        };
    }

    public void Show(string message)
    {
        Message = message;
        IsVisible = true;
        _timer.Stop();
        _timer.Start();
    }
}
