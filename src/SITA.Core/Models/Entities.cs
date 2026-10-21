namespace SITA.Core.Models;

public class ProgramStudi
{
    public int Id { get; set; }
    public string Kode { get; set; } = "";
    public string Nama { get; set; } = "";
    public string Jenjang { get; set; } = "S1";

    // Computed by queries, not stored
    public int JumlahMahasiswa { get; set; }
    public int JumlahDosen { get; set; }

    public ProgramStudi Clone() => (ProgramStudi)MemberwiseClone();
}

public class Dosen
{
    public int Id { get; set; }
    public string Nip { get; set; } = "";
    public string Nama { get; set; } = "";
    public string Email { get; set; } = "";
    public int ProdiId { get; set; }
    public string BidangKeahlian { get; set; } = "";

    public string ProdiNama { get; set; } = "";
    public int JumlahBimbingan { get; set; }

    public Dosen Clone() => (Dosen)MemberwiseClone();
}

public class Mahasiswa
{
    public int Id { get; set; }
    public string Nim { get; set; } = "";
    public string Nama { get; set; } = "";
    public string Email { get; set; } = "";
    public int ProdiId { get; set; }
    public int Angkatan { get; set; }
    public string JenisKelamin { get; set; } = "L";
    public string TempatLahir { get; set; } = "";
    public DateTime? TanggalLahir { get; set; }
    public string Alamat { get; set; } = "";
    public string Telepon { get; set; } = "";
    public int? DosenWaliId { get; set; }
    public StatusMahasiswa Status { get; set; } = StatusMahasiswa.Aktif;

    public string ProdiNama { get; set; } = "";
    public TahapTA? TahapTA { get; set; }

    public string Display => $"{Nim} — {Nama}";
    public string TahapLabel => TahapTA is { } t ? EnumLabels.Of(t) : "Belum TA";
    public string JenisKelaminLabel => JenisKelamin == "P" ? "Perempuan" : "Laki-laki";
    public string StatusLabel => Status switch
    {
        StatusMahasiswa.Aktif => "Aktif",
        StatusMahasiswa.Cuti => "Cuti",
        StatusMahasiswa.Lulus => "Lulus",
        StatusMahasiswa.DropOut => "Drop Out",
        _ => "-"
    };

    public Mahasiswa Clone() => (Mahasiswa)MemberwiseClone();
}

public class TugasAkhir
{
    public int Id { get; set; }
    public int MahasiswaId { get; set; }
    public string Judul { get; set; } = "";
    public string BidangMinat { get; set; } = "";
    public int Pembimbing1Id { get; set; }
    public int? Pembimbing2Id { get; set; }
    public TahapTA Tahap { get; set; }
    public DateTime TanggalMulai { get; set; } = DateTime.Today;
    public DateTime UpdatedAt { get; set; }

    public string Nim { get; set; } = "";
    public string NamaMahasiswa { get; set; } = "";
    public string Pembimbing1Nama { get; set; } = "";
    public string? Pembimbing2Nama { get; set; }
    public int JumlahBimbingan { get; set; }
    public DateTime? BimbinganTerakhir { get; set; }

    public string TahapLabel => EnumLabels.Of(Tahap);
    public string Display => $"{Nim} — {NamaMahasiswa}";
    public string PembimbingDisplay => Pembimbing2Nama is null ? Pembimbing1Nama : $"{Pembimbing1Nama}, {Pembimbing2Nama}";

    public TugasAkhir Clone() => (TugasAkhir)MemberwiseClone();
}

public class Bimbingan
{
    public int Id { get; set; }
    public int TugasAkhirId { get; set; }
    public int DosenId { get; set; }
    public DateTime Tanggal { get; set; } = DateTime.Today;
    public string Topik { get; set; } = "";
    public string Catatan { get; set; } = "";
    public StatusBimbingan Status { get; set; }

    public string Nim { get; set; } = "";
    public string NamaMahasiswa { get; set; } = "";
    public string JudulTA { get; set; } = "";
    public string DosenNama { get; set; } = "";

    public string StatusLabel => EnumLabels.Of(Status);

    public Bimbingan Clone() => (Bimbingan)MemberwiseClone();
}

public class JadwalSidang
{
    public int Id { get; set; }
    public int TugasAkhirId { get; set; }
    public JenisSidang Jenis { get; set; }
    public DateTime Waktu { get; set; } = DateTime.Today.AddDays(7).AddHours(9);
    public string Ruang { get; set; } = "";
    public int Penguji1Id { get; set; }
    public int Penguji2Id { get; set; }
    public HasilSidang Hasil { get; set; }

    public string Nim { get; set; } = "";
    public string NamaMahasiswa { get; set; } = "";
    public string JudulTA { get; set; } = "";
    public string Penguji1Nama { get; set; } = "";
    public string Penguji2Nama { get; set; } = "";

    public string JenisLabel => EnumLabels.Of(Jenis);
    public string HasilLabel => EnumLabels.Of(Hasil);
    public string PengujiDisplay => $"{Penguji1Nama}, {Penguji2Nama}";

    public JadwalSidang Clone() => (JadwalSidang)MemberwiseClone();
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string NamaLengkap { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Dosen;
    public int? DosenId { get; set; }
    public bool IsActive { get; set; } = true;

    public string? DosenNama { get; set; }

    public string RoleLabel => EnumLabels.Of(Role);
    public string StatusLabel => IsActive ? "Aktif" : "Nonaktif";

    public User Clone() => (User)MemberwiseClone();
}

public class ActivityLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "";
    public string Aksi { get; set; } = "";
    public string Entitas { get; set; } = "";
    public string Deskripsi { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
