using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class TugasAkhirService : ServiceBase
{
    private readonly ITugasAkhirRepository _repo;
    private readonly IMahasiswaRepository _mahasiswa;

    public TugasAkhirService(ITugasAkhirRepository repo, IMahasiswaRepository mahasiswa, UserSession session, ActivityLogger logger)
        : base(session, logger)
    {
        _repo = repo;
        _mahasiswa = mahasiswa;
    }

    /// <summary>Admins see every TA; lecturers only the ones they supervise.</summary>
    public IReadOnlyList<TugasAkhir> Search(string? keyword = null, TahapTA? tahap = null, int? pembimbingId = null)
    {
        RequireLogin();
        int? scope = Session.DosenScope ?? pembimbingId;
        return _repo.Search(keyword, tahap, scope);
    }

    public OperationResult Save(TugasAkhir input) => Execute(() =>
    {
        RequireLogin();
        var ta = input.Clone();
        ta.Judul = ta.Judul.Trim();
        ta.BidangMinat = ta.BidangMinat.Trim();
        ta.UpdatedAt = DateTime.Now;

        TugasAkhir? existing = null;
        if (ta.Id == 0)
        {
            RequireAdmin();
        }
        else
        {
            existing = _repo.GetById(ta.Id);
            if (existing is null) return OperationResult.Fail("Data tugas akhir tidak ditemukan. Mungkin sudah dihapus.");

            if (!Session.IsAdmin)
            {
                if (Session.DosenId is not { } dosenId || !_repo.IsPembimbing(ta.Id, dosenId))
                    throw new AccessDeniedException("Anda hanya dapat mengubah TA mahasiswa bimbingan Anda.");

                // Lecturers may only change the title, topic area and stage.
                ta.MahasiswaId = existing.MahasiswaId;
                ta.Pembimbing1Id = existing.Pembimbing1Id;
                ta.Pembimbing2Id = existing.Pembimbing2Id;
                ta.TanggalMulai = existing.TanggalMulai;
            }
        }

        var errors = Validators.ValidateTugasAkhir(ta, DateTime.Today);
        if (errors.Count == 0 && ta.Id == 0 && _mahasiswa.GetById(ta.MahasiswaId)?.TahapTA is not null)
            errors.Add("Mahasiswa ini sudah memiliki tugas akhir.");
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (existing is null)
        {
            int id = _repo.Insert(ta);
            var mhs = _mahasiswa.GetById(ta.MahasiswaId);
            Logger.Log("Tambah", "Tugas Akhir", $"Mendaftarkan TA {mhs?.Nama}: \"{ta.Judul}\"");
            return OperationResult.Ok(id);
        }

        _repo.Update(ta);
        string detail = existing.Tahap != ta.Tahap
            ? $"Tahap TA {existing.NamaMahasiswa} berubah: {EnumLabels.Of(existing.Tahap)} → {EnumLabels.Of(ta.Tahap)}"
            : $"Mengubah data TA {existing.NamaMahasiswa}";
        Logger.Log("Ubah", "Tugas Akhir", detail);
        return OperationResult.Ok();
    });

    public OperationResult Delete(TugasAkhir ta) => Execute(() =>
    {
        RequireAdmin();
        _repo.Delete(ta.Id);
        Logger.Log("Hapus", "Tugas Akhir", $"Menghapus TA {ta.NamaMahasiswa} beserta log bimbingan dan jadwal sidangnya");
        return OperationResult.Ok();
    });
}
