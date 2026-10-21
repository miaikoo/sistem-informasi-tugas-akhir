using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IMahasiswaRepository
{
    IReadOnlyList<Mahasiswa> Search(string? keyword = null, int? prodiId = null, int? angkatan = null);
    Mahasiswa? GetById(int id);
    IReadOnlyList<Mahasiswa> GetWithoutTugasAkhir();
    IReadOnlyList<int> GetAngkatanList();
    bool ExistsNim(string nim, int excludeId = 0);
    int Insert(Mahasiswa mahasiswa);
    void Update(Mahasiswa mahasiswa);
    void Delete(int id);
}

public class MahasiswaRepository : IMahasiswaRepository
{
    private readonly IDbConnectionFactory _factory;

    public MahasiswaRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectSql = """
        SELECT m.*, p.Nama AS ProdiNama, t.Tahap AS TahapTA
        FROM Mahasiswa m
        JOIN ProgramStudi p ON p.Id = m.ProdiId
        LEFT JOIN TugasAkhir t ON t.MahasiswaId = m.Id
        """;

    public IReadOnlyList<Mahasiswa> Search(string? keyword = null, int? prodiId = null, int? angkatan = null)
    {
        using var conn = _factory.Open();
        return conn.Query<Mahasiswa>(
            SelectSql + """

            WHERE (@Keyword IS NULL OR m.Nim LIKE @Keyword ESCAPE '\' OR m.Nama LIKE @Keyword ESCAPE '\')
              AND (@ProdiId IS NULL OR m.ProdiId = @ProdiId)
              AND (@Angkatan IS NULL OR m.Angkatan = @Angkatan)
            ORDER BY m.Nim
            """,
            new { Keyword = SqlHelpers.ContainsPattern(keyword), ProdiId = prodiId, Angkatan = angkatan }).AsList();
    }

    public Mahasiswa? GetById(int id)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<Mahasiswa>(SelectSql + " WHERE m.Id = @Id", new { Id = id });
    }

    public IReadOnlyList<Mahasiswa> GetWithoutTugasAkhir()
    {
        using var conn = _factory.Open();
        return conn.Query<Mahasiswa>(SelectSql + " WHERE t.Id IS NULL ORDER BY m.Nim").AsList();
    }

    public IReadOnlyList<int> GetAngkatanList()
    {
        using var conn = _factory.Open();
        return conn.Query<int>("SELECT DISTINCT Angkatan FROM Mahasiswa ORDER BY Angkatan DESC").AsList();
    }

    public bool ExistsNim(string nim, int excludeId = 0)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Mahasiswa WHERE Nim = @Nim AND Id <> @ExcludeId",
            new { Nim = nim, ExcludeId = excludeId }) > 0;
    }

    public int Insert(Mahasiswa mahasiswa)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Mahasiswa (Nim, Nama, Email, ProdiId, Angkatan)
            VALUES (@Nim, @Nama, @Email, @ProdiId, @Angkatan); SELECT last_insert_rowid();
            """, mahasiswa);
    }

    public void Update(Mahasiswa mahasiswa)
    {
        using var conn = _factory.Open();
        conn.Execute(
            "UPDATE Mahasiswa SET Nim = @Nim, Nama = @Nama, Email = @Email, ProdiId = @ProdiId, Angkatan = @Angkatan WHERE Id = @Id",
            mahasiswa);
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM Mahasiswa WHERE Id = @Id", new { Id = id });
    }
}
