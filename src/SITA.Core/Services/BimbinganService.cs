using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class BimbinganService : ServiceBase
{
    private readonly IBimbinganRepository _repo;
    private readonly ITugasAkhirRepository _ta;

    public BimbinganService(IBimbinganRepository repo, ITugasAkhirRepository ta, UserSession session, ActivityLogger logger)
        : base(session, logger)
    {
        _repo = repo;
        _ta = ta;
    }

    public IReadOnlyList<Bimbingan> Search(string? keyword = null, StatusBimbingan? status = null)
    {
        RequireLogin();
        return _repo.Search(keyword, status, Session.DosenScope);
    }

    public OperationResult Save(Bimbingan input) => Execute(() =>
    {
        RequireLogin();
        var b = input.Clone();
        b.Topik = b.Topik.Trim();
        b.Catatan = b.Catatan.Trim();

        if (b.Id != 0)
        {
            var existing = _repo.GetById(b.Id);
            if (existing is null) return OperationResult.Fail("Data bimbingan tidak ditemukan. Mungkin sudah dihapus.");
            if (!Session.IsAdmin && existing.DosenId != Session.DosenId)
                throw new AccessDeniedException("Anda hanya dapat mengubah catatan bimbingan yang Anda buat.");
        }

        if (!Session.IsAdmin)
            b.DosenId = Session.DosenId ?? 0; // a lecturer always records sessions as themself

        var errors = Validators.ValidateBimbingan(b, DateTime.Today);
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (!_ta.IsPembimbing(b.TugasAkhirId, b.DosenId))
        {
            return Session.IsAdmin
                ? OperationResult.Fail("Dosen yang dipilih bukan pembimbing TA ini.")
                : throw new AccessDeniedException("Anda hanya dapat mencatat bimbingan untuk mahasiswa bimbingan Anda.");
        }

        var ta = _ta.GetById(b.TugasAkhirId);
        if (b.Id == 0)
        {
            int id = _repo.Insert(b);
            Logger.Log("Tambah", "Bimbingan", $"Mencatat bimbingan {ta?.NamaMahasiswa}: {b.Topik}");
            return OperationResult.Ok(id);
        }
        _repo.Update(b);
        Logger.Log("Ubah", "Bimbingan", $"Mengubah catatan bimbingan {ta?.NamaMahasiswa}: {b.Topik}");
        return OperationResult.Ok();
    });

    public OperationResult Delete(Bimbingan b) => Execute(() =>
    {
        RequireLogin();
        var existing = _repo.GetById(b.Id);
        if (existing is null) return OperationResult.Ok();
        if (!Session.IsAdmin && existing.DosenId != Session.DosenId)
            throw new AccessDeniedException("Anda hanya dapat menghapus catatan bimbingan yang Anda buat.");

        _repo.Delete(b.Id);
        Logger.Log("Hapus", "Bimbingan", $"Menghapus catatan bimbingan {existing.NamaMahasiswa}: {existing.Topik}");
        return OperationResult.Ok();
    });
}
