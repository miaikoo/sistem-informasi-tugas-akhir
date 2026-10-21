using Microsoft.Data.Sqlite;
using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class AuthService
{
    public const string InvalidCredentialsMessage = "Username atau password salah.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly UserSession _session;
    private readonly ActivityLogger _logger;

    public AuthService(IUserRepository users, IPasswordHasher hasher, UserSession session, ActivityLogger logger)
    {
        _users = users;
        _hasher = hasher;
        _session = session;
        _logger = logger;
    }

    public OperationResult Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return OperationResult.Fail("Username dan password wajib diisi.");

        try
        {
            var user = _users.GetByUsername(username);
            // Same message for unknown user and wrong password so usernames cannot be probed.
            if (user is null || !_hasher.Verify(password, user.PasswordHash))
                return OperationResult.Fail(InvalidCredentialsMessage);

            if (!user.IsActive)
                return OperationResult.Fail("Akun Anda dinonaktifkan. Hubungi admin program studi.");

            if (user.Role == UserRole.Dosen && user.DosenId is null)
                return OperationResult.Fail("Akun dosen belum ditautkan ke data dosen. Hubungi admin program studi.");

            _session.SignIn(user);
            _logger.Log("Login", "Pengguna", $"{user.NamaLengkap} masuk ke aplikasi");
            return OperationResult.Ok(user.Id);
        }
        catch (SqliteException ex)
        {
            return OperationResult.Fail(DbErrorTranslator.Translate(ex));
        }
    }

    public void Logout()
    {
        if (_session.CurrentUser is { } user)
        {
            try
            {
                _logger.Log("Logout", "Pengguna", $"{user.NamaLengkap} keluar dari aplikasi");
            }
            catch (SqliteException)
            {
                // Logging out must still succeed even if the audit write fails.
            }
        }
        _session.SignOut();
    }
}
