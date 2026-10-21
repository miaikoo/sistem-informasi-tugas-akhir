using SITA.Core.Data;
using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.Core.Tests;

public class AuthTests
{
    [Fact]
    public void Login_WithSeededAdmin_Succeeds()
    {
        using var ctx = new TestContext(seed: true);
        var result = ctx.Auth.Login(DemoDataSeeder.AdminUsername, DemoDataSeeder.AdminPassword);

        Assert.True(result.Success, result.ErrorText);
        Assert.True(ctx.Session.IsAdmin);
    }

    [Theory]
    [InlineData("admin", "salah")]
    [InlineData("tidakada", "admin123")]
    public void Login_WithBadCredentials_ShowsGenericMessage(string username, string password)
    {
        using var ctx = new TestContext(seed: true);
        var result = ctx.Auth.Login(username, password);

        Assert.False(result.Success);
        Assert.Equal(AuthService.InvalidCredentialsMessage, result.Errors.Single());
        Assert.False(ctx.Session.IsAuthenticated);
    }

    [Fact]
    public void Login_InactiveAccount_IsRefused()
    {
        using var ctx = new TestContext(seed: true);
        var user = ctx.UserRepo.GetByUsername("budi")!;
        user.IsActive = false;
        ctx.UserRepo.Update(user);

        Assert.False(ctx.Auth.Login("budi", DemoDataSeeder.DosenPassword).Success);
    }

    [Fact]
    public void Passwords_AreStoredHashed()
    {
        using var ctx = new TestContext(seed: true);
        var user = ctx.UserRepo.GetByUsername("admin")!;

        Assert.NotEqual(DemoDataSeeder.AdminPassword, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash);
    }

    [Fact]
    public void Login_IsWrittenToActivityLog()
    {
        using var ctx = new TestContext(seed: true);
        ctx.Auth.Login("admin", DemoDataSeeder.AdminPassword);

        Assert.Contains(ctx.ActivityRepo.GetRecent(5), a => a.Aksi == "Login" && a.Username == "admin");
    }
}

public class MahasiswaServiceTests
{
    [Fact]
    public void DuplicateNim_IsRejected()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();

        var result = ctx.Mahasiswa.Save(new Mahasiswa { Nim = "5025211001", Nama = "Lain", ProdiId = data.ProdiId, Angkatan = 2022 });

        Assert.False(result.Success);
        Assert.Contains("NIM sudah terdaftar.", result.Errors);
    }

    [Fact]
    public void Search_ByPartialName_ReturnsMatches()
    {
        using var ctx = new TestContext(seed: true);
        ctx.SignInAdmin();

        var all = ctx.Mahasiswa.Search();
        var target = all[5];
        var found = ctx.Mahasiswa.Search(target.Nama[..4]);

        Assert.Contains(found, m => m.Id == target.Id);
        Assert.All(found, m => Assert.Contains(target.Nama[..4], m.Nama + m.Nim, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_TreatsWildcardsLiterally()
    {
        using var ctx = new TestContext(seed: true);
        ctx.SignInAdmin();

        Assert.Empty(ctx.Mahasiswa.Search("%"));
    }

    [Fact]
    public void Dosen_CannotManageMahasiswa()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInDosen(data.DosenIds[0]);

        var result = ctx.Mahasiswa.Save(new Mahasiswa { Nim = "5025211999", Nama = "Baru", ProdiId = data.ProdiId, Angkatan = 2022 });

        Assert.False(result.Success);
        Assert.Contains("hak akses", result.ErrorText);
    }

    [Fact]
    public void Delete_MahasiswaWithTa_IsRefused()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();

        var mhs = ctx.MahasiswaRepo.GetById(data.MahasiswaId)!;
        Assert.False(ctx.Mahasiswa.Delete(mhs).Success);
        Assert.NotNull(ctx.MahasiswaRepo.GetById(data.MahasiswaId));
    }

    [Fact]
    public void Delete_DosenStillReferenced_IsTranslatedToFriendlyMessage()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();

        // Bypass the in-memory count check to exercise the database foreign key path.
        var dosen = ctx.DosenRepo.GetById(data.DosenIds[0])!;
        dosen.JumlahBimbingan = 0;
        var result = ctx.Dosen.Delete(dosen);

        Assert.False(result.Success);
        Assert.Contains("masih terkait", result.ErrorText);
    }
}

