using SITA.Core.Data;
using SITA.Core.Models;
using SITA.Core.Repositories;
using SITA.Core.Services;

namespace SITA.Core.Tests;

/// <summary>A fresh, isolated in-memory database wired up with real repositories and services.</summary>
public sealed class TestContext : IDisposable
{
    private readonly SqliteConnectionFactory _factory;

    public UserSession Session { get; } = new();
    public IPasswordHasher Hasher { get; } = new BCryptPasswordHasher(workFactor: 4);

    public ProgramStudiRepository ProdiRepo { get; }
    public DosenRepository DosenRepo { get; }
    public MahasiswaRepository MahasiswaRepo { get; }
    public TugasAkhirRepository TaRepo { get; }
    public BimbinganRepository BimbinganRepo { get; }
    public JadwalSidangRepository SidangRepo { get; }
    public UserRepository UserRepo { get; }
    public ActivityLogRepository ActivityRepo { get; }
    public DashboardRepository DashboardRepo { get; }

    public AuthService Auth { get; }
    public MahasiswaService Mahasiswa { get; }
    public DosenService Dosen { get; }
    public ProdiService Prodi { get; }
    public TugasAkhirService TugasAkhir { get; }
    public BimbinganService Bimbingan { get; }
    public SidangService Sidang { get; }
    public UserService Users { get; }
    public DashboardService Dashboard { get; }

    public TestContext(bool seed = false)
    {
        _factory = SqliteConnectionFactory.InMemory("test_" + Guid.NewGuid().ToString("N"));
        var init = new DatabaseInitializer(_factory);
        init.EnsureCreated();
        if (seed) new DemoDataSeeder(_factory, Hasher).Seed();

        ProdiRepo = new(_factory);
        DosenRepo = new(_factory);
        MahasiswaRepo = new(_factory);
        TaRepo = new(_factory);
        BimbinganRepo = new(_factory);
        SidangRepo = new(_factory);
        UserRepo = new(_factory);
        ActivityRepo = new(_factory);
        DashboardRepo = new(_factory);

        var logger = new ActivityLogger(ActivityRepo, Session);
        Auth = new(UserRepo, Hasher, Session, logger);
        Mahasiswa = new(MahasiswaRepo, Session, logger);
        Dosen = new(DosenRepo, Session, logger);
        Prodi = new(ProdiRepo, Session, logger);
        TugasAkhir = new(TaRepo, MahasiswaRepo, Session, logger);
        Bimbingan = new(BimbinganRepo, TaRepo, Session, logger);
        Sidang = new(SidangRepo, TaRepo, Session, logger);
        Users = new(UserRepo, DosenRepo, Hasher, Session, logger);
        Dashboard = new(DashboardRepo, ActivityRepo, Session, logger);
    }

    public void SignInAdmin() => Session.SignIn(new User { Id = 1, Username = "admin", NamaLengkap = "Admin", Role = UserRole.Admin });

    public void SignInDosen(int dosenId, int userId = 99) =>
        Session.SignIn(new User { Id = userId, Username = "dosen", NamaLengkap = "Dosen", Role = UserRole.Dosen, DosenId = dosenId });

    // Minimal fixture: 1 prodi, 3 dosen, 1 mahasiswa with a TA supervised by dosen 1.
    public (int ProdiId, int[] DosenIds, int MahasiswaId, int TaId) CreateBasicData()
    {
        int prodiId = ProdiRepo.Insert(new ProgramStudi { Kode = "IF", Nama = "Teknik Informatika" });
        int[] dosenIds = Enumerable.Range(1, 3).Select(i => DosenRepo.Insert(new Dosen
        {
            Nip = $"19800101200501100{i}", Nama = $"Dosen {i}", Email = $"d{i}@kampus.ac.id", ProdiId = prodiId
        })).ToArray();
        int mhsId = MahasiswaRepo.Insert(new Mahasiswa
        {
            Nim = "5025211001", Nama = "Andi Pratama", ProdiId = prodiId, Angkatan = DateTime.Today.Year - 4
        });
        int taId = TaRepo.Insert(new TugasAkhir
        {
            MahasiswaId = mhsId, Judul = "Sistem Informasi Monitoring Tugas Akhir", Pembimbing1Id = dosenIds[0],
            Tahap = TahapTA.Proposal, TanggalMulai = DateTime.Today.AddDays(-60), UpdatedAt = DateTime.Now
        });
        return (prodiId, dosenIds, mhsId, taId);
    }

    public void Dispose() => _factory.Dispose();
}
