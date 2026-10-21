using Microsoft.Data.Sqlite;
using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

/// <summary>Outcome of a write operation: either success, or a list of user-facing error messages.</summary>
public class OperationResult
{
    public bool Success => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }
    public int? NewId { get; init; }

    private OperationResult(IReadOnlyList<string> errors) => Errors = errors;

    public static OperationResult Ok(int? newId = null) => new([]) { NewId = newId };
    public static OperationResult Fail(params string[] errors) => new(errors);
    public static OperationResult Fail(IEnumerable<string> errors) => new(errors.ToList());

    public string ErrorText => string.Join(Environment.NewLine, Errors);
}

/// <summary>Holds the logged-in user for the lifetime of a session.</summary>
public class UserSession
{
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
    public int? DosenId => CurrentUser?.Role == UserRole.Dosen ? CurrentUser.DosenId : null;

    /// <summary>Scope for queries: null for admins (see everything), the lecturer's id for dosen.</summary>
    public int? DosenScope => IsAdmin ? null : DosenId ?? -1;

    public void SignIn(User user) => CurrentUser = user;
    public void SignOut() => CurrentUser = null;
}

public class AccessDeniedException : Exception
{
    public AccessDeniedException(string message = "Anda tidak memiliki hak akses untuk melakukan tindakan ini.")
        : base(message) { }
}

/// <summary>Translates low-level SQLite failures into messages a non-technical user can act on.</summary>
public static class DbErrorTranslator
{
    private const int SqliteConstraint = 19;
    private const int ConstraintForeignKey = 787;
    private const int ConstraintRestrict = 1811; // ON DELETE RESTRICT is enforced via an internal trigger
    private const int ConstraintUnique = 2067;

    public static string Translate(Exception ex) => ex switch
    {
        AccessDeniedException a => a.Message,
        SqliteException { SqliteExtendedErrorCode: ConstraintForeignKey or ConstraintRestrict } =>
            "Data tidak dapat dihapus atau disimpan karena masih terkait dengan data lain.",
        SqliteException { SqliteExtendedErrorCode: ConstraintUnique } =>
            "Data yang sama sudah terdaftar.",
        SqliteException { SqliteErrorCode: SqliteConstraint } =>
            "Data tidak memenuhi aturan database.",
        SqliteException { SqliteErrorCode: 5 or 6 } =>
            "Database sedang digunakan proses lain. Coba beberapa saat lagi.",
        SqliteException =>
            "Terjadi kesalahan saat mengakses database. Pastikan file database tidak rusak atau terkunci.",
        _ => "Terjadi kesalahan yang tidak terduga: " + ex.Message
    };
}

public class ActivityLogger
{
    private readonly IActivityLogRepository _repo;
    private readonly UserSession _session;

    public ActivityLogger(IActivityLogRepository repo, UserSession session)
    {
        _repo = repo;
        _session = session;
    }

    public void Log(string aksi, string entitas, string deskripsi)
    {
        var user = _session.CurrentUser;
        _repo.Add(new ActivityLog
        {
            UserId = user?.Id,
            Username = user?.Username ?? "sistem",
            Aksi = aksi,
            Entitas = entitas,
            Deskripsi = deskripsi,
            CreatedAt = DateTime.Now
        });
    }
}

/// <summary>Shared plumbing for services: authorization guards and exception-to-result translation.</summary>
public abstract class ServiceBase
{
    protected UserSession Session { get; }
    protected ActivityLogger Logger { get; }

    protected ServiceBase(UserSession session, ActivityLogger logger)
    {
        Session = session;
        Logger = logger;
    }

    protected void RequireLogin()
    {
        if (!Session.IsAuthenticated) throw new AccessDeniedException("Sesi Anda telah berakhir. Silakan login kembali.");
    }

    protected void RequireAdmin()
    {
        RequireLogin();
        if (!Session.IsAdmin) throw new AccessDeniedException();
    }

    /// <summary>Runs a write operation, converting expected failures into an <see cref="OperationResult"/>.</summary>
    protected static OperationResult Execute(Func<OperationResult> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is SqliteException or AccessDeniedException)
        {
            return OperationResult.Fail(DbErrorTranslator.Translate(ex));
        }
    }
}
