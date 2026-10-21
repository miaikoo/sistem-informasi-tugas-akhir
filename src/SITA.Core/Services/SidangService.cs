using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class SidangService : ServiceBase
{
    private readonly IJadwalSidangRepository _repo;
    private readonly ITugasAkhirRepository _ta;

    public SidangService(IJadwalSidangRepository repo, ITugasAkhirRepository ta, UserSession session, ActivityLogger logger)
        : base(session, logger)
    {
        _repo = repo;
        _ta = ta;
    }

    public IReadOnlyList<JadwalSidang> Search(string? keyword = null, bool upcomingOnly = false)
    {
        RequireLogin();
        return _repo.Search(keyword, upcomingOnly, Session.DosenScope);
    }

    public OperationResult Save(JadwalSidang input) => Execute(() =>
    {
        RequireAdmin();
        var s = input.Clone();
        s.Ruang = s.Ruang.Trim();

        var errors = Validators.ValidateSidang(s);
        if (errors.Count > 0) return OperationResult.Fail(errors);

        var ta = _ta.GetById(s.TugasAkhirId);
        if (ta is null) return OperationResult.Fail("Data tugas akhir tidak ditemukan.");

        int?[] pembimbing = [ta.Pembimbing1Id, ta.Pembimbing2Id];
        if (pembimbing.Contains(s.Penguji1Id) || pembimbing.Contains(s.Penguji2Id))
            errors.Add("Penguji tidak boleh merupakan pembimbing dari TA yang sama.");
        if (_repo.HasRoomConflict(s.Ruang, s.Waktu, s.Id))
            errors.Add($"{s.Ruang} sudah dipakai sidang lain pada {s.Waktu:dd/MM/yyyy HH:mm}.");
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (s.Id == 0)
        {
            int id = _repo.Insert(s);
            Logger.Log("Tambah", "Jadwal Sidang", $"Menjadwalkan {EnumLabels.Of(s.Jenis)} {ta.NamaMahasiswa} pada {s.Waktu:dd/MM/yyyy HH:mm}");
            return OperationResult.Ok(id);
        }
        _repo.Update(s);
        Logger.Log("Ubah", "Jadwal Sidang", $"Mengubah jadwal {EnumLabels.Of(s.Jenis)} {ta.NamaMahasiswa}");
        return OperationResult.Ok();
    });

    public OperationResult Delete(JadwalSidang s) => Execute(() =>
    {
        RequireAdmin();
        _repo.Delete(s.Id);
        Logger.Log("Hapus", "Jadwal Sidang", $"Menghapus jadwal {s.JenisLabel} {s.NamaMahasiswa}");
        return OperationResult.Ok();
    });
}