public class TugasAkhirServiceTests
{
    [Fact]
    public void Dosen_CanChangeStageOfOwnStudent_ButNotSupervisors()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInDosen(data.DosenIds[0]);

        var ta = ctx.TaRepo.GetById(data.TaId)!;
        ta.Tahap = TahapTA.Penelitian;
        ta.Pembimbing1Id = data.DosenIds[2]; // attempt to reassign — must be ignored
        var result = ctx.TugasAkhir.Save(ta);

        Assert.True(result.Success, result.ErrorText);
        var saved = ctx.TaRepo.GetById(data.TaId)!;
        Assert.Equal(TahapTA.Penelitian, saved.Tahap);
        Assert.Equal(data.DosenIds[0], saved.Pembimbing1Id);
    }

    [Fact]
    public void Dosen_CannotEditOtherLecturersStudent()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInDosen(data.DosenIds[1]);

        var ta = ctx.TaRepo.GetById(data.TaId)!;
        ta.Tahap = TahapTA.Lulus;

        Assert.False(ctx.TugasAkhir.Save(ta).Success);
        Assert.Equal(TahapTA.Proposal, ctx.TaRepo.GetById(data.TaId)!.Tahap);
    }

    [Fact]
    public void Dosen_OnlySeesOwnStudents()
    {
        using var ctx = new TestContext(seed: true);
        var budi = ctx.UserRepo.GetByUsername("budi")!;
        ctx.Session.SignIn(budi);

        var list = ctx.TugasAkhir.Search();

        Assert.NotEmpty(list);
        Assert.All(list, t => Assert.True(t.Pembimbing1Id == budi.DosenId || t.Pembimbing2Id == budi.DosenId));
    }

    [Fact]
    public void SecondTaForSameStudent_IsRejected()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();

        var result = ctx.TugasAkhir.Save(new TugasAkhir
        {
            MahasiswaId = data.MahasiswaId, Judul = "Judul kedua untuk mahasiswa yang sama", Pembimbing1Id = data.DosenIds[1],
            TanggalMulai = DateTime.Today
        });

        Assert.False(result.Success);
    }

    [Fact]
    public void DeletingTa_CascadesToBimbingan()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();
        ctx.BimbinganRepo.Insert(new Bimbingan { TugasAkhirId = data.TaId, DosenId = data.DosenIds[0], Topik = "Bab 1", Tanggal = DateTime.Today });

        Assert.True(ctx.TugasAkhir.Delete(ctx.TaRepo.GetById(data.TaId)!).Success);
        Assert.Empty(ctx.BimbinganRepo.Search());
    }
}

public class BimbinganServiceTests
{
    [Fact]
    public void Dosen_CannotLogBimbinganForOtherLecturersStudent()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInDosen(data.DosenIds[1]);

        var result = ctx.Bimbingan.Save(new Bimbingan { TugasAkhirId = data.TaId, Topik = "Bab 2", Tanggal = DateTime.Today });

        Assert.False(result.Success);
        Assert.Empty(ctx.BimbinganRepo.Search());
    }

    [Fact]
    public void Dosen_LogsBimbinganAsThemself()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInDosen(data.DosenIds[0]);

        // DosenId supplied by the caller is overridden by the session
        var result = ctx.Bimbingan.Save(new Bimbingan { TugasAkhirId = data.TaId, DosenId = data.DosenIds[2], Topik = "Bab 2", Tanggal = DateTime.Today });

        Assert.True(result.Success, result.ErrorText);
        Assert.Equal(data.DosenIds[0], ctx.BimbinganRepo.Search().Single().DosenId);
    }

    [Fact]
    public void Admin_MustPickAPembimbingOfTheTa()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();

        var result = ctx.Bimbingan.Save(new Bimbingan { TugasAkhirId = data.TaId, DosenId = data.DosenIds[2], Topik = "Bab 2", Tanggal = DateTime.Today });

        Assert.False(result.Success);
    }

    [Fact]
    public void Dates_RoundTripThroughDatabase()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();
        var tanggal = new DateTime(2026, 3, 14);
        ctx.Bimbingan.Save(new Bimbingan { TugasAkhirId = data.TaId, DosenId = data.DosenIds[0], Topik = "Bab 3", Tanggal = tanggal, Status = StatusBimbingan.Revisi });

        var saved = ctx.BimbinganRepo.Search().Single();
        Assert.Equal(tanggal, saved.Tanggal);
        Assert.Equal(StatusBimbingan.Revisi, saved.Status);
        Assert.Equal(tanggal, ctx.TaRepo.GetById(data.TaId)!.BimbinganTerakhir);
    }
}

