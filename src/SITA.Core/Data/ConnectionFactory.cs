using System.Data;
using Microsoft.Data.Sqlite;

namespace SITA.Core.Data;

public interface IDbConnectionFactory
{
    /// <summary>Returns an open connection with foreign keys enforced.</summary>
    IDbConnection Open();
}

public sealed class SqliteConnectionFactory : IDbConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _keepAlive;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
        // A shared in-memory database only lives as long as at least one connection is open.
        if (connectionString.Contains("mode=memory", StringComparison.OrdinalIgnoreCase))
        {
            _keepAlive = new SqliteConnection(connectionString);
            _keepAlive.Open();
        }
    }

    public static SqliteConnectionFactory ForFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        };
        return new SqliteConnectionFactory(builder.ToString());
    }

    public static SqliteConnectionFactory InMemory(string name) =>
        new($"Data Source=file:{name}?mode=memory&cache=shared;Foreign Keys=True");

    public IDbConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public void Dispose() => _keepAlive?.Dispose();
}
