using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IProgramStudiRepository
{
    IReadOnlyList<ProgramStudi> GetAll(string? keyword = null);
    bool ExistsKode(string kode, int excludeId = 0);
    int Insert(ProgramStudi prodi);
    void Update(ProgramStudi prodi);
    void Delete(int id);
}

public class ProgramStudiRepository : IProgramStudiRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProgramStudiRepository(IDbConnectionFactory factory) => _factory = factory;

    public IReadOnlyList<ProgramStudi> GetAll(string? keyword = null)
    {
        using var conn = _factory.Open();
        return conn.Query<ProgramStudi>(
            """
            SELECT p.*,
                   (SELECT COUNT(*) FROM Mahasiswa m WHERE m.ProdiId = p.Id) AS JumlahMahasiswa,
                   (SELECT COUNT(*) FROM Dosen d WHERE d.ProdiId = p.Id) AS JumlahDosen
            FROM ProgramStudi p
            WHERE @Keyword IS NULL OR p.Kode LIKE @Keyword ESCAPE '\' OR p.Nama LIKE @Keyword ESCAPE '\'
            ORDER BY p.Nama
            """,
            new { Keyword = SqlHelpers.ContainsPattern(keyword) }).AsList();
    }

    public bool ExistsKode(string kode, int excludeId = 0)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM ProgramStudi WHERE Kode = @Kode COLLATE NOCASE AND Id <> @ExcludeId",
            new { Kode = kode, ExcludeId = excludeId }) > 0;
    }

    public int Insert(ProgramStudi prodi)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            "INSERT INTO ProgramStudi (Kode, Nama, Jenjang) VALUES (@Kode, @Nama, @Jenjang); SELECT last_insert_rowid();",
            prodi);
    }

    public void Update(ProgramStudi prodi)
    {
        using var conn = _factory.Open();
        conn.Execute("UPDATE ProgramStudi SET Kode = @Kode, Nama = @Nama, Jenjang = @Jenjang WHERE Id = @Id", prodi);
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM ProgramStudi WHERE Id = @Id", new { Id = id });
    }
}
