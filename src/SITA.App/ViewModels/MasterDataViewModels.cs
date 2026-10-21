using CommunityToolkit.Mvvm.ComponentModel;
using SITA.App.Services;
using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.App.ViewModels;

public partial class MahasiswaViewModel : CrudPageViewModel<Mahasiswa>
{
    private readonly MahasiswaService _service;
    private readonly ProdiService _prodi;

    public MahasiswaViewModel(MahasiswaService service, ProdiService prodi, IDialogService dialog,
        NotificationService notifications, UserSession session) : base(dialog, notifications, session)
    {
        _service = service;
        _prodi = prodi;
    }

    public override string Title => "Mahasiswa";
    public override string Subtitle => "Kelola data mahasiswa beserta status tugas akhirnya";
    protected override string EntityName => "Mahasiswa";

    [ObservableProperty] private List<ProgramStudi> _prodiList = [];
    [ObservableProperty] private List<Option<int?>> _prodiFilterOptions = [];
    [ObservableProperty] private List<Option<int?>> _angkatanFilterOptions = [];
    [ObservableProperty] private Option<int?>? _prodiFilter;
    [ObservableProperty] private Option<int?>? _angkatanFilter;

    partial void OnProdiFilterChanged(Option<int?>? value) => FilterChanged();
    partial void OnAngkatanFilterChanged(Option<int?>? value) => FilterChanged();

    protected override void LoadLookups()
    {
        ProdiList = _prodi.Search().ToList();
        ProdiFilterOptions = [new(null, "Semua prodi"), .. ProdiList.Select(p => new Option<int?>(p.Id, p.Nama))];
        AngkatanFilterOptions = [new(null, "Semua angkatan"), .. _service.GetAngkatanList().Select(a => new Option<int?>(a, a.ToString()))];
        ProdiFilter = ProdiFilterOptions[0];
        AngkatanFilter = AngkatanFilterOptions[0];
    }

    protected override IEnumerable<Mahasiswa> Query() => _service.Search(SearchText, ProdiFilter?.Value, AngkatanFilter?.Value);

    protected override Mahasiswa CreateNew() => new()
    {
        Angkatan = DateTime.Today.Year,
        ProdiId = ProdiFilter?.Value ?? ProdiList.FirstOrDefault()?.Id ?? 0
    };

    protected override Mahasiswa CloneForEdit(Mahasiswa item) => item.Clone();
    protected override OperationResult SaveItem(Mahasiswa item) => _service.Save(item);
    protected override OperationResult DeleteItem(Mahasiswa item) => _service.Delete(item);
    protected override string Describe(Mahasiswa item) => $"Mahasiswa {item.Nim} – {item.Nama}";
}

public partial class DosenViewModel : CrudPageViewModel<Dosen>
{
    private readonly DosenService _service;
    private readonly ProdiService _prodi;

    public DosenViewModel(DosenService service, ProdiService prodi, IDialogService dialog,
        NotificationService notifications, UserSession session) : base(dialog, notifications, session)
    {
        _service = service;
        _prodi = prodi;
    }

    public override string Title => "Dosen";
    public override string Subtitle => "Kelola data dosen pembimbing dan penguji";
    protected override string EntityName => "Dosen";

    [ObservableProperty] private List<ProgramStudi> _prodiList = [];
    [ObservableProperty] private List<Option<int?>> _prodiFilterOptions = [];
    [ObservableProperty] private Option<int?>? _prodiFilter;

    partial void OnProdiFilterChanged(Option<int?>? value) => FilterChanged();

    protected override void LoadLookups()
    {
        ProdiList = _prodi.Search().ToList();
        ProdiFilterOptions = [new(null, "Semua prodi"), .. ProdiList.Select(p => new Option<int?>(p.Id, p.Nama))];
        ProdiFilter = ProdiFilterOptions[0];
    }

    protected override IEnumerable<Dosen> Query() => _service.Search(SearchText, ProdiFilter?.Value);
    protected override Dosen CreateNew() => new() { ProdiId = ProdiFilter?.Value ?? ProdiList.FirstOrDefault()?.Id ?? 0 };
    protected override Dosen CloneForEdit(Dosen item) => item.Clone();
    protected override OperationResult SaveItem(Dosen item) => _service.Save(item);
    protected override OperationResult DeleteItem(Dosen item) => _service.Delete(item);
    protected override string Describe(Dosen item) => $"Dosen {item.Nama}";
}

public partial class ProdiViewModel : CrudPageViewModel<ProgramStudi>
{
    private readonly ProdiService _service;

    public ProdiViewModel(ProdiService service, IDialogService dialog, NotificationService notifications, UserSession session)
        : base(dialog, notifications, session) => _service = service;

    public override string Title => "Program Studi";
    public override string Subtitle => "Master data program studi";
    protected override string EntityName => "Program Studi";

    public List<string> JenjangOptions { get; } = ["D3", "D4", "S1", "S2", "S3"];

    protected override IEnumerable<ProgramStudi> Query() => _service.Search(SearchText);
    protected override ProgramStudi CreateNew() => new();
    protected override ProgramStudi CloneForEdit(ProgramStudi item) => item.Clone();
    protected override OperationResult SaveItem(ProgramStudi item) => _service.Save(item);
    protected override OperationResult DeleteItem(ProgramStudi item) => _service.Delete(item);
    protected override string Describe(ProgramStudi item) => $"Program studi {item.Kode} – {item.Nama}";
}

public partial class PenggunaViewModel : CrudPageViewModel<User>
{
    private readonly UserService _service;
    private readonly DosenService _dosen;

    public PenggunaViewModel(UserService service, DosenService dosen, IDialogService dialog,
        NotificationService notifications, UserSession session) : base(dialog, notifications, session)
    {
        _service = service;
        _dosen = dosen;
    }

    public override string Title => "Pengguna";
    public override string Subtitle => "Kelola akun, peran, dan akses aplikasi";
    protected override string EntityName => "Pengguna";

    public List<Option<UserRole>> RoleOptions { get; } = Option<UserRole>.FromEnum<UserRole>();

    [ObservableProperty] private List<Dosen> _dosenList = [];
    [ObservableProperty] private UserRole _formRole;

    /// <summary>Set from the form's PasswordBox; cleared every time the form opens.</summary>
    public string FormPassword { get; set; } = "";

    public bool IsDosenRole => FormRole == UserRole.Dosen;

    public event EventHandler? PasswordResetRequested;

    partial void OnFormRoleChanged(UserRole value)
    {
        if (Editor is not null) Editor.Role = value;
        OnPropertyChanged(nameof(IsDosenRole));
    }

    protected override void LoadLookups() => DosenList = _dosen.Search().ToList();

    protected override void OnFormOpening(User editor, bool isNew)
    {
        FormPassword = "";
        PasswordResetRequested?.Invoke(this, EventArgs.Empty);
        FormRole = editor.Role;
    }

    protected override IEnumerable<User> Query() => _service.Search(SearchText);
    protected override User CreateNew() => new() { Role = UserRole.Dosen, IsActive = true };
    protected override User CloneForEdit(User item) => item.Clone();

    protected override OperationResult SaveItem(User item)
    {
        item.Role = FormRole;
        return _service.Save(item, FormPassword);
    }

    protected override OperationResult DeleteItem(User item) => _service.Delete(item);
    protected override string Describe(User item) => $"Akun {item.Username}";
}
