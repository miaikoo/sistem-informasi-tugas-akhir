using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IActivityLogRepository
{
    void Add(ActivityLog log);
    /// <param name="userId">When set, only activity by this user.</param>
    IReadOnlyList<ActivityLog> GetRecent(int limit, int? userId = null);
}

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly IDbConnectionFactory _factory;

    public ActivityLogRepository(IDbConnectionFactory factory) => _factory = factory;

    public void Add(ActivityLog log)
    {
        using var conn = _factory.Open();
        conn.Execute(
            """
            INSERT INTO ActivityLog (UserId, Username, Aksi, Entitas, Deskripsi, CreatedAt)
            VALUES (@UserId, @Username, @Aksi, @Entitas, @Deskripsi, @CreatedAt)
            """, log);
    }

    public IReadOnlyList<ActivityLog> GetRecent(int limit, int? userId = null)
    {
        using var conn = _factory.Open();
        return conn.Query<ActivityLog>(
            """
            SELECT * FROM ActivityLog
            WHERE @UserId IS NULL OR UserId = @UserId
            ORDER BY CreatedAt DESC, Id DESC
            LIMIT @Limit
            """,
            new { Limit = limit, UserId = userId }).AsList();
    }
}
