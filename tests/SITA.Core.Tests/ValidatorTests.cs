using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.Core.Tests;

public class ValidatorTests
{
    private static Mahasiswa ValidMahasiswa() => new()
    {
        Nim = "5025211001", Nama = "Andi", Email = "andi@kampus.ac.id", ProdiId = 1, Angkatan = 2022
    };

    [Fact]
    public void ValidMahasiswa_HasNoErrors() =>
        Assert.Empty(Validators.ValidateMahasiswa(ValidMahasiswa(), 2026));

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("50252110ab")]
    [InlineData("123456789012345")]
    public void InvalidNim_IsRejected(string nim)
    {
        var m = ValidMahasiswa();
        m.Nim = nim;
        Assert.Contains(Validators.ValidateMahasiswa(m, 2026), e => e.Contains("NIM"));
    }

    [Fact]
    public void FutureAngkatan_IsRejected()
    {
        var m = ValidMahasiswa();
        m.Angkatan = 2030;
        Assert.Contains(Validators.ValidateMahasiswa(m, 2026), e => e.Contains("Angkatan"));
    }

    [Fact]
    public void InvalidEmail_IsRejected()
    {
        var m = ValidMahasiswa();
        m.Email = "bukan-email";
        Assert.Contains(Validators.ValidateMahasiswa(m, 2026), e => e.Contains("email"));
    }

    [Fact]
    public void Nip_MustBe18Digits()
    {
        var d = new Dosen { Nip = "123", Nama = "X", Email = "x@y.id", ProdiId = 1 };
        Assert.Contains(Validators.ValidateDosen(d), e => e.Contains("NIP"));
    }

    [Fact]
    public void SamePembimbing_IsRejected()
    {
        var ta = new TugasAkhir
        {
            MahasiswaId = 1, Judul = "Judul tugas akhir yang cukup panjang", Pembimbing1Id = 3, Pembimbing2Id = 3,
            TanggalMulai = DateTime.Today
        };
        Assert.Contains(Validators.ValidateTugasAkhir(ta, DateTime.Today), e => e.Contains("tidak boleh sama"));
    }

    [Fact]
    public void FutureBimbingan_IsRejected()
    {
        var b = new Bimbingan { TugasAkhirId = 1, DosenId = 1, Topik = "Bab 1", Tanggal = DateTime.Today.AddDays(1) };
        Assert.Contains(Validators.ValidateBimbingan(b, DateTime.Today), e => e.Contains("masa depan"));
    }

    [Fact]
    public void ShortPassword_IsRejected() =>
        Assert.NotEmpty(Validators.ValidatePassword("123"));

    [Fact]
    public void ExistingUser_WithBlankPassword_KeepsPassword()
    {
        var u = new User { Id = 5, Username = "budi", NamaLengkap = "Budi", Role = UserRole.Dosen, DosenId = 1 };
        Assert.Empty(Validators.ValidateUser(u, "", isNew: false));
        Assert.NotEmpty(Validators.ValidateUser(u, "", isNew: true));
    }
}
