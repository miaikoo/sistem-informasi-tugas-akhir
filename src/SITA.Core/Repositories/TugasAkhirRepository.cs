using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface ITugasAkhirRepository
{
    /// <param name="dosenId">When set, only TA supervised by this lecturer (as pembimbing 1 or 2).</param>
    IReadOnlyList<TugasAkhir> Search(string? keyword = null, TahapTA? tahap = null, int? dosenId = null);
    TugasAkhir? GetById(int id);
    bool IsPembimbing(int tugasAkhirId, int dosenId);
    int Insert(TugasAkhir ta);
    void Update(TugasAkhir ta);
    void Delete(int id);
}

public class TugasAkhirRepository : ITugasAkhirRepository
{
    private readonly IDbConnectionFactory _factory;

    public TugasAkhirRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectSql = """
        SELECT t.*, m.Nim, m.Nama AS NamaMahasiswa,
               d1.Nama AS Pembimbing1Nama, d2.Nama AS Pembimbing2Nama,
               (SELECT COUNT(*) FROM Bimbingan b WHERE b.TugasAkhirId = t.Id) AS JumlahBimbingan,
               (SELECT MAX(b.Tanggal) FROM Bimbingan b WHERE b.TugasAkhirId = t.Id) AS BimbinganTerakhir
        FROM TugasAkhir t
        JOIN Mahasiswa m ON m.Id = t.MahasiswaId
        JOIN Dosen d1 ON d1.Id = t.Pembimbing1Id
        LEFT JOIN Dosen d2 ON d2.Id = t.Pembimbing2Id
        """;

    public IReadOnlyList<TugasAkhir> Search(string? keyword = null, TahapTA? tahap = null, int? dosenId = null)
    {
        using var conn = _factory.Open();
        return conn.Query<TugasAkhir>(
            SelectSql + """

            WHERE (@Keyword IS NULL OR m.Nim LIKE @Keyword ESCAPE '\' OR m.Nama LIKE @Keyword ESCAPE '\' OR t.Judul LIKE @Keyword ESCAPE '\')
              AND (@Tahap IS NULL OR t.Tahap = @Tahap)
              AND (@DosenId IS NULL OR t.Pembimbing1Id = @DosenId OR t.Pembimbing2Id = @DosenId)
            ORDER BY t.Tahap DESC, m.Nim
            """,
            new { Keyword = SqlHelpers.ContainsPattern(keyword), Tahap = (int?)tahap, DosenId = dosenId }).AsList();
    }

    public TugasAkhir? GetById(int id)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<TugasAkhir>(SelectSql + " WHERE t.Id = @Id", new { Id = id });
    }

    public bool IsPembimbing(int tugasAkhirId, int dosenId)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM TugasAkhir WHERE Id = @Id AND (Pembimbing1Id = @DosenId OR Pembimbing2Id = @DosenId)",
            new { Id = tugasAkhirId, DosenId = dosenId }) > 0;
    }

    public int Insert(TugasAkhir ta)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO TugasAkhir (MahasiswaId, Judul, BidangMinat, Pembimbing1Id, Pembimbing2Id, Tahap, TanggalMulai, UpdatedAt)
            VALUES (@MahasiswaId, @Judul, @BidangMinat, @Pembimbing1Id, @Pembimbing2Id, @Tahap, @TanggalMulai, @UpdatedAt);
            SELECT last_insert_rowid();
            """, ta);
    }

    public void Update(TugasAkhir ta)
    {
        using var conn = _factory.Open();
        conn.Execute(
            """
            UPDATE TugasAkhir
            SET MahasiswaId = @MahasiswaId, Judul = @Judul, BidangMinat = @BidangMinat,
                Pembimbing1Id = @Pembimbing1Id, Pembimbing2Id = @Pembimbing2Id, Tahap = @Tahap,
                TanggalMulai = @TanggalMulai, UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """, ta);
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM TugasAkhir WHERE Id = @Id", new { Id = id });
    }
}
