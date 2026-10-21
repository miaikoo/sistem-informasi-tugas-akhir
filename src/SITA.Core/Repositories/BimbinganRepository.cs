using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IBimbinganRepository
{
    /// <param name="dosenId">When set, only bimbingan of TA supervised by this lecturer.</param>
    IReadOnlyList<Bimbingan> Search(string? keyword = null, StatusBimbingan? status = null, int? dosenId = null);
    Bimbingan? GetById(int id);
    int Insert(Bimbingan bimbingan);
    void Update(Bimbingan bimbingan);
    void Delete(int id);
}

public class BimbinganRepository : IBimbinganRepository
{
    private readonly IDbConnectionFactory _factory;

    public BimbinganRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectSql = """
        SELECT b.*, m.Nim, m.Nama AS NamaMahasiswa, t.Judul AS JudulTA, d.Nama AS DosenNama
        FROM Bimbingan b
        JOIN TugasAkhir t ON t.Id = b.TugasAkhirId
        JOIN Mahasiswa m ON m.Id = t.MahasiswaId
        JOIN Dosen d ON d.Id = b.DosenId
        """;

    public IReadOnlyList<Bimbingan> Search(string? keyword = null, StatusBimbingan? status = null, int? dosenId = null)
    {
        using var conn = _factory.Open();
        return conn.Query<Bimbingan>(
            SelectSql + """

            WHERE (@Keyword IS NULL OR m.Nim LIKE @Keyword ESCAPE '\' OR m.Nama LIKE @Keyword ESCAPE '\' OR b.Topik LIKE @Keyword ESCAPE '\')
              AND (@Status IS NULL OR b.Status = @Status)
              AND (@DosenId IS NULL OR t.Pembimbing1Id = @DosenId OR t.Pembimbing2Id = @DosenId)
            ORDER BY b.Tanggal DESC, b.Id DESC
            """,
            new { Keyword = SqlHelpers.ContainsPattern(keyword), Status = (int?)status, DosenId = dosenId }).AsList();
    }

    public Bimbingan? GetById(int id)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<Bimbingan>(SelectSql + " WHERE b.Id = @Id", new { Id = id });
    }

    public int Insert(Bimbingan bimbingan)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Bimbingan (TugasAkhirId, DosenId, Tanggal, Topik, Catatan, Status)
            VALUES (@TugasAkhirId, @DosenId, @Tanggal, @Topik, @Catatan, @Status); SELECT last_insert_rowid();
            """, bimbingan);
    }

    public void Update(Bimbingan bimbingan)
    {
        using var conn = _factory.Open();
        conn.Execute(
            """
            UPDATE Bimbingan SET TugasAkhirId = @TugasAkhirId, DosenId = @DosenId, Tanggal = @Tanggal,
                   Topik = @Topik, Catatan = @Catatan, Status = @Status
            WHERE Id = @Id
            """, bimbingan);
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM Bimbingan WHERE Id = @Id", new { Id = id });
    }
}
