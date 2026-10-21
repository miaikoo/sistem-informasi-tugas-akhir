# PRD — SITA (Sistem Informasi Tugas Akhir)

**Rich UI – Aplikasi Desktop Monitoring Tugas Akhir Mahasiswa**

SITA (Sistem Informasi Tugas Akhir) adalah aplikasi desktop untuk membantu program studi memantau perjalanan tugas akhir (TA) mahasiswa, mulai dari pengajuan judul, bimbingan, sampai sidang akhir. Produk dirancang dengan pendekatan Rich UI yang menampilkan navigasi sidebar, kartu statistik, tabel data, grafik progres, aktivitas terbaru, formulir terstruktur, dan pilihan tema gelap/terang. PRD ini menjabarkan kebutuhan produk yang dapat direncanakan, dibangun, dan diuji.

---

## 1. Ringkasan Produk

### 1.1 Tujuan Produk

- Menyediakan satu aplikasi desktop untuk melihat dan mengelola data tugas akhir, mahasiswa, dosen pembimbing, bimbingan, dan jadwal sidang.
- Mempercepat pekerjaan administratif program studi melalui pencarian, filter, dan operasi CRUD.
- Memberi dosen pembimbing gambaran cepat tentang progres mahasiswa bimbingannya dan memudahkan pencatatan log bimbingan.
- Menyajikan ringkasan data (jumlah mahasiswa TA, sebaran tahap, intensitas bimbingan, beban dosen) dalam bentuk kartu statistik dan grafik.
- Memberikan pengalaman penggunaan yang konsisten, jelas, dan mudah dipelajari.
- Menerapkan pemisahan kode yang terstruktur (UI – logika bisnis – akses data) agar aplikasi mudah diuji dan dikembangkan.

### 1.2 Pernyataan Produk

Untuk **staf/admin program studi** dan **dosen pembimbing** yang perlu memantau progres tugas akhir mahasiswa, **SITA** menyediakan aplikasi desktop terintegrasi yang menggabungkan manajemen data TA, pencatatan bimbingan, penjadwalan sidang, dan ringkasan analitik dalam satu lingkungan kerja dengan UI modern.

---

## 2. Latar Belakang dan Permasalahan

- Data TA (judul, pembimbing, tahap) sering tersebar di spreadsheet, formulir kertas, dan chat, sehingga sulit mengetahui posisi terakhir setiap mahasiswa.
- Log bimbingan dicatat manual di kartu bimbingan sehingga sulit direkap dan rawan hilang.
- Program studi membutuhkan gambaran cepat: berapa mahasiswa yang sedang TA, berapa yang sudah sidang, dan apakah beban bimbingan antar dosen merata.
- Dosen membutuhkan daftar mahasiswa bimbingannya beserta riwayat bimbingan tanpa harus membuka banyak berkas.
- Tampilan aplikasi administrasi yang padat perlu memiliki hierarki visual dan navigasi yang konsisten.
- Perubahan data perlu divalidasi, dicatat (audit log), dan dilindungi dari akses yang tidak berwenang.

---

## 3. Sasaran, Indikator, dan Batasan

| Sasaran | Indikator keberhasilan awal |
|---|---|
| Pengelolaan data TA | Admin dapat menambah, melihat, mengubah, dan menghapus data mahasiswa, dosen, program studi, TA, bimbingan, dan sidang. |
| Pencarian data | Pencarian berdasarkan NIM, nama mahasiswa, atau judul TA menghasilkan data yang relevan; tersedia filter prodi, angkatan, tahap, dan pembimbing. |
| Monitoring progres | Setiap TA memiliki tahap yang jelas (Pengajuan → Proposal → Penelitian → Seminar Hasil → Sidang → Lulus) dan dapat diperbarui. |
| Dashboard | Kartu statistik dan grafik dihitung dari database, bukan angka statis. |
| Kualitas UI | Navigasi utama konsisten, teks terbaca, dan tampilan mendukung dark/light mode. |
| Keandalan | Kesalahan validasi atau koneksi database ditangani dengan pesan yang dapat dipahami pengguna. |
| Keamanan dasar | Akses berbasis peran (Admin, Dosen), query berparameter, dan penyimpanan password dengan hash (BCrypt). |

**Batasan**

- Aplikasi berjalan di Windows (desktop), satu pengguna per sesi, dengan database lokal SQLite (diperbolehkan memakai database selain SQL Server).
- Angka yang muncul pada data contoh (misalnya 60 mahasiswa, 10 dosen) hanyalah data awal untuk demo. Semua angka di dashboard dihitung langsung dari database.
- Unggah berkas dokumen TA, notifikasi email, dan portal mahasiswa berada di luar cakupan versi ini (lihat bagian 11).

---

## 4. Pengguna dan Peran

| Peran | Deskripsi | Hak akses |
|---|---|---|
| **Admin (Staf Prodi)** | Mengelola seluruh data akademik terkait TA. | Akses penuh: CRUD program studi, dosen, mahasiswa, TA, bimbingan, jadwal sidang, dan akun pengguna. Melihat dashboard seluruh prodi dan semua aktivitas. |
| **Dosen Pembimbing** | Membimbing dan memantau mahasiswa TA. | Melihat dashboard dan TA mahasiswa bimbingannya saja; mengubah judul dan tahap TA mahasiswa bimbingannya; CRUD log bimbingan miliknya; melihat jadwal sidang yang melibatkan dirinya (sebagai pembimbing atau penguji). |

Pembatasan akses diterapkan di dua lapis: menu yang tidak berhak disembunyikan di UI, **dan** setiap operasi diperiksa ulang di lapisan layanan (service) sehingga tidak dapat dilewati dari UI.

---

## 5. Ruang Lingkup Fitur

1. **Autentikasi** — login dengan username dan password, logout, sesi berbasis peran.
2. **Dashboard** — kartu statistik, grafik sebaran tahap TA, grafik bimbingan per bulan, grafik beban bimbingan dosen, jadwal sidang terdekat, dan aktivitas terbaru.
3. **Manajemen Mahasiswa** — CRUD, pencarian NIM/nama, filter prodi dan angkatan.
4. **Manajemen Dosen** — CRUD, pencarian NIP/nama, filter prodi.
5. **Manajemen Program Studi** — CRUD master data prodi.
6. **Manajemen Tugas Akhir** — CRUD data TA (judul, bidang minat, pembimbing 1 & 2, tahap), pencarian dan filter tahap/pembimbing.
7. **Log Bimbingan** — pencatatan tanggal, topik, catatan, dan status bimbingan (Menunggu / Disetujui / Revisi).
8. **Jadwal Sidang** — penjadwalan seminar proposal, seminar hasil, dan sidang akhir beserta ruang, penguji, dan hasil.
9. **Manajemen Pengguna** — CRUD akun, penetapan peran, penautan akun ke data dosen, reset password, aktif/nonaktif.
10. **Tema** — dark mode dan light mode yang tersimpan antar sesi.
11. **Audit Log** — pencatatan setiap login dan perubahan data.

---

## 6. Kebutuhan Fungsional

### 6.1 Autentikasi dan Otorisasi

| ID | Kebutuhan |
|---|---|
| FR-01 | Pengguna login dengan username dan password. Password dicocokkan dengan hash BCrypt yang tersimpan. |
| FR-02 | Login gagal menampilkan pesan umum "Username atau password salah" tanpa membocorkan bagian mana yang salah. |
| FR-03 | Akun yang dinonaktifkan tidak dapat login. |
| FR-04 | Menu dan tombol aksi menyesuaikan peran pengguna yang login. |
| FR-05 | Setiap operasi tulis memeriksa peran dan kepemilikan data di lapisan layanan. |
| FR-06 | Pengguna dapat logout dan kembali ke layar login. |

### 6.2 Dashboard

| ID | Kebutuhan |
|---|---|
| FR-10 | Menampilkan 4 kartu statistik: **Mahasiswa TA Aktif**, **Bimbingan Bulan Ini**, **Sidang 30 Hari ke Depan**, **Mahasiswa Lulus**. |
| FR-11 | Grafik batang sebaran TA per tahap. |
| FR-12 | Grafik batang jumlah bimbingan per bulan selama 6 bulan terakhir. |
| FR-13 | (Admin) Grafik beban bimbingan per dosen (jumlah mahasiswa bimbingan aktif). |
| FR-14 | Daftar jadwal sidang terdekat. |
| FR-15 | Daftar aktivitas terbaru dari audit log. |
| FR-16 | Untuk Dosen, seluruh angka dan grafik hanya mencakup mahasiswa bimbingannya. |
| FR-17 | Semua angka dihitung dari database setiap kali dashboard dibuka atau di-refresh. |

### 6.3 Data Master (Mahasiswa, Dosen, Program Studi)

| ID | Kebutuhan |
|---|---|
| FR-20 | Admin dapat menambah, melihat, mengubah, dan menghapus data mahasiswa (NIM, nama, email, prodi, angkatan). |
| FR-21 | NIM wajib unik, 10–14 digit angka. |
| FR-22 | Pencarian mahasiswa berdasarkan NIM atau nama; filter berdasarkan prodi dan angkatan. |
| FR-23 | Admin dapat CRUD data dosen (NIP, nama, email, prodi, bidang keahlian). NIP wajib unik, 18 digit angka. |
| FR-24 | Admin dapat CRUD program studi (kode, nama, jenjang). Kode wajib unik. |
| FR-25 | Data yang masih dirujuk (misalnya dosen yang masih menjadi pembimbing) tidak dapat dihapus, dan sistem menampilkan alasannya. |

### 6.4 Tugas Akhir

| ID | Kebutuhan |
|---|---|
| FR-30 | Admin dapat membuat TA untuk mahasiswa yang belum memiliki TA, dengan judul, bidang minat, pembimbing 1 (wajib), pembimbing 2 (opsional), dan tanggal mulai. |
| FR-31 | Pembimbing 1 dan pembimbing 2 tidak boleh orang yang sama. |
| FR-32 | Tahap TA: Pengajuan, Proposal, Penelitian, Seminar Hasil, Sidang, Lulus. |
| FR-33 | Dosen dapat mengubah judul dan tahap TA mahasiswa bimbingannya, tetapi tidak dapat mengganti pembimbing, membuat, atau menghapus TA. |
| FR-34 | Pencarian berdasarkan NIM, nama mahasiswa, atau judul; filter tahap dan pembimbing. |
| FR-35 | Tabel TA menampilkan jumlah bimbingan dan tanggal bimbingan terakhir. |

### 6.5 Log Bimbingan

| ID | Kebutuhan |
|---|---|
| FR-40 | Mencatat bimbingan: TA, tanggal, topik, catatan, status (Menunggu/Disetujui/Revisi). |
| FR-41 | Tanggal bimbingan tidak boleh di masa depan. Topik wajib diisi. |
| FR-42 | Dosen hanya dapat menambah dan mengubah bimbingan untuk TA yang dibimbingnya; pencatat otomatis diisi dosen yang login. |
| FR-43 | Admin dapat mencatat bimbingan atas nama salah satu pembimbing TA tersebut. |
| FR-44 | Filter berdasarkan kata kunci (mahasiswa/topik) dan status. |

### 6.6 Jadwal Sidang

| ID | Kebutuhan |
|---|---|
| FR-50 | Admin dapat menjadwalkan sidang: TA, jenis (Seminar Proposal, Seminar Hasil, Sidang Akhir), tanggal & jam, ruang, penguji 1 & 2, hasil. |
| FR-51 | Penguji tidak boleh pembimbing dari TA yang sama, dan penguji 1 ≠ penguji 2. |
| FR-52 | Ruang yang sama tidak boleh dipakai untuk dua sidang pada tanggal dan jam yang sama. |
| FR-53 | Dosen dapat melihat jadwal sidang yang melibatkan dirinya. |
| FR-54 | Filter "hanya yang akan datang". |

### 6.7 Pengguna, Tema, dan Audit

