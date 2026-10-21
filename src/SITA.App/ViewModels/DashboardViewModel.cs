using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SITA.App.Services;
using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.App.ViewModels;

/// <summary>One bar of a bar chart; the two GridLengths split the track into filled and empty parts.</summary>
public record ChartBar(string Label, int Value, GridLength Filled, GridLength Empty)
{
    public static List<ChartBar> From(IEnumerable<ChartPoint> points)
    {
        var list = points.ToList();
        int max = list.Count == 0 ? 0 : list.Max(p => p.Value);
        return list.Select(p =>
        {
            double ratio = max == 0 ? 0 : (double)p.Value / max;
            return new ChartBar(p.Label, p.Value, new GridLength(ratio, GridUnitType.Star), new GridLength(1 - ratio, GridUnitType.Star));
        }).ToList();
    }
}

public partial class DashboardViewModel : PageViewModel
{
    private readonly DashboardService _service;
    private readonly IDialogService _dialog;
    private readonly UserSession _session;

    public DashboardViewModel(DashboardService service, IDialogService dialog, UserSession session)
    {
        _service = service;
        _dialog = dialog;
        _session = session;
    }

    public override string Title => Greeting + ", " + FirstName;
    public override string Subtitle => _session.IsAdmin
        ? "Ringkasan progres tugas akhir seluruh program studi"
        : "Ringkasan progres mahasiswa bimbingan Anda";

    public bool IsAdmin => _session.IsAdmin;

    [ObservableProperty] private DashboardSummary _summary = new();
    [ObservableProperty] private List<ChartBar> _tahapBars = [];
    [ObservableProperty] private List<ChartBar> _bulanBars = [];
    [ObservableProperty] private List<ChartBar> _bebanBars = [];
    [ObservableProperty] private IReadOnlyList<JadwalSidang> _upcomingSidang = [];
    [ObservableProperty] private IReadOnlyList<ActivityLog> _activities = [];

    public string BimbinganTrend => Summary.BimbinganBulanLalu == 0
        ? "Belum ada data bulan lalu"
        : $"{(Summary.BimbinganBulanIni >= Summary.BimbinganBulanLalu ? "▲" : "▼")} bulan lalu: {Summary.BimbinganBulanLalu}";

    public string TaCaption => _session.IsAdmin
        ? $"dari {Summary.TotalMahasiswa} mahasiswa terdaftar"
        : $"dari {Summary.TotalMahasiswa} mahasiswa bimbingan";

    public string LulusCaption => Summary.TotalMahasiswa == 0 || !_session.IsAdmin
        ? "telah menyelesaikan TA"
        : $"{Summary.TotalDosen} dosen pembimbing aktif";

    partial void OnSummaryChanged(DashboardSummary value)
    {
        OnPropertyChanged(nameof(BimbinganTrend));
        OnPropertyChanged(nameof(TaCaption));
        OnPropertyChanged(nameof(LulusCaption));
    }

    private static string Greeting => DateTime.Now.Hour switch
    {
        < 11 => "Selamat pagi",
        < 15 => "Selamat siang",
        < 18 => "Selamat sore",
        _ => "Selamat malam"
    };

    private string FirstName
    {
        get
        {
            var words = (_session.CurrentUser?.NamaLengkap ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !w.EndsWith('.')).ToArray();
            return words.FirstOrDefault()?.TrimEnd(',') ?? "";
        }
    }

    public override void Load()
    {
        try
        {
            var data = _service.Load();
            Summary = data.Summary;
            TahapBars = ChartBar.From(data.TahapDistribution);
            BulanBars = ChartBar.From(data.BimbinganPerBulan);
            BebanBars = ChartBar.From(data.BebanDosen.Select(p => p with { Label = ShortName(p.Label) }));
            UpcomingSidang = data.UpcomingSidang;
            Activities = data.RecentActivity;
        }
        catch (Exception ex)
        {
            _dialog.Error(DbErrorTranslator.Translate(ex));
        }
    }

    /// <summary>"Dr. Ahmad Fauzi, M.Kom." → "Ahmad Fauzi" so labels fit the chart.</summary>
    private static string ShortName(string fullName)
    {
        var name = fullName.Split(',')[0];
        return string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !w.EndsWith('.')));
    }
}
