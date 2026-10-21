using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace SITA.App.Services;

/// <summary>Switches between light and dark palettes and remembers the choice between sessions.</summary>
public class ThemeService
{
    private readonly string _settingsPath;

    public bool IsDark { get; private set; }

    public event EventHandler? ThemeChanged;

    public ThemeService(string settingsPath) => _settingsPath = settingsPath;

    public void LoadSaved()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsPath));
                Apply(settings?.Theme == "Dark");
                return;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A broken settings file should never stop the app from starting.
        }
        Apply(false);
    }

    public void Toggle()
    {
        Apply(!IsDark);
        Save();
    }

    private void Apply(bool dark)
    {
        IsDark = dark;
        var palette = new ResourceDictionary
        {
            Source = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative)
        };
        Application.Current.Resources.MergedDictionaries[0] = palette;

        foreach (Window window in Application.Current.Windows)
            ApplyTitleBar(window);

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new Settings { Theme = IsDark ? "Dark" : "Light" }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not being able to persist the preference is harmless.
        }
    }

    /// <summary>Matches the native Windows title bar to the current theme.</summary>
    public void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            window.SourceInitialized += (_, _) => ApplyTitleBar(window);
            return;
        }
        int useDark = IsDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref useDark, sizeof(int));
    }

    private const int DwmUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private class Settings
    {
        public string Theme { get; set; } = "Light";
    }
}
