using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IJadwalSidangRepository
{
    /// <param name="dosenId">When set, only sessions where this lecturer is a supervisor or examiner.</param>
    IReadOnlyList<JadwalSidang> Search(string? keyword = null, bool upcomingOnly = false, int? dosenId = null);
    JadwalSidang? GetById(int id);
    bool HasRoomConflict(string ruang, DateTime waktu, int excludeId = 0);
    int Insert(JadwalSidang sidang);
    void Update(JadwalSidang sidang);
    void Delete(int id);
}

public class JadwalSidangRepository : IJadwalSidangRepository
{
    private readonly IDbConnectionFactory _factory;

    public JadwalSidangRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectSql = """
        SELECT s.*, m.Nim, m.Nama AS NamaMahasiswa, t.Judul AS JudulTA,
               d1.Nama AS Penguji1Nama, d2.Nama AS Penguji2Nama
        FROM JadwalSidang s
        JOIN TugasAkhir t ON t.Id = s.TugasAkhirId
        JOIN Mahasiswa m ON m.Id = t.MahasiswaId
        JOIN Dosen d1 ON d1.Id = s.Penguji1Id
        JOIN Dosen d2 ON d2.Id = s.Penguji2Id
        """;

    public IReadOnlyList<JadwalSidang> Search(string? keyword = null, bool upcomingOnly = false, int? dosenId = null)
    {
        using var conn = _factory.Open();
        return conn.Query<JadwalSidang>(
            SelectSql + """

            WHERE (@Keyword IS NULL OR m.Nim LIKE @Keyword ESCAPE '\' OR m.Nama LIKE @Keyword ESCAPE '\' OR s.Ruang LIKE @Keyword ESCAPE '\')
              AND (@UpcomingOnly = 0 OR s.Waktu >= @Now)
              AND (@DosenId IS NULL OR t.Pembimbing1Id = @DosenId OR t.Pembimbing2Id = @DosenId
                   OR s.Penguji1Id = @DosenId OR s.Penguji2Id = @DosenId)
            ORDER BY s.Waktu DESC
            """,
            new
            {
                Keyword = SqlHelpers.ContainsPattern(keyword), UpcomingOnly = upcomingOnly ? 1 : 0,
                Now = DateTime.Today, DosenId = dosenId
            }).AsList();
    }

    public JadwalSidang? GetById(int id)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<JadwalSidang>(SelectSql + " WHERE s.Id = @Id", new { Id = id });
    }

    public bool HasRoomConflict(string ruang, DateTime waktu, int excludeId = 0)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM JadwalSidang WHERE Ruang = @Ruang COLLATE NOCASE AND Waktu = @Waktu AND Id <> @ExcludeId",
            new { Ruang = ruang.Trim(), Waktu = waktu, ExcludeId = excludeId }) > 0;
    }

    public int Insert(JadwalSidang sidang)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO JadwalSidang (TugasAkhirId, Jenis, Waktu, Ruang, Penguji1Id, Penguji2Id, Hasil)
            VALUES (@TugasAkhirId, @Jenis, @Waktu, @Ruang, @Penguji1Id, @Penguji2Id, @Hasil); SELECT last_insert_rowid();
            """, sidang);
    }

    public void Update(JadwalSidang sidang)
    {
        using var conn = _factory.Open();
        conn.Execute(
            """
            UPDATE JadwalSidang SET TugasAkhirId = @TugasAkhirId, Jenis = @Jenis, Waktu = @Waktu, Ruang = @Ruang,
                   Penguji1Id = @Penguji1Id, Penguji2Id = @Penguji2Id, Hasil = @Hasil
            WHERE Id = @Id
            """, sidang);
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM JadwalSidang WHERE Id = @Id", new { Id = id });
    }
}
