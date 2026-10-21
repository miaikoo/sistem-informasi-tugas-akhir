using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class UserService : ServiceBase
{
    private readonly IUserRepository _repo;
    private readonly IDosenRepository _dosen;
    private readonly IPasswordHasher _hasher;

    public UserService(IUserRepository repo, IDosenRepository dosen, IPasswordHasher hasher, UserSession session, ActivityLogger logger)
        : base(session, logger)
    {
        _repo = repo;
        _dosen = dosen;
        _hasher = hasher;
    }

    public IReadOnlyList<User> Search(string? keyword = null)
    {
        RequireAdmin();
        return _repo.Search(keyword);
    }

    /// <param name="password">Required for new accounts; for existing accounts an empty value keeps the current password.</param>
    public OperationResult Save(User input, string? password) => Execute(() =>
    {
        RequireAdmin();
        var u = input.Clone();
        u.Username = u.Username.Trim();
        u.NamaLengkap = u.NamaLengkap.Trim();
        if (u.Role == UserRole.Admin) u.DosenId = null;

        bool isNew = u.Id == 0;
        var errors = Validators.ValidateUser(u, password, isNew);
        if (errors.Count == 0)
        {
            if (_repo.ExistsUsername(u.Username, u.Id)) errors.Add("Username sudah digunakan.");
            if (u.DosenId is { } dosenId && _dosen.GetById(dosenId) is null) errors.Add("Data dosen yang dipilih tidak ditemukan.");
            if (u.Id == Session.CurrentUser!.Id && (!u.IsActive || u.Role != UserRole.Admin))
                errors.Add("Anda tidak dapat menonaktifkan akun sendiri atau menghapus peran admin Anda.");
        }
        if (errors.Count > 0) return OperationResult.Fail(errors);

        if (isNew)
        {
            u.PasswordHash = _hasher.Hash(password!);
            int id = _repo.Insert(u);
            Logger.Log("Tambah", "Pengguna", $"Membuat akun {u.Username} ({u.RoleLabel})");
            return OperationResult.Ok(id);
        }

        _repo.Update(u);
        if (!string.IsNullOrEmpty(password))
            _repo.UpdatePassword(u.Id, _hasher.Hash(password));
        Logger.Log("Ubah", "Pengguna", string.IsNullOrEmpty(password)
            ? $"Mengubah akun {u.Username}"
            : $"Mengubah akun {u.Username} dan mereset password");
        return OperationResult.Ok();
    });

    public OperationResult Delete(User u) => Execute(() =>
    {
        RequireAdmin();
        if (u.Id == Session.CurrentUser!.Id) return OperationResult.Fail("Anda tidak dapat menghapus akun sendiri.");
        _repo.Delete(u.Id);
        Logger.Log("Hapus", "Pengguna", $"Menghapus akun {u.Username}");
        return OperationResult.Ok();
    });
}
