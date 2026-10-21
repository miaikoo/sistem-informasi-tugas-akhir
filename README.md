# SITA — Sistem Informasi Tugas Akhir

Aplikasi desktop (WPF, .NET 8) untuk memantau tugas akhir mahasiswa: pengajuan judul, log bimbingan, jadwal sidang, dan dashboard progres. Kebutuhan produk lengkap ada di [PRD.md](PRD.md).

## Menjalankan

Prasyarat: Windows 10/11 dan [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run --project src/SITA.App
```

Secara default aplikasi berjalan dalam **mode demo**: data dummy (4 prodi, 10 dosen, 60 mahasiswa, 44 TA beserta bimbingan dan jadwal sidang) dibuat di memori setiap kali aplikasi dibuka. Tidak ada file database, dan semua perubahan hilang saat aplikasi ditutup.

Untuk menyimpan data secara permanen, jalankan dengan flag `--db`. Database SQLite akan dibuat di `%LOCALAPPDATA%\SITA\sita.db` dan diisi data contoh yang sama saat pertama kali dipakai:

```bash
dotnet run --project src/SITA.App -- --db
```

| Peran | Username | Password |
|---|---|---|
| Admin prodi | `admin` | `admin123` |
| Dosen (contoh) | `budi` | `dosen123` |

Semua akun dosen contoh (`ahmad`, `rina`, `budi`, `dewi`, `hendra`, `siti`, `agus`, `maya`, `rudi`, `lina`) memakai password `dosen123`.

## Menjalankan test

```bash
dotnet test
```

## Struktur

```
src/SITA.Core     Model, skema & seeder database, repositori (Dapper, query berparameter),
                  layanan (validasi, otorisasi per peran, audit log, hash BCrypt)
src/SITA.App      UI WPF (MVVM): tema terang/gelap, view, view model
tests/            xUnit — validasi, login, hak akses, aturan bisnis, dashboard (SQLite in-memory)
```
