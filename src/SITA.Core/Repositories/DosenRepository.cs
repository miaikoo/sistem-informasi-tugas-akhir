using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IDosenRepository
{
    IReadOnlyList<Dosen> Search(string? keyword = null, int? prodiId = null);
    Dosen? GetById(int id);
    bool ExistsNip(string nip, int excludeId = 0);
    int Insert(Dosen dosen);
    void Update(Dosen dosen);
    void Delete(int id);
}

public class DosenRepository : IDosenRepository
{
    private readonly IDbConnectionFactory _factory;

    public DosenRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectSql = """
        SELECT d.*, p.Nama AS ProdiNama,
               (SELECT COUNT(*) FROM TugasAkhir t
                 WHERE (t.Pembimbing1Id = d.Id OR t.Pembimbing2Id = d.Id) AND t.Tahap < 5) AS JumlahBimbingan
        FROM Dosen d
        JOIN ProgramStudi p ON p.Id = d.ProdiId
        """;

    public IReadOnlyList<Dosen> Search(string? keyword = null, int? prodiId = null)
    {
        using var conn = _factory.Open();
        return conn.Query<Dosen>(
            SelectSql + """

            WHERE (@Keyword IS NULL OR d.Nip LIKE @Keyword ESCAPE '\' OR d.Nama LIKE @Keyword ESCAPE '\')
              AND (@ProdiId IS NULL OR d.ProdiId = @ProdiId)
            ORDER BY d.Nama
            """,
            new { Keyword = SqlHelpers.ContainsPattern(keyword), ProdiId = prodiId }).AsList();
    }

    public Dosen? GetById(int id)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<Dosen>(SelectSql + " WHERE d.Id = @Id", new { Id = id });
    }

    public bool ExistsNip(string nip, int excludeId = 0)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Dosen WHERE Nip = @Nip AND Id <> @ExcludeId",
            new { Nip = nip, ExcludeId = excludeId }) > 0;
    }

    public int Insert(Dosen dosen)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Dosen (Nip, Nama, Email, ProdiId, BidangKeahlian)
            VALUES (@Nip, @Nama, @Email, @ProdiId, @BidangKeahlian); SELECT last_insert_rowid();
            """, dosen);
    }

    public void Update(Dosen dosen)
    {
        using var conn = _factory.Open();
        conn.Execute(
            """
            UPDATE Dosen SET Nip = @Nip, Nama = @Nama, Email = @Email, ProdiId = @ProdiId, BidangKeahlian = @BidangKeahlian
            WHERE Id = @Id
            """, dosen);
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM Dosen WHERE Id = @Id", new { Id = id });
    }
}