| ID | Kebutuhan |
|---|---|
| FR-60 | Admin dapat CRUD akun pengguna, memilih peran, menautkan akun Dosen ke data dosen, mereset password, dan menonaktifkan akun. |
| FR-61 | Password minimal 6 karakter dan selalu disimpan dalam bentuk hash. |
| FR-62 | Admin tidak dapat menghapus atau menonaktifkan akunnya sendiri. |
| FR-63 | Pengguna dapat beralih antara dark mode dan light mode; pilihan disimpan dan dipakai kembali saat aplikasi dibuka. |
| FR-64 | Setiap login dan operasi tambah/ubah/hapus dicatat di audit log (siapa, apa, kapan). |

---

## 7. Kebutuhan Antarmuka (Rich UI)

- **Layout utama:** sidebar kiri (logo SITA, menu navigasi dengan ikon, info pengguna, tombol logout) dan area konten dengan header (judul halaman, subjudul, tombol tema).
- **Dashboard:** baris kartu statistik berwarna aksen, grafik batang, panel "Sidang Terdekat", dan panel "Aktivitas Terbaru".
- **Halaman data:** toolbar (kotak pencarian, filter dropdown, tombol Tambah/Refresh), tabel data dengan header jelas dan baris bergantian, badge berwarna untuk tahap/status, serta panel formulir geser di sisi kanan untuk tambah/ubah.
- **Formulir:** label di atas field, field wajib ditandai, pesan validasi ditampilkan di dalam formulir, tombol Simpan/Batal konsisten.
- **Konfirmasi:** penghapusan selalu meminta konfirmasi.
- **Tema:** palet warna light dan dark yang dapat dialihkan tanpa restart; kontras teks tetap terbaca di kedua tema.
- **Konsistensi:** ikon dari satu set (Segoe Fluent Icons), radius sudut, jarak, dan tipografi seragam.

---

## 8. Model Data

| Entitas | Atribut utama | Relasi |
|---|---|---|
| ProgramStudi | Id, Kode (unik), Nama, Jenjang | 1–N Mahasiswa, 1–N Dosen |
| Dosen | Id, NIP (unik), Nama, Email, ProdiId, BidangKeahlian | Pembimbing/penguji TA |
| Mahasiswa | Id, NIM (unik), Nama, Email, ProdiId, Angkatan | 1–0..1 TugasAkhir |
| TugasAkhir | Id, MahasiswaId (unik), Judul, BidangMinat, Pembimbing1Id, Pembimbing2Id?, Tahap, TanggalMulai, UpdatedAt | 1–N Bimbingan, 1–N JadwalSidang |
| Bimbingan | Id, TugasAkhirId, DosenId, Tanggal, Topik, Catatan, Status | – |
| JadwalSidang | Id, TugasAkhirId, Jenis, Waktu, Ruang, Penguji1Id, Penguji2Id, Hasil | – |
| Users | Id, Username (unik), PasswordHash, NamaLengkap, Role, DosenId?, IsActive | Opsional terhubung ke Dosen |
| ActivityLog | Id, UserId?, Username, Aksi, Entitas, Deskripsi, CreatedAt | – |

Aturan integritas: foreign key aktif; menghapus TA ikut menghapus bimbingan dan jadwal sidangnya (cascade); menghapus mahasiswa, dosen, atau prodi yang masih dirujuk ditolak (restrict).

---

## 9. Kebutuhan Non-Fungsional

| Kategori | Kebutuhan |
|---|---|
| Keamanan | Seluruh query memakai parameter (tidak ada penggabungan string input). Password di-hash dengan BCrypt. Otorisasi diperiksa di lapisan layanan. |
| Keandalan | Kesalahan database (koneksi, constraint unik, foreign key) diterjemahkan menjadi pesan berbahasa Indonesia yang dapat dipahami. Aplikasi tidak crash karena input tidak valid. |
| Kinerja | Halaman data dan dashboard tampil < 1 detik untuk ±1.000 baris data di perangkat standar. Pencarian memakai jeda (debounce) agar tidak membebani database saat mengetik. |
| Kegunaan | Pengguna baru dapat menyelesaikan alur "tambah mahasiswa → buat TA → catat bimbingan" tanpa pelatihan. |
| Pemeliharaan | Arsitektur MVVM dengan proyek terpisah: `SITA.Core` (model, validasi, layanan, repositori) dan `SITA.App` (UI WPF). Logika bisnis dapat diuji tanpa UI. |
| Portabilitas | Database SQLite tersimpan di `%LOCALAPPDATA%\SITA\sita.db` dan dibuat otomatis saat pertama kali dijalankan. |

