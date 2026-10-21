using System.Text.RegularExpressions;
using SITA.Core.Models;

namespace SITA.Core.Services;

/// <summary>
/// Field-level validation rules. These only check the shape of the data; rules that need the
/// database (uniqueness, ownership) live in the services.
/// </summary>
public static partial class Validators
{
    public const int MinPasswordLength = 6;

    [GeneratedRegex(@"^\d{10,14}$")]
    private static partial Regex NimRegex();

    [GeneratedRegex(@"^\d{18}$")]
    private static partial Regex NipRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]{2,10}$")]
    private static partial Regex KodeRegex();

    [GeneratedRegex(@"^[A-Za-z0-9._]{3,30}$")]
    private static partial Regex UsernameRegex();

    private static bool Blank(string? s) => string.IsNullOrWhiteSpace(s);

    private static void CheckEmail(string? email, List<string> errors, bool required)
    {
        if (Blank(email))
        {
            if (required) errors.Add("Email wajib diisi.");
        }
        else if (!EmailRegex().IsMatch(email!.Trim()))
        {
            errors.Add("Format email tidak valid.");
        }
    }

    public static List<string> ValidateProdi(ProgramStudi p)
    {
        var errors = new List<string>();
        if (Blank(p.Kode)) errors.Add("Kode prodi wajib diisi.");
        else if (!KodeRegex().IsMatch(p.Kode.Trim())) errors.Add("Kode prodi 2–10 karakter huruf/angka.");
        if (Blank(p.Nama)) errors.Add("Nama prodi wajib diisi.");
        if (p.Jenjang is not ("D3" or "D4" or "S1" or "S2" or "S3")) errors.Add("Jenjang harus D3, D4, S1, S2, atau S3.");
        return errors;
    }

    public static List<string> ValidateDosen(Dosen d)
    {
        var errors = new List<string>();
        if (Blank(d.Nip)) errors.Add("NIP wajib diisi.");
        else if (!NipRegex().IsMatch(d.Nip.Trim())) errors.Add("NIP harus terdiri dari 18 digit angka.");
        if (Blank(d.Nama)) errors.Add("Nama dosen wajib diisi.");
        else if (d.Nama.Trim().Length > 100) errors.Add("Nama dosen maksimal 100 karakter.");
        CheckEmail(d.Email, errors, required: true);
        if (d.ProdiId <= 0) errors.Add("Program studi wajib dipilih.");
        return errors;
    }

    public static List<string> ValidateMahasiswa(Mahasiswa m, int currentYear)
    {
        var errors = new List<string>();
        if (Blank(m.Nim)) errors.Add("NIM wajib diisi.");
        else if (!NimRegex().IsMatch(m.Nim.Trim())) errors.Add("NIM harus terdiri dari 10–14 digit angka.");
        if (Blank(m.Nama)) errors.Add("Nama mahasiswa wajib diisi.");
        else if (m.Nama.Trim().Length > 100) errors.Add("Nama mahasiswa maksimal 100 karakter.");
        CheckEmail(m.Email, errors, required: false);
        if (m.ProdiId <= 0) errors.Add("Program studi wajib dipilih.");
        if (m.Angkatan < 2000 || m.Angkatan > currentYear) errors.Add($"Angkatan harus antara 2000 dan {currentYear}.");
        return errors;
    }

    public static List<string> ValidateTugasAkhir(TugasAkhir t, DateTime today)
    {
        var errors = new List<string>();
        if (t.MahasiswaId <= 0) errors.Add("Mahasiswa wajib dipilih.");
        if (Blank(t.Judul)) errors.Add("Judul TA wajib diisi.");
        else if (t.Judul.Trim().Length < 10) errors.Add("Judul TA minimal 10 karakter.");
        else if (t.Judul.Trim().Length > 250) errors.Add("Judul TA maksimal 250 karakter.");
        if (t.Pembimbing1Id <= 0) errors.Add("Pembimbing 1 wajib dipilih.");
        if (t.Pembimbing2Id is { } p2 && p2 == t.Pembimbing1Id) errors.Add("Pembimbing 1 dan Pembimbing 2 tidak boleh sama.");
        if (!Enum.IsDefined(t.Tahap)) errors.Add("Tahap TA tidak valid.");
        if (t.TanggalMulai.Date > today.Date) errors.Add("Tanggal mulai tidak boleh di masa depan.");
        return errors;
    }

    public static List<string> ValidateBimbingan(Bimbingan b, DateTime today)
    {
        var errors = new List<string>();
        if (b.TugasAkhirId <= 0) errors.Add("Tugas akhir wajib dipilih.");
        if (b.DosenId <= 0) errors.Add("Dosen pembimbing wajib dipilih.");
        if (b.Tanggal.Date > today.Date) errors.Add("Tanggal bimbingan tidak boleh di masa depan.");
        if (Blank(b.Topik)) errors.Add("Topik bimbingan wajib diisi.");
        else if (b.Topik.Trim().Length > 150) errors.Add("Topik maksimal 150 karakter.");
        if (!Enum.IsDefined(b.Status)) errors.Add("Status bimbingan tidak valid.");
        return errors;
    }

    public static List<string> ValidateSidang(JadwalSidang s)
    {
        var errors = new List<string>();
        if (s.TugasAkhirId <= 0) errors.Add("Tugas akhir wajib dipilih.");
        if (Blank(s.Ruang)) errors.Add("Ruang wajib diisi.");
        if (s.Penguji1Id <= 0 || s.Penguji2Id <= 0) errors.Add("Penguji 1 dan Penguji 2 wajib dipilih.");
        else if (s.Penguji1Id == s.Penguji2Id) errors.Add("Penguji 1 dan Penguji 2 tidak boleh sama.");
        if (s.Waktu.Hour < 7 || s.Waktu.Hour > 17) errors.Add("Jam sidang harus antara 07:00 dan 17:00.");
        if (!Enum.IsDefined(s.Jenis)) errors.Add("Jenis sidang tidak valid.");
        if (!Enum.IsDefined(s.Hasil)) errors.Add("Hasil sidang tidak valid.");
        return errors;
    }

    public static List<string> ValidateUser(User u, string? password, bool isNew)
    {
        var errors = new List<string>();
        if (Blank(u.Username)) errors.Add("Username wajib diisi.");
        else if (!UsernameRegex().IsMatch(u.Username.Trim())) errors.Add("Username 3–30 karakter (huruf, angka, titik, garis bawah).");
        if (Blank(u.NamaLengkap)) errors.Add("Nama lengkap wajib diisi.");
        if (!Enum.IsDefined(u.Role)) errors.Add("Peran tidak valid.");
        if (u.Role == UserRole.Dosen && u.DosenId is null or <= 0) errors.Add("Akun dosen harus ditautkan ke data dosen.");
        if (isNew || !Blank(password))
        {
            var pwErrors = ValidatePassword(password);
            errors.AddRange(pwErrors);
        }
        return errors;
    }

    public static List<string> ValidatePassword(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password)) errors.Add("Password wajib diisi.");
        else if (password.Length < MinPasswordLength) errors.Add($"Password minimal {MinPasswordLength} karakter.");
        return errors;
    }
}
