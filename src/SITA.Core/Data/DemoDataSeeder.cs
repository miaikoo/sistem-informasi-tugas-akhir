using System.Data;
using Dapper;
using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.Core.Data;

/// <summary>
/// Fills an empty database with realistic sample data so the dashboard has something to show.
/// Dates are generated relative to today, so the data always looks "current".
/// </summary>
public class DemoDataSeeder
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "admin123";
    public const string DosenPassword = "dosen123";

    private readonly IDbConnectionFactory _factory;
    private readonly IPasswordHasher _hasher;
    private readonly Random _rng = new(42);

    public DemoDataSeeder(IDbConnectionFactory factory, IPasswordHasher hasher)
    {
        _factory = factory;
        _hasher = hasher;
    }

    private static readonly (string Kode, string Nama, string NimCode)[] Prodi =
    [
        ("IF", "Teknik Informatika", "25"),
        ("SI", "Sistem Informasi", "26"),
        ("TK", "Teknik Komputer", "24"),
        ("TI", "Teknologi Informasi", "27"),
    ];

    private static readonly (string Username, string Nama, int Prodi, string Bidang)[] DosenData =
    [
        ("ahmad", "Dr. Ahmad Fauzi, M.Kom.", 0, "Kecerdasan Buatan"),
        ("rina", "Dr. Rina Kartika, M.T.", 0, "Rekayasa Perangkat Lunak"),
        ("budi", "Budi Santoso, S.Kom., M.Kom.", 0, "Jaringan Komputer"),
        ("dewi", "Dewi Lestari, M.Sc.", 1, "Sistem Pendukung Keputusan"),
        ("hendra", "Prof. Hendra Wijaya, Ph.D.", 0, "Visi Komputer"),
        ("siti", "Siti Nurhaliza, M.Kom.", 1, "Data Mining"),
        ("agus", "Agus Prasetyo, M.T.", 2, "Internet of Things"),
        ("maya", "Maya Anggraini, M.Cs.", 3, "Keamanan Informasi"),
        ("rudi", "Rudi Hartono, M.Kom.", 2, "Sistem Tertanam"),
        ("lina", "Lina Marlina, M.MSI.", 1, "Tata Kelola TI"),
    ];

    private static readonly string[] NamaDepan =
    [
        "Andi", "Bayu", "Citra", "Dimas", "Eka", "Fajar", "Gita", "Hana", "Ilham", "Joko",
        "Kevin", "Laras", "Mega", "Nanda", "Oki", "Putri", "Raka", "Salsa", "Tegar", "Umi",
        "Vina", "Wahyu", "Yoga", "Zahra", "Arif", "Bella", "Dina", "Farhan", "Intan", "Rizky"
    ];

    private static readonly string[] NamaBelakang =
    [
        "Pratama", "Saputra", "Wulandari", "Nugroho", "Permata", "Hidayat", "Rahmawati", "Kurniawan",
        "Puspita", "Firmansyah", "Maharani", "Setiawan", "Anggraeni", "Ramadhan", "Safitri"
    ];

    private static readonly string[] Metode =
    [
        "Penerapan Convolutional Neural Network untuk",
        "Implementasi Algoritma Random Forest pada",
        "Rancang Bangun Aplikasi Mobile untuk",
        "Analisis Sentimen Berbasis IndoBERT terhadap",
        "Sistem Pendukung Keputusan Metode AHP untuk",
        "Pengembangan Sistem Berbasis IoT untuk",
        "Optimasi Algoritma Genetika pada",
        "Evaluasi Keamanan Aplikasi Web pada",
        "Penerapan Metode K-Means untuk",
        "Rancang Bangun Sistem Informasi untuk",
        "Implementasi Blockchain untuk",
    ];

    private static readonly string[] Domain =
    [
        "Klasifikasi Penyakit Daun Padi",
        "Prediksi Kelulusan Mahasiswa",
        "Monitoring Kualitas Air Tambak",
        "Ulasan Aplikasi Transportasi Daring",
        "Pemilihan Lokasi UMKM",
        "Penjadwalan Mata Kuliah",
        "Layanan Perpustakaan Digital",
        "Segmentasi Pelanggan E-Commerce",
        "Pelacakan Distribusi Logistik",
        "Deteksi Kendaraan di Persimpangan",
        "Pengelolaan Rekam Medis Klinik",
    ];

    private static readonly string[] BidangMinat =
    [
        "Kecerdasan Buatan", "Rekayasa Perangkat Lunak", "Data Science", "Internet of Things",
        "Keamanan Informasi", "Sistem Informasi", "Jaringan Komputer", "Visi Komputer"
    ];

    private static readonly string[][] TopikPerTahap =
    [
        ["Diskusi ide topik", "Pemilihan judul", "Studi literatur awal"],
        ["Penyusunan Bab 1", "Revisi latar belakang", "Penyusunan tinjauan pustaka", "Metodologi penelitian"],
        ["Perancangan sistem", "Pengumpulan dataset", "Implementasi modul utama", "Review progres implementasi"],
        ["Pengujian sistem", "Analisis hasil", "Penyusunan Bab 4", "Persiapan seminar hasil"],
        ["Revisi pasca seminar hasil", "Penyusunan kesimpulan", "Persiapan sidang akhir"],
        ["Revisi akhir buku TA", "Finalisasi dokumen"],
    ];

    private static readonly string[] Ruang = ["Ruang Sidang 1", "Ruang Sidang 2", "Lab RPL", "Lab KCV", "IF-105"];
    private static readonly int[] JamSlot = [8, 10, 13, 15];

    public void Seed()
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();

        var prodiIds = Prodi.Select(p => conn.ExecuteScalar<int>(
            "INSERT INTO ProgramStudi (Kode, Nama, Jenjang) VALUES (@Kode, @Nama, 'S1'); SELECT last_insert_rowid();",
            new { p.Kode, p.Nama }, tx)).ToArray();

        var dosenIds = new int[DosenData.Length];
        for (int i = 0; i < DosenData.Length; i++)
        {
            var d = DosenData[i];
            int n = i + 1;
            string nip = $"{1968 + n}{n % 12 + 1:00}{n * 3 % 28 + 1:00}{1998 + n}{n % 12 + 1:00}{n % 2 + 1}{n:000}";
            dosenIds[i] = conn.ExecuteScalar<int>(
                """
                INSERT INTO Dosen (Nip, Nama, Email, ProdiId, BidangKeahlian)
                VALUES (@Nip, @Nama, @Email, @ProdiId, @Bidang); SELECT last_insert_rowid();
                """,
                new { Nip = nip, d.Nama, Email = $"{d.Username}@kampus.ac.id", ProdiId = prodiIds[d.Prodi], d.Bidang }, tx);
        }

        SeedUsers(conn, tx, dosenIds);

        var today = DateTime.Today;
        var usedSlots = new HashSet<(string, DateTime)>();
        int taCounter = 0;

        int[] angkatanList = [today.Year - 5, today.Year - 4, today.Year - 3];
        int seq = 1;
        foreach (var angkatan in angkatanList)
        {
            for (int k = 0; k < 20; k++, seq++)
            {
                int prodiIdx = seq % Prodi.Length;
                string nama = $"{NamaDepan[seq % NamaDepan.Length]} {NamaBelakang[seq * 7 % NamaBelakang.Length]}";
                string nim = $"50{Prodi[prodiIdx].NimCode}{angkatan % 100:00}1{seq:000}";
                int mhsId = conn.ExecuteScalar<int>(
                    """
                    INSERT INTO Mahasiswa (Nim, Nama, Email, ProdiId, Angkatan)
                    VALUES (@Nim, @Nama, @Email, @ProdiId, @Angkatan); SELECT last_insert_rowid();
                    """,
                    new { Nim = nim, Nama = nama, Email = $"{nim}@student.kampus.ac.id", ProdiId = prodiIds[prodiIdx], Angkatan = angkatan }, tx);

                TahapTA? tahap = PickTahap(angkatan - angkatanList[0], k);
                if (tahap is null) continue;

                SeedTugasAkhir(conn, tx, mhsId, tahap.Value, dosenIds, taCounter++, today, usedSlots);
            }
        }

        conn.Execute(
            "INSERT INTO ActivityLog (UserId, Username, Aksi, Entitas, Deskripsi, CreatedAt) VALUES (NULL, 'sistem', 'Tambah', 'Sistem', @D, @T)",
            new { D = "Inisialisasi database dengan data contoh", T = DateTime.Now }, tx);

        tx.Commit();
    }

    private void SeedUsers(IDbConnection conn, IDbTransaction tx, int[] dosenIds)
    {
        conn.Execute(
            "INSERT INTO Users (Username, PasswordHash, NamaLengkap, Role, DosenId, IsActive) VALUES (@U, @H, @N, @R, NULL, 1)",
            new { U = AdminUsername, H = _hasher.Hash(AdminPassword), N = "Administrator Prodi", R = (int)UserRole.Admin }, tx);

        for (int i = 0; i < DosenData.Length; i++)
        {
            conn.Execute(
                "INSERT INTO Users (Username, PasswordHash, NamaLengkap, Role, DosenId, IsActive) VALUES (@U, @H, @N, @R, @D, 1)",
                new { U = DosenData[i].Username, H = _hasher.Hash(DosenPassword), N = DosenData[i].Nama, R = (int)UserRole.Dosen, D = dosenIds[i] }, tx);
        }
    }

    /// <summary>Older cohorts are further along; the youngest cohort has only a few students starting TA.</summary>
    private TahapTA? PickTahap(int cohortIndex, int k) => cohortIndex switch
    {
        0 => (TahapTA)(2 + k % 4),                         // Penelitian .. Lulus
        1 => k < 18 ? (TahapTA)(k % 4) : null,             // Pengajuan .. Seminar Hasil
        _ => k < 6 ? (TahapTA)(k % 2) : null,              // Pengajuan .. Proposal
    };

    private void SeedTugasAkhir(IDbConnection conn, IDbTransaction tx, int mhsId, TahapTA tahap, int[] dosenIds,
        int index, DateTime today, HashSet<(string, DateTime)> usedSlots)
    {
        int p1 = dosenIds[_rng.Next(dosenIds.Length)];
        int? p2 = _rng.NextDouble() < 0.6 ? dosenIds.Where(d => d != p1).ElementAt(_rng.Next(dosenIds.Length - 1)) : null;

        int t = (int)tahap;
        var mulai = today.AddDays(-(45 * (t + 1) + _rng.Next(0, 30)));
        string judul = $"{Metode[index % Metode.Length]} {Domain[index * 7 % Domain.Length]}";

        int taId = conn.ExecuteScalar<int>(
            """
            INSERT INTO TugasAkhir (MahasiswaId, Judul, BidangMinat, Pembimbing1Id, Pembimbing2Id, Tahap, TanggalMulai, UpdatedAt)
            VALUES (@MahasiswaId, @Judul, @BidangMinat, @P1, @P2, @Tahap, @Mulai, @Updated); SELECT last_insert_rowid();
            """,
            new
            {
                MahasiswaId = mhsId, Judul = judul, BidangMinat = BidangMinat[index % BidangMinat.Length],
                P1 = p1, P2 = p2, Tahap = t, Mulai = mulai, Updated = today.AddDays(-_rng.Next(0, 20))
            }, tx);

        // Log bimbingan spread between the start date and today
        int count = 2 + t * 2 + _rng.Next(0, 3);
        int span = Math.Max(1, (today - mulai).Days);
        var dates = Enumerable.Range(0, count).Select(_ => mulai.AddDays(_rng.Next(0, span + 1))).OrderBy(d => d).ToList();
        for (int i = 0; i < dates.Count; i++)
        {
            int topikTahap = Math.Min(t, i * (t + 1) / count);
            var topikList = TopikPerTahap[topikTahap];
            bool latest = i == dates.Count - 1;
            var status = latest && tahap != TahapTA.Lulus
                ? (StatusBimbingan)_rng.Next(0, 3)
                : (_rng.NextDouble() < 0.85 ? StatusBimbingan.Disetujui : StatusBimbingan.Revisi);

            conn.Execute(
                """
                INSERT INTO Bimbingan (TugasAkhirId, DosenId, Tanggal, Topik, Catatan, Status)
                VALUES (@TaId, @DosenId, @Tanggal, @Topik, @Catatan, @Status)
                """,
                new
                {
                    TaId = taId, DosenId = p2 is not null && _rng.NextDouble() < 0.35 ? p2.Value : p1, Tanggal = dates[i],
                    Topik = topikList[_rng.Next(topikList.Length)],
                    Catatan = status == StatusBimbingan.Revisi ? "Perbaiki sesuai catatan pada dokumen." : "Lanjutkan ke tahap berikutnya.",
                    Status = (int)status
                }, tx);
        }

        // Sidang history (past, passed) and upcoming sessions depending on the current stage
        if (t >= (int)TahapTA.Penelitian)
            AddSidang(conn, tx, taId, JenisSidang.SeminarProposal, Past(mulai, today, 0.35), HasilSidang.Lulus, p1, p2, dosenIds, usedSlots);
        if (t >= (int)TahapTA.Sidang)
            AddSidang(conn, tx, taId, JenisSidang.SeminarHasil, Past(mulai, today, 0.75), HasilSidang.LulusDenganRevisi, p1, p2, dosenIds, usedSlots);
        if (tahap == TahapTA.Lulus)
            AddSidang(conn, tx, taId, JenisSidang.SidangAkhir, Past(mulai, today, 0.92), HasilSidang.Lulus, p1, p2, dosenIds, usedSlots);
        if (tahap == TahapTA.Sidang)
            AddSidang(conn, tx, taId, JenisSidang.SidangAkhir, today.AddDays(_rng.Next(1, 30)), HasilSidang.BelumDilaksanakan, p1, p2, dosenIds, usedSlots);
        if (tahap == TahapTA.SeminarHasil && _rng.NextDouble() < 0.6)
            AddSidang(conn, tx, taId, JenisSidang.SeminarHasil, today.AddDays(_rng.Next(1, 45)), HasilSidang.BelumDilaksanakan, p1, p2, dosenIds, usedSlots);
        if (tahap == TahapTA.Proposal && _rng.NextDouble() < 0.5)
            AddSidang(conn, tx, taId, JenisSidang.SeminarProposal, today.AddDays(_rng.Next(1, 30)), HasilSidang.BelumDilaksanakan, p1, p2, dosenIds, usedSlots);
    }

    private static DateTime Past(DateTime start, DateTime today, double fraction) =>
        start.AddDays((int)((today - start).Days * fraction));

    private void AddSidang(IDbConnection conn, IDbTransaction tx, int taId, JenisSidang jenis, DateTime day, HasilSidang hasil,
        int p1, int? p2, int[] dosenIds, HashSet<(string, DateTime)> usedSlots)
    {
        if (day.DayOfWeek == DayOfWeek.Saturday) day = day.AddDays(2);
        if (day.DayOfWeek == DayOfWeek.Sunday) day = day.AddDays(1);

        string ruang;
        DateTime waktu;
        do
        {
            ruang = Ruang[_rng.Next(Ruang.Length)];
            waktu = day.Date.AddHours(JamSlot[_rng.Next(JamSlot.Length)]);
        } while (!usedSlots.Add((ruang, waktu)));

        var penguji = dosenIds.Where(d => d != p1 && d != p2).OrderBy(_ => _rng.Next()).Take(2).ToArray();
        conn.Execute(
            """
            INSERT INTO JadwalSidang (TugasAkhirId, Jenis, Waktu, Ruang, Penguji1Id, Penguji2Id, Hasil)
            VALUES (@TaId, @Jenis, @Waktu, @Ruang, @P1, @P2, @Hasil)
            """,
            new { TaId = taId, Jenis = (int)jenis, Waktu = waktu, Ruang = ruang, P1 = penguji[0], P2 = penguji[1], Hasil = (int)hasil }, tx);
    }
}