---

## 10. Arsitektur dan Teknologi

- **Platform:** .NET 8, WPF (Windows).
- **Pola:** MVVM (CommunityToolkit.Mvvm), dependency injection (Microsoft.Extensions.DependencyInjection).
- **Database:** SQLite (Microsoft.Data.Sqlite) dengan Dapper sebagai micro-ORM; semua query berparameter.
- **Keamanan:** BCrypt.Net-Next untuk hash password.
- **Pengujian:** xUnit untuk validasi, autentikasi, otorisasi, dan perhitungan dashboard menggunakan database SQLite in-memory.

```
SITA.App (WPF)  ──►  SITA.Core.Services  ──►  SITA.Core.Repositories  ──►  SQLite
  Views/ViewModels      validasi, otorisasi,      query SQL berparameter
                        audit log
```

---

## 11. Di Luar Cakupan (Versi Ini)

- Unggah dan penyimpanan berkas dokumen TA (proposal, laporan).
- Portal/login untuk mahasiswa.
- Notifikasi email atau push.
- Sinkronisasi multi-pengguna melalui server pusat.
- Ekspor laporan ke PDF/Excel.

---

## 12. Kriteria Penerimaan dan Skenario Uji

| No | Skenario | Hasil yang diharapkan |
|---|---|---|
| 1 | Login dengan password salah | Muncul pesan "Username atau password salah"; tidak masuk aplikasi. |
| 2 | Login sebagai Admin | Semua menu tampil; dashboard menampilkan data seluruh prodi. |
| 3 | Login sebagai Dosen | Hanya menu Dashboard, Tugas Akhir, Bimbingan, dan Jadwal Sidang yang tampil; data terbatas pada mahasiswa bimbingannya. |
| 4 | Tambah mahasiswa dengan NIM yang sudah ada | Ditolak dengan pesan "NIM sudah terdaftar". |
| 5 | Cari mahasiswa dengan sebagian nama | Tabel hanya menampilkan mahasiswa yang cocok. |
| 6 | Buat TA dengan pembimbing 1 = pembimbing 2 | Ditolak dengan pesan validasi. |
| 7 | Dosen mengubah tahap TA mahasiswa bimbingannya | Tahap berubah, dashboard ikut berubah, dan perubahan tercatat di aktivitas terbaru. |
| 8 | Dosen mencoba mencatat bimbingan untuk TA yang bukan bimbingannya | Ditolak oleh lapisan layanan. |
| 9 | Hapus dosen yang masih menjadi pembimbing | Ditolak dengan penjelasan bahwa data masih dipakai. |
| 10 | Jadwalkan dua sidang di ruang dan waktu yang sama | Sidang kedua ditolak. |
| 11 | Ganti tema lalu tutup dan buka aplikasi | Tema terakhir tetap dipakai. |
| 12 | Tambah bimbingan bulan ini | Kartu "Bimbingan Bulan Ini" dan grafik per bulan bertambah setelah refresh. |

---

## 13. Risiko dan Mitigasi

| Risiko | Mitigasi |
|---|---|
| File database terhapus atau rusak | Database dibuat ulang otomatis dengan skema kosong; disarankan backup berkala file `sita.db`. |
| Akun admin default dipakai di produksi | Password default hanya untuk demo; admin wajib menggantinya lewat menu Pengguna. |
| Data tidak konsisten antar tabel | Foreign key aktif dan validasi di lapisan layanan sebelum penyimpanan. |
