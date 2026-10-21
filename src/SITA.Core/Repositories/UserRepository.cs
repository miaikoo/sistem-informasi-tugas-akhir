using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IUserRepository
{
    User? GetByUsername(string username);
    User? GetById(int id);
    IReadOnlyList<User> Search(string? keyword = null);
    bool ExistsUsername(string username, int excludeId = 0);
    int Insert(User user);
    /// <summary>Updates profile fields only; the password is changed via <see cref="UpdatePassword"/>.</summary>
    void Update(User user);
    void UpdatePassword(int id, string passwordHash);
    void Delete(int id);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string SelectSql = """
        SELECT u.*, d.Nama AS DosenNama
        FROM Users u
        LEFT JOIN Dosen d ON d.Id = u.DosenId
        """;

    public User? GetByUsername(string username)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<User>(SelectSql + " WHERE u.Username = @Username COLLATE NOCASE",
            new { Username = username.Trim() });
    }

    public User? GetById(int id)
    {
        using var conn = _factory.Open();
        return conn.QuerySingleOrDefault<User>(SelectSql + " WHERE u.Id = @Id", new { Id = id });
    }

    public IReadOnlyList<User> Search(string? keyword = null)
    {
        using var conn = _factory.Open();
        return conn.Query<User>(
            SelectSql + """

            WHERE @Keyword IS NULL OR u.Username LIKE @Keyword ESCAPE '\' OR u.NamaLengkap LIKE @Keyword ESCAPE '\'
            ORDER BY u.Role, u.Username
            """,
            new { Keyword = SqlHelpers.ContainsPattern(keyword) }).AsList();
    }

    public bool ExistsUsername(string username, int excludeId = 0)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE Username = @Username COLLATE NOCASE AND Id <> @ExcludeId",
            new { Username = username.Trim(), ExcludeId = excludeId }) > 0;
    }

    public int Insert(User user)
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Users (Username, PasswordHash, NamaLengkap, Role, DosenId, IsActive)
            VALUES (@Username, @PasswordHash, @NamaLengkap, @Role, @DosenId, @IsActive); SELECT last_insert_rowid();
            """, user);
    }

    public void Update(User user)
    {
        using var conn = _factory.Open();
        conn.Execute(
            """
            UPDATE Users SET Username = @Username, NamaLengkap = @NamaLengkap, Role = @Role,
                   DosenId = @DosenId, IsActive = @IsActive
            WHERE Id = @Id
            """, user);
    }

    public void UpdatePassword(int id, string passwordHash)
    {
        using var conn = _factory.Open();
        conn.Execute("UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id", new { Id = id, Hash = passwordHash });
    }

    public void Delete(int id)
    {
        using var conn = _factory.Open();
        conn.Execute("DELETE FROM Users WHERE Id = @Id", new { Id = id });
    }
}
