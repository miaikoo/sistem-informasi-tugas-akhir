namespace SITA.Core.Models;

public enum UserRole
{
    Admin = 1,
    Dosen = 2
}

public enum TahapTA
{
    Pengajuan = 0,
    Proposal = 1,
    Penelitian = 2,
    SeminarHasil = 3,
    Sidang = 4,
    Lulus = 5
}

public enum StatusBimbingan
{
    Menunggu = 0,
    Disetujui = 1,
    Revisi = 2
}

public enum JenisSidang
{
    SeminarProposal = 0,
    SeminarHasil = 1,
    SidangAkhir = 2
}

public enum HasilSidang
{
    BelumDilaksanakan = 0,
    Lulus = 1,
    LulusDenganRevisi = 2,
    TidakLulus = 3
}

/// <summary>Human-readable Indonesian labels for enum values shown in the UI.</summary>
public static class EnumLabels
{
    public static string Of(Enum value) => value switch
    {
        UserRole.Admin => "Admin",
        UserRole.Dosen => "Dosen",
        TahapTA.Pengajuan => "Pengajuan",
        TahapTA.Proposal => "Proposal",
        TahapTA.Penelitian => "Penelitian",
        TahapTA.SeminarHasil => "Seminar Hasil",
        TahapTA.Sidang => "Sidang",
        TahapTA.Lulus => "Lulus",
        StatusBimbingan.Menunggu => "Menunggu",
        StatusBimbingan.Disetujui => "Disetujui",
        StatusBimbingan.Revisi => "Revisi",
        JenisSidang.SeminarProposal => "Seminar Proposal",
        JenisSidang.SeminarHasil => "Seminar Hasil",
        JenisSidang.SidangAkhir => "Sidang Akhir",
        HasilSidang.BelumDilaksanakan => "Belum Dilaksanakan",
        HasilSidang.Lulus => "Lulus",
        HasilSidang.LulusDenganRevisi => "Lulus dengan Revisi",
        HasilSidang.TidakLulus => "Tidak Lulus",
        _ => value.ToString()
    };
}
