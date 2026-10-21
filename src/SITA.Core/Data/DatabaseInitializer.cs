using Dapper;

namespace SITA.Core.Data;

public class DatabaseInitializer
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS ProgramStudi (
            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
            Kode     TEXT NOT NULL UNIQUE COLLATE NOCASE,
            Nama     TEXT NOT NULL,
            Jenjang  TEXT NOT NULL DEFAULT 'S1'
        );

        CREATE TABLE IF NOT EXISTS Dosen (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            Nip             TEXT NOT NULL UNIQUE,
            Nama            TEXT NOT NULL,
            Email           TEXT NOT NULL DEFAULT '',
            ProdiId         INTEGER NOT NULL REFERENCES ProgramStudi(Id) ON DELETE RESTRICT,
            BidangKeahlian  TEXT NOT NULL DEFAULT ''
        );

        CREATE TABLE IF NOT EXISTS Mahasiswa (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            Nim           TEXT NOT NULL UNIQUE,
            Nama          TEXT NOT NULL,
            Email         TEXT NOT NULL DEFAULT '',
            ProdiId       INTEGER NOT NULL REFERENCES ProgramStudi(Id) ON DELETE RESTRICT,
            Angkatan      INTEGER NOT NULL,
            JenisKelamin  TEXT NOT NULL DEFAULT 'L' CHECK (JenisKelamin IN ('L','P')),
            TempatLahir   TEXT NOT NULL DEFAULT '',
            TanggalLahir  TEXT NULL,
            Alamat        TEXT NOT NULL DEFAULT '',
            Telepon       TEXT NOT NULL DEFAULT '',
            DosenWaliId   INTEGER NULL REFERENCES Dosen(Id) ON DELETE SET NULL,
            Status        INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS TugasAkhir (
            Id             INTEGER PRIMARY KEY AUTOINCREMENT,
            MahasiswaId    INTEGER NOT NULL UNIQUE REFERENCES Mahasiswa(Id) ON DELETE RESTRICT,
            Judul          TEXT NOT NULL,
            BidangMinat    TEXT NOT NULL DEFAULT '',
            Pembimbing1Id  INTEGER NOT NULL REFERENCES Dosen(Id) ON DELETE RESTRICT,
            Pembimbing2Id  INTEGER NULL REFERENCES Dosen(Id) ON DELETE RESTRICT,
            Tahap          INTEGER NOT NULL DEFAULT 0,
            TanggalMulai   TEXT NOT NULL,
            UpdatedAt      TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Bimbingan (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            TugasAkhirId  INTEGER NOT NULL REFERENCES TugasAkhir(Id) ON DELETE CASCADE,
            DosenId       INTEGER NOT NULL REFERENCES Dosen(Id) ON DELETE RESTRICT,
            Tanggal       TEXT NOT NULL,
            Topik         TEXT NOT NULL,
            Catatan       TEXT NOT NULL DEFAULT '',
            Status        INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS JadwalSidang (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            TugasAkhirId  INTEGER NOT NULL REFERENCES TugasAkhir(Id) ON DELETE CASCADE,
            Jenis         INTEGER NOT NULL,
            Waktu         TEXT NOT NULL,
            Ruang         TEXT NOT NULL,
            Penguji1Id    INTEGER NOT NULL REFERENCES Dosen(Id) ON DELETE RESTRICT,
            Penguji2Id    INTEGER NOT NULL REFERENCES Dosen(Id) ON DELETE RESTRICT,
            Hasil         INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS Users (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            Username      TEXT NOT NULL UNIQUE COLLATE NOCASE,
            PasswordHash  TEXT NOT NULL,
            NamaLengkap   TEXT NOT NULL,
            Role          INTEGER NOT NULL,
            DosenId       INTEGER NULL REFERENCES Dosen(Id) ON DELETE RESTRICT,
            IsActive      INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS ActivityLog (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId     INTEGER NULL,
            Username   TEXT NOT NULL,
            Aksi       TEXT NOT NULL,
            Entitas    TEXT NOT NULL,
            Deskripsi  TEXT NOT NULL,
            CreatedAt  TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_Mahasiswa_Nama ON Mahasiswa(Nama COLLATE NOCASE);
        CREATE INDEX IF NOT EXISTS IX_Mahasiswa_Angkatan ON Mahasiswa(Angkatan);
        CREATE INDEX IF NOT EXISTS IX_Mahasiswa_Prodi ON Mahasiswa(ProdiId);
        CREATE INDEX IF NOT EXISTS IX_Bimbingan_TA ON Bimbingan(TugasAkhirId);
        CREATE INDEX IF NOT EXISTS IX_Bimbingan_Tanggal ON Bimbingan(Tanggal);
        CREATE INDEX IF NOT EXISTS IX_Sidang_Waktu ON JadwalSidang(Waktu);
        CREATE INDEX IF NOT EXISTS IX_Activity_CreatedAt ON ActivityLog(CreatedAt);
        """;

    private readonly IDbConnectionFactory _factory;

    public DatabaseInitializer(IDbConnectionFactory factory) => _factory = factory;

    public void EnsureCreated()
    {
        using var conn = _factory.Open();
        conn.Execute(Schema);
    }

    public bool IsEmpty()
    {
        using var conn = _factory.Open();
        return conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Users") == 0;
    }
}