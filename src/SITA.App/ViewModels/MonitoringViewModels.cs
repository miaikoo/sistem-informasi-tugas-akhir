using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SITA.App.Services;
using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.App.ViewModels;

public partial class TugasAkhirViewModel : CrudPageViewModel<TugasAkhir>
{
    private const int NoPembimbing2 = 0;

    private readonly TugasAkhirService _service;
    private readonly MahasiswaService _mahasiswa;
    private readonly DosenService _dosen;

    public TugasAkhirViewModel(TugasAkhirService service, MahasiswaService mahasiswa, DosenService dosen,
        IDialogService dialog, NotificationService notifications, UserSession session) : base(dialog, notifications, session)
    {
        _service = service;
        _mahasiswa = mahasiswa;
        _dosen = dosen;
    }

    public override string Title => Session.IsAdmin ? "Tugas Akhir" : "Mahasiswa Bimbingan";
    public override string Subtitle => Session.IsAdmin
        ? "Pantau judul, pembimbing, dan tahap tugas akhir setiap mahasiswa"
        : "Perbarui judul dan tahap TA mahasiswa yang Anda bimbing";
    protected override string EntityName => "Tugas Akhir";

    public override bool CanModify => true; // lecturers may edit their own students; the service enforces it
    public override bool CanDelete => Session.IsAdmin;
    public bool IsAdmin => Session.IsAdmin;

    public List<Option<TahapTA>> TahapOptions { get; } = Option<TahapTA>.FromEnum<TahapTA>();
    public List<Option<TahapTA?>> TahapFilterOptions { get; } = Option<TahapTA?>.FilterFromEnum<TahapTA>("Semua tahap");

    [ObservableProperty] private Option<TahapTA?>? _tahapFilter;
    [ObservableProperty] private Option<int?>? _pembimbingFilter;
    [ObservableProperty] private List<Option<int?>> _pembimbingFilterOptions = [];
    [ObservableProperty] private List<Dosen> _dosenList = [];
    [ObservableProperty] private List<Option<int>> _pembimbing2Options = [];
    [ObservableProperty] private List<Mahasiswa> _mahasiswaOptions = [];
    [ObservableProperty] private int _pembimbing2Selection;

    partial void OnTahapFilterChanged(Option<TahapTA?>? value) => FilterChanged();
    partial void OnPembimbingFilterChanged(Option<int?>? value) => FilterChanged();

    protected override void LoadLookups()
    {
        DosenList = _dosen.Search().ToList();
        Pembimbing2Options = [new(NoPembimbing2, "— Tidak ada —"), .. DosenList.Select(d => new Option<int>(d.Id, d.Nama))];
        PembimbingFilterOptions = [new(null, "Semua pembimbing"), .. DosenList.Select(d => new Option<int?>(d.Id, d.Nama))];
        TahapFilter = TahapFilterOptions[0];
        PembimbingFilter = PembimbingFilterOptions[0];
    }

    protected override IEnumerable<TugasAkhir> Query() =>
        _service.Search(SearchText, TahapFilter?.Value, PembimbingFilter?.Value);

    protected override void OnFormOpening(TugasAkhir editor, bool isNew)
    {
        // A new TA can only be created for students that don't have one yet; when editing, the student is fixed.
        MahasiswaOptions = isNew
            ? _mahasiswa.GetWithoutTugasAkhir().ToList()
            : [new Mahasiswa { Id = editor.MahasiswaId, Nim = editor.Nim, Nama = editor.NamaMahasiswa }];
        Pembimbing2Selection = editor.Pembimbing2Id ?? NoPembimbing2;
    }

    protected override TugasAkhir CreateNew() => new() { TanggalMulai = DateTime.Today, Tahap = TahapTA.Pengajuan };
    protected override TugasAkhir CloneForEdit(TugasAkhir item) => item.Clone();

    protected override OperationResult SaveItem(TugasAkhir item)
    {
        item.Pembimbing2Id = Pembimbing2Selection == NoPembimbing2 ? null : Pembimbing2Selection;
        return _service.Save(item);
    }

    protected override OperationResult DeleteItem(TugasAkhir item) => _service.Delete(item);
    protected override string Describe(TugasAkhir item) =>
        $"Tugas akhir {item.NamaMahasiswa} beserta seluruh log bimbingan dan jadwal sidangnya";
}

public partial class BimbinganViewModel : CrudPageViewModel<Bimbingan>
{
    private readonly BimbinganService _service;
    private readonly TugasAkhirService _ta;

    public BimbinganViewModel(BimbinganService service, TugasAkhirService ta, IDialogService dialog,
        NotificationService notifications, UserSession session) : base(dialog, notifications, session)
    {
        _service = service;
        _ta = ta;
    }

    public override string Title => "Log Bimbingan";
    public override string Subtitle => "Catatan setiap sesi bimbingan tugas akhir";
    protected override string EntityName => "Bimbingan";

    public override bool CanCreate => true;
    public override bool CanModify => true;
    public bool IsAdmin => Session.IsAdmin;

    public List<Option<StatusBimbingan>> StatusOptions { get; } = Option<StatusBimbingan>.FromEnum<StatusBimbingan>();
    public List<Option<StatusBimbingan?>> StatusFilterOptions { get; } =
        Option<StatusBimbingan?>.FilterFromEnum<StatusBimbingan>("Semua status");

    [ObservableProperty] private Option<StatusBimbingan?>? _statusFilter;
    [ObservableProperty] private List<TugasAkhir> _taOptions = [];
    [ObservableProperty] private List<Option<int>> _pembimbingOptions = [];
    [ObservableProperty] private int _selectedTaId;
    [ObservableProperty] private int _selectedDosenId;