public class SidangServiceTests
{
    [Fact]
    public void PengujiCannotBePembimbing()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();

        var result = ctx.Sidang.Save(new JadwalSidang
        {
            TugasAkhirId = data.TaId, Ruang = "Ruang 1", Waktu = DateTime.Today.AddDays(3).AddHours(9),
            Penguji1Id = data.DosenIds[0], Penguji2Id = data.DosenIds[1]
        });

        Assert.False(result.Success);
        Assert.Contains("pembimbing", result.ErrorText);
    }

    [Fact]
    public void RoomDoubleBooking_IsRejected()
    {
        using var ctx = new TestContext();
        var data = ctx.CreateBasicData();
        ctx.SignInAdmin();
        var sidang = new JadwalSidang
        {
            TugasAkhirId = data.TaId, Ruang = "Ruang 1", Waktu = DateTime.Today.AddDays(3).AddHours(9),
            Penguji1Id = data.DosenIds[1], Penguji2Id = data.DosenIds[2]
        };

        Assert.True(ctx.Sidang.Save(sidang).Success);
        var second = ctx.Sidang.Save(sidang);

        Assert.False(second.Success);
        Assert.Contains("sudah dipakai", second.ErrorText);
    }
}

public class UserServiceTests
{
    [Fact]
    public void Admin_CannotDeactivateSelf()
    {
        using var ctx = new TestContext(seed: true);
        ctx.Auth.Login("admin", DemoDataSeeder.AdminPassword);

        var me = ctx.UserRepo.GetByUsername("admin")!;
        me.IsActive = false;

        Assert.False(ctx.Users.Save(me, null).Success);
    }

    [Fact]
    public void ResetPassword_AllowsLoginWithNewPassword()
    {
        using var ctx = new TestContext(seed: true);
        ctx.Auth.Login("admin", DemoDataSeeder.AdminPassword);

        var budi = ctx.UserRepo.GetByUsername("budi")!;
        Assert.True(ctx.Users.Save(budi, "rahasia99").Success);

        ctx.Auth.Logout();
        Assert.False(ctx.Auth.Login("budi", DemoDataSeeder.DosenPassword).Success);
        Assert.True(ctx.Auth.Login("budi", "rahasia99").Success);
    }
}

public class DashboardTests
{
    [Fact]
    public void Summary_IsComputedFromDatabase()
    {
        using var ctx = new TestContext(seed: true);
        ctx.SignInAdmin();

        var before = ctx.Dashboard.Load();
        var ta = ctx.TugasAkhir.Search().First(t => t.Tahap != TahapTA.Lulus);
        ctx.Bimbingan.Save(new Bimbingan { TugasAkhirId = ta.Id, DosenId = ta.Pembimbing1Id, Topik = "Tambahan", Tanggal = DateTime.Today });
        var after = ctx.Dashboard.Load();

        Assert.Equal(before.Summary.BimbinganBulanIni + 1, after.Summary.BimbinganBulanIni);
        Assert.Equal(60, after.Summary.TotalMahasiswa);
        Assert.Equal(ctx.TugasAkhir.Search().Count(t => t.Tahap != TahapTA.Lulus), after.Summary.MahasiswaTaAktif);
        Assert.Equal(ctx.TugasAkhir.Search().Count, after.TahapDistribution.Sum(p => p.Value));
        Assert.Equal(6, after.BimbinganPerBulan.Count);
        Assert.True(after.Summary.SidangMendatang > 0);
    }

    [Fact]
    public void DosenDashboard_IsScopedToOwnStudents()
    {
        using var ctx = new TestContext(seed: true);
        var budi = ctx.UserRepo.GetByUsername("budi")!;
        ctx.Session.SignIn(budi);

        var data = ctx.Dashboard.Load();

        Assert.Equal(ctx.TugasAkhir.Search().Count, data.TahapDistribution.Sum(p => p.Value));
        Assert.Empty(data.BebanDosen);
    }
}
