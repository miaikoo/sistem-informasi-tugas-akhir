using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SITA.App.Services;
using SITA.App.ViewModels;
using SITA.App.Views;
using SITA.Core.Data;
using SITA.Core.Repositories;
using SITA.Core.Services;

namespace SITA.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    public static string DataFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SITA");

    /// <summary>
    /// Demo mode (the default) keeps dummy data in memory only: nothing is written to disk and every
    /// launch starts from fresh sample data. Start with <c>--db</c> to use the persistent SQLite file.
    /// </summary>
    public static bool IsDemoMode { get; private set; } = true;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        IsDemoMode = !e.Args.Contains("--db", StringComparer.OrdinalIgnoreCase);

        var culture = new CultureInfo("id-ID");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        DispatcherUnhandledException += OnUnhandledException;

        _services = ConfigureServices();
        _services.GetRequiredService<ThemeService>().LoadSaved();

        if (!InitializeDatabase()) return;

        ShowLogin();
    }

    private static ServiceProvider ConfigureServices()
    {
        var s = new ServiceCollection();

        if (IsDemoMode)
        {
            s.AddSingleton<IDbConnectionFactory>(_ => SqliteConnectionFactory.InMemory("sita_demo"));
            // Throwaway data, so a cheap work factor keeps the reseed on every launch instant.
            s.AddSingleton<IPasswordHasher, BCryptPasswordHasher>(_ => new BCryptPasswordHasher(workFactor: 6));
        }
        else
        {
            s.AddSingleton<IDbConnectionFactory>(_ => SqliteConnectionFactory.ForFile(Path.Combine(DataFolder, "sita.db")));
            s.AddSingleton<IPasswordHasher, BCryptPasswordHasher>(_ => new BCryptPasswordHasher());
        }
        s.AddSingleton<DatabaseInitializer>();
        s.AddSingleton<DemoDataSeeder>();

        s.AddSingleton<IProgramStudiRepository, ProgramStudiRepository>();
        s.AddSingleton<IDosenRepository, DosenRepository>();
        s.AddSingleton<IMahasiswaRepository, MahasiswaRepository>();
        s.AddSingleton<ITugasAkhirRepository, TugasAkhirRepository>();
        s.AddSingleton<IBimbinganRepository, BimbinganRepository>();
        s.AddSingleton<IJadwalSidangRepository, JadwalSidangRepository>();
        s.AddSingleton<IUserRepository, UserRepository>();
        s.AddSingleton<IActivityLogRepository, ActivityLogRepository>();
        s.AddSingleton<IDashboardRepository, DashboardRepository>();

        s.AddSingleton<UserSession>();
        s.AddSingleton<ActivityLogger>();
        s.AddSingleton<AuthService>();
        s.AddSingleton<ProdiService>();
        s.AddSingleton<DosenService>();
        s.AddSingleton<MahasiswaService>();
        s.AddSingleton<TugasAkhirService>();
        s.AddSingleton<BimbinganService>();
        s.AddSingleton<SidangService>();
        s.AddSingleton<UserService>();
        s.AddSingleton<DashboardService>();

        s.AddSingleton(_ => new ThemeService(Path.Combine(DataFolder, "settings.json")));
        s.AddSingleton<IDialogService, DialogService>();
        s.AddSingleton<NotificationService>();

        s.AddTransient<LoginViewModel>();
        s.AddTransient<MainViewModel>();
        s.AddTransient<DashboardViewModel>();
        s.AddTransient<MahasiswaViewModel>();
        s.AddTransient<DosenViewModel>();
        s.AddTransient<ProdiViewModel>();
        s.AddTransient<TugasAkhirViewModel>();
        s.AddTransient<BimbinganViewModel>();
        s.AddTransient<SidangViewModel>();
        s.AddTransient<PenggunaViewModel>();

        return s.BuildServiceProvider();
    }

    private bool InitializeDatabase()
    {
        try
        {
            var init = _services!.GetRequiredService<DatabaseInitializer>();
            init.EnsureCreated();
            if (init.IsEmpty())
                _services!.GetRequiredService<DemoDataSeeder>().Seed();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Database tidak dapat dibuka atau dibuat.\n\n" + DbErrorTranslator.Translate(ex) +
                $"\n\nLokasi: {Path.Combine(DataFolder, "sita.db")}",
                "SITA – Gagal Memulai", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return false;
        }
    }

    public void ShowLogin()
    {
        var vm = _services!.GetRequiredService<LoginViewModel>();
        var login = new LoginWindow { DataContext = vm };
        vm.LoginSucceeded += (_, _) =>
        {
            ShowMain();
            login.Close();
        };
        login.Closed += (_, _) =>
        {
            if (Windows.Count == 0) Shutdown();
        };
        _services!.GetRequiredService<ThemeService>().ApplyTitleBar(login);
        MainWindow = login;
        login.Show();
    }

    private void ShowMain()
    {
        var vm = _services!.GetRequiredService<MainViewModel>();
        var main = new MainWindow { DataContext = vm };
        if (IsDemoMode) main.Title += " (Mode Demo – data tidak disimpan)";
        vm.LogoutRequested += (_, _) =>
        {
            ShowLogin();
            main.Close();
        };
        main.Closed += (_, _) =>
        {
            if (Windows.Count == 0) Shutdown();
        };
        _services!.GetRequiredService<ThemeService>().ApplyTitleBar(main);
        MainWindow = main;
        main.Show();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Last line of defence: keep the app alive and explain what happened in plain language.
        e.Handled = true;
        _services?.GetService<IDialogService>()?.Error(DbErrorTranslator.Translate(e.Exception));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
