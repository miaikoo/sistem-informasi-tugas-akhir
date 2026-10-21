using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SITA.App.Services;
using SITA.Core.Data;
using SITA.Core.Services;

namespace SITA.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    /// <summary>Set from the view's PasswordBox (which cannot be data-bound for security reasons).</summary>
    public string Password { get; set; } = "";

    public string DemoHint { get; } =
        $"Akun demo — Admin: {DemoDataSeeder.AdminUsername} / {DemoDataSeeder.AdminPassword}  •  Dosen: budi / {DemoDataSeeder.DosenPassword}";

    public event EventHandler? LoginSucceeded;

    public LoginViewModel(AuthService auth) => _auth = auth;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            string user = Username, pass = Password;
            // BCrypt is deliberately slow; keep the UI responsive while it runs.
            var result = await Task.Run(() => _auth.Login(user, pass));
            if (result.Success)
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            else
                ErrorMessage = result.ErrorText;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public record NavItem(string Title, string Glyph, Type? PageType, bool IsHeader = false);

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly AuthService _auth;
    private readonly ThemeService _theme;
    private readonly IDialogService _dialog;

    public UserSession Session { get; }
    public NotificationService Notifications { get; }
    public ObservableCollection<NavItem> NavItems { get; } = [];

    [ObservableProperty] private NavItem? _selectedNav;
    [ObservableProperty] private PageViewModel? _currentPage;
    [ObservableProperty] private bool _isDark;

    public string UserName => Session.CurrentUser?.NamaLengkap ?? "";
    public string UserRole => Session.IsAdmin ? "Admin Program Studi" : "Dosen Pembimbing";
    public string Initials =>
        string.Concat(UserName.Replace("Dr.", "").Replace("Prof.", "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetter(w[0])).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    public event EventHandler? LogoutRequested;

    public MainViewModel(IServiceProvider services, UserSession session, AuthService auth, ThemeService theme,
        IDialogService dialog, NotificationService notifications)
    {
        _services = services;
        _auth = auth;
        _theme = theme;
        _dialog = dialog;
        Session = session;
        Notifications = notifications;
        IsDark = theme.IsDark;

        BuildNavigation();
        SelectedNav = NavItems.First(n => !n.IsHeader);
    }

    private void BuildNavigation()
    {
        NavItems.Add(new NavItem("Dashboard", "", typeof(DashboardViewModel)));
        if (Session.IsAdmin)
        {
            NavItems.Add(new NavItem("MONITORING TA", "", null, IsHeader: true));
            NavItems.Add(new NavItem("Tugas Akhir", "", typeof(TugasAkhirViewModel)));
            NavItems.Add(new NavItem("Log Bimbingan", "", typeof(BimbinganViewModel)));
            NavItems.Add(new NavItem("Jadwal Sidang", "", typeof(SidangViewModel)));
            NavItems.Add(new NavItem("DATA MASTER", "", null, IsHeader: true));
            NavItems.Add(new NavItem("Mahasiswa", "", typeof(MahasiswaViewModel)));
            NavItems.Add(new NavItem("Dosen", "", typeof(DosenViewModel)));
            NavItems.Add(new NavItem("Program Studi", "", typeof(ProdiViewModel)));
            NavItems.Add(new NavItem("SISTEM", "", null, IsHeader: true));
            NavItems.Add(new NavItem("Pengguna", "", typeof(PenggunaViewModel)));
        }
        else
        {
            NavItems.Add(new NavItem("BIMBINGAN SAYA", "", null, IsHeader: true));
            NavItems.Add(new NavItem("Mahasiswa Bimbingan", "", typeof(TugasAkhirViewModel)));
            NavItems.Add(new NavItem("Log Bimbingan", "", typeof(BimbinganViewModel)));
            NavItems.Add(new NavItem("Jadwal Sidang", "", typeof(SidangViewModel)));
        }
    }

    partial void OnSelectedNavChanged(NavItem? value)
    {
        if (value?.PageType is null) return;
        var page = (PageViewModel)_services.GetRequiredService(value.PageType);
        CurrentPage = page;
        page.Load();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _theme.Toggle();
        IsDark = _theme.IsDark;
    }

    [RelayCommand]
    private void RefreshPage() => CurrentPage?.Load();

    [RelayCommand]
    private void Logout()
    {
        if (!_dialog.Confirm("Keluar", "Anda yakin ingin keluar dari aplikasi?", "Keluar")) return;
        _auth.Logout();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }
}