    partial void OnStatusFilterChanged(Option<StatusBimbingan?>? value) => FilterChanged();

    /// <summary>The "dicatat oleh" list only offers the supervisors of the chosen TA.</summary>
    partial void OnSelectedTaIdChanged(int value)
    {
        var ta = TaOptions.FirstOrDefault(t => t.Id == value);
        var options = new List<Option<int>>();
        if (ta is not null)
        {
            options.Add(new(ta.Pembimbing1Id, ta.Pembimbing1Nama + " (Pembimbing 1)"));
            if (ta.Pembimbing2Id is { } p2) options.Add(new(p2, ta.Pembimbing2Nama + " (Pembimbing 2)"));
        }
        PembimbingOptions = options;
        if (options.All(o => o.Value != SelectedDosenId))
            SelectedDosenId = options.FirstOrDefault()?.Value ?? 0;
    }

    protected override void LoadLookups() => StatusFilter = StatusFilterOptions[0];

    protected override IEnumerable<Bimbingan> Query() => _service.Search(SearchText, StatusFilter?.Value);

    protected override void OnFormOpening(Bimbingan editor, bool isNew)
    {
        TaOptions = _ta.Search().Where(t => t.Tahap != TahapTA.Lulus || t.Id == editor.TugasAkhirId).ToList();
        SelectedDosenId = editor.DosenId;
        SelectedTaId = 0;
        SelectedTaId = editor.TugasAkhirId;
    }

    protected override Bimbingan CreateNew() => new() { Tanggal = DateTime.Today, Status = StatusBimbingan.Menunggu };
    protected override Bimbingan CloneForEdit(Bimbingan item) => item.Clone();

    protected override OperationResult SaveItem(Bimbingan item)
    {
        item.TugasAkhirId = SelectedTaId;
        item.DosenId = SelectedDosenId;
        return _service.Save(item);
    }

    protected override OperationResult DeleteItem(Bimbingan item) => _service.Delete(item);
    protected override string Describe(Bimbingan item) =>
        $"Catatan bimbingan {item.NamaMahasiswa} tanggal {item.Tanggal:dd/MM/yyyy}";
}

public partial class SidangViewModel : CrudPageViewModel<JadwalSidang>
{
    private readonly SidangService _service;
    private readonly TugasAkhirService _ta;
    private readonly DosenService _dosen;

    public SidangViewModel(SidangService service, TugasAkhirService ta, DosenService dosen, IDialogService dialog,
        NotificationService notifications, UserSession session) : base(dialog, notifications, session)
    {
        _service = service;
        _ta = ta;
        _dosen = dosen;
    }

    public override string Title => "Jadwal Sidang";
    public override string Subtitle => Session.IsAdmin
        ? "Jadwalkan seminar proposal, seminar hasil, dan sidang akhir"
        : "Jadwal sidang yang melibatkan Anda sebagai pembimbing atau penguji";
    protected override string EntityName => "Jadwal Sidang";

    public List<Option<JenisSidang>> JenisOptions { get; } = Option<JenisSidang>.FromEnum<JenisSidang>();
    public List<Option<HasilSidang>> HasilOptions { get; } = Option<HasilSidang>.FromEnum<HasilSidang>();
    public List<string> RuangOptions { get; } = ["Ruang Sidang 1", "Ruang Sidang 2", "Lab RPL", "Lab KCV", "IF-105"];
    public List<string> JamOptions { get; } =
        Enumerable.Range(0, 21).Select(i => TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(30 * i)).ToString(@"hh\:mm")).ToList();

    [ObservableProperty] private bool _upcomingOnly = true;
    [ObservableProperty] private List<TugasAkhir> _taOptions = [];
    [ObservableProperty] private List<Dosen> _dosenList = [];
    [ObservableProperty] private DateTime? _formTanggal;
    [ObservableProperty] private string _formJam = "09:00";

    partial void OnUpcomingOnlyChanged(bool value) => FilterChanged();

    protected override void LoadLookups()
    {
        if (Session.IsAdmin) DosenList = _dosen.Search().ToList();
    }

    protected override IEnumerable<JadwalSidang> Query() => _service.Search(SearchText, UpcomingOnly);

    protected override void OnFormOpening(JadwalSidang editor, bool isNew)
    {
        TaOptions = _ta.Search().Where(t => t.Tahap != TahapTA.Lulus || t.Id == editor.TugasAkhirId).ToList();
        FormTanggal = editor.Waktu.Date;
        FormJam = editor.Waktu.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    protected override JadwalSidang CreateNew() => new()
    {
        Jenis = JenisSidang.SeminarProposal,
        Waktu = DateTime.Today.AddDays(7).AddHours(9),
        Hasil = HasilSidang.BelumDilaksanakan
    };

    protected override JadwalSidang CloneForEdit(JadwalSidang item) => item.Clone();

    protected override OperationResult SaveItem(JadwalSidang item)
    {
        if (FormTanggal is null) return OperationResult.Fail("Tanggal sidang wajib diisi.");
        if (!TimeSpan.TryParseExact(FormJam, @"hh\:mm", CultureInfo.InvariantCulture, out var jam))
            return OperationResult.Fail("Format jam harus JJ:MM, misalnya 09:30.");
        item.Waktu = FormTanggal.Value.Date + jam;
        return _service.Save(item);
    }

    protected override OperationResult DeleteItem(JadwalSidang item) => _service.Delete(item);
    protected override string Describe(JadwalSidang item) =>
        $"{item.JenisLabel} {item.NamaMahasiswa} pada {item.Waktu:dd/MM/yyyy HH:mm}";
}
