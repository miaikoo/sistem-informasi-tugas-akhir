using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class ProdiService : ServiceBase
{
    private readonly IProgramStudiRepository _repo;

    public ProdiService(IProgramStudiRepository repo, UserSession session, ActivityLogger logger) : base(session, logger)
        => _repo = repo;

    public IReadOnlyList<ProgramStudi> Search(string? keyword = null)
    {
        RequireLogin();
        return _repo.GetAll(keyword);
    }

    public OperationResult Save(ProgramStudi input) => Execute(() =>
    {
        RequireAdmin();
        var p = input.Clone();
        p.Kode = p.Kode.Trim().ToUpperInvariant();
        p.Nama = p.Nama.Trim();

        var errors = Validators.ValidateProdi(p);
        if (errors.Count == 0 && _repo.ExistsKode(p.Kode, p.Id)) errors.Add("Kode prodi sudah digunakan.");
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (p.Id == 0)
        {
            int id = _repo.Insert(p);
            Logger.Log("Tambah", "Program Studi", $"Menambah prodi {p.Kode} – {p.Nama}");
            return OperationResult.Ok(id);
        }
        _repo.Update(p);
        Logger.Log("Ubah", "Program Studi", $"Mengubah prodi {p.Kode} – {p.Nama}");
        return OperationResult.Ok();
    });

    public OperationResult Delete(ProgramStudi p) => Execute(() =>
    {
        RequireAdmin();
        if (p.JumlahMahasiswa > 0 || p.JumlahDosen > 0)
            return OperationResult.Fail($"Prodi {p.Nama} masih memiliki {p.JumlahMahasiswa} mahasiswa dan {p.JumlahDosen} dosen, sehingga tidak dapat dihapus.");
        _repo.Delete(p.Id);
        Logger.Log("Hapus", "Program Studi", $"Menghapus prodi {p.Kode} – {p.Nama}");
        return OperationResult.Ok();
    });
}

public class DosenService : ServiceBase
{
    private readonly IDosenRepository _repo;

    public DosenService(IDosenRepository repo, UserSession session, ActivityLogger logger) : base(session, logger)
        => _repo = repo;

    public IReadOnlyList<Dosen> Search(string? keyword = null, int? prodiId = null)
    {
        RequireLogin();
        return _repo.Search(keyword, prodiId);
    }

    public OperationResult Save(Dosen input) => Execute(() =>
    {
        RequireAdmin();
        var d = input.Clone();
        d.Nip = d.Nip.Trim();
        d.Nama = d.Nama.Trim();
        d.Email = d.Email.Trim();
        d.BidangKeahlian = d.BidangKeahlian.Trim();

        var errors = Validators.ValidateDosen(d);
        if (errors.Count == 0 && _repo.ExistsNip(d.Nip, d.Id)) errors.Add("NIP sudah terdaftar.");
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (d.Id == 0)
        {
            int id = _repo.Insert(d);
            Logger.Log("Tambah", "Dosen", $"Menambah dosen {d.Nama}");
            return OperationResult.Ok(id);
        }
        _repo.Update(d);
        Logger.Log("Ubah", "Dosen", $"Mengubah data dosen {d.Nama}");
        return OperationResult.Ok();
    });

    public OperationResult Delete(Dosen d) => Execute(() =>
    {
        RequireAdmin();
        if (d.JumlahBimbingan > 0)
            return OperationResult.Fail($"{d.Nama} masih membimbing {d.JumlahBimbingan} mahasiswa aktif, sehingga tidak dapat dihapus.");
        _repo.Delete(d.Id);
        Logger.Log("Hapus", "Dosen", $"Menghapus dosen {d.Nama}");
        return OperationResult.Ok();
    });
}

public class MahasiswaService : ServiceBase
{
    private readonly IMahasiswaRepository _repo;

    public MahasiswaService(IMahasiswaRepository repo, UserSession session, ActivityLogger logger) : base(session, logger)
        => _repo = repo;

    public IReadOnlyList<Mahasiswa> Search(string? keyword = null, int? prodiId = null, int? angkatan = null)
    {
        RequireAdmin();
        return _repo.Search(keyword, prodiId, angkatan);
    }

    public IReadOnlyList<Mahasiswa> GetWithoutTugasAkhir()
    {
        RequireAdmin();
        return _repo.GetWithoutTugasAkhir();
    }

    public IReadOnlyList<int> GetAngkatanList()
    {
        RequireLogin();
        return _repo.GetAngkatanList();
    }

    public OperationResult Save(Mahasiswa input) => Execute(() =>
    {
        RequireAdmin();
        var m = input.Clone();
        m.Nim = m.Nim.Trim();
        m.Nama = m.Nama.Trim();
        m.Email = m.Email.Trim();

        var errors = Validators.ValidateMahasiswa(m, DateTime.Today.Year);
        if (errors.Count == 0 && _repo.ExistsNim(m.Nim, m.Id)) errors.Add("NIM sudah terdaftar.");
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (m.Id == 0)
        {
            int id = _repo.Insert(m);
            Logger.Log("Tambah", "Mahasiswa", $"Menambah mahasiswa {m.Nim} – {m.Nama}");
            return OperationResult.Ok(id);
        }
        _repo.Update(m);
        Logger.Log("Ubah", "Mahasiswa", $"Mengubah data mahasiswa {m.Nim} – {m.Nama}");
        return OperationResult.Ok();
    });

    public OperationResult Delete(Mahasiswa m) => Execute(() =>
    {
        RequireAdmin();
        if (m.TahapTA is not null)
            return OperationResult.Fail($"{m.Nama} sudah memiliki data tugas akhir. Hapus data TA terlebih dahulu.");
        _repo.Delete(m.Id);
        Logger.Log("Hapus", "Mahasiswa", $"Menghapus mahasiswa {m.Nim} – {m.Nama}");
        return OperationResult.Ok();
    });
}
