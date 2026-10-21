using System.Globalization;
using Dapper;
using SITA.Core.Data;
using SITA.Core.Models;

namespace SITA.Core.Repositories;

public interface IDashboardRepository
{
    /// <param name="dosenId">When set, every figure is limited to TA supervised by this lecturer.</param>
    DashboardSummary GetSummary(DateTime today, int? dosenId = null);
    IReadOnlyList<ChartPoint> GetTahapDistribution(int? dosenId = null);
    IReadOnlyList<ChartPoint> GetBimbinganPerBulan(DateTime today, int months, int? dosenId = null);
    IReadOnlyList<ChartPoint> GetBebanDosen(int top);
    IReadOnlyList<JadwalSidang> GetUpcomingSidang(DateTime now, int limit, int? dosenId = null);
}

public class DashboardRepository : IDashboardRepository
{
    private static readonly CultureInfo Indonesian = new("id-ID");
    private const string TaScope = "(@DosenId IS NULL OR t.Pembimbing1Id = @DosenId OR t.Pembimbing2Id = @DosenId)";

    private readonly IDbConnectionFactory _factory;

    public DashboardRepository(IDbConnectionFactory factory) => _factory = factory;

    public DashboardSummary GetSummary(DateTime today, int? dosenId = null)
    {
        var monthStart = new DateTime(today.Year, today.Month, 1);
        using var conn = _factory.Open();
        return conn.QuerySingle<DashboardSummary>(
            $"""
            SELECT
              (SELECT COUNT(*) FROM TugasAkhir t WHERE t.Tahap < 5 AND {TaScope}) AS MahasiswaTaAktif,
              CASE WHEN @DosenId IS NULL THEN (SELECT COUNT(*) FROM Mahasiswa)
                   ELSE (SELECT COUNT(*) FROM TugasAkhir t WHERE {TaScope}) END AS TotalMahasiswa,
              (SELECT COUNT(*) FROM Bimbingan b JOIN TugasAkhir t ON t.Id = b.TugasAkhirId
                 WHERE b.Tanggal >= @MonthStart AND b.Tanggal < @NextMonth AND {TaScope}) AS BimbinganBulanIni,
              (SELECT COUNT(*) FROM Bimbingan b JOIN TugasAkhir t ON t.Id = b.TugasAkhirId
                 WHERE b.Tanggal >= @PrevMonth AND b.Tanggal < @MonthStart AND {TaScope}) AS BimbinganBulanLalu,
              (SELECT COUNT(*) FROM JadwalSidang s JOIN TugasAkhir t ON t.Id = s.TugasAkhirId
                 WHERE s.Waktu >= @Today AND s.Waktu < @In30Days AND s.Hasil = 0
                   AND (@DosenId IS NULL OR t.Pembimbing1Id = @DosenId OR t.Pembimbing2Id = @DosenId
                        OR s.Penguji1Id = @DosenId OR s.Penguji2Id = @DosenId)) AS SidangMendatang,
              (SELECT COUNT(*) FROM TugasAkhir t WHERE t.Tahap = 5 AND {TaScope}) AS Lulus,
              (SELECT COUNT(*) FROM Dosen) AS TotalDosen
            """,
            new
            {
                DosenId = dosenId, Today = today.Date, In30Days = today.Date.AddDays(30),
                MonthStart = monthStart, NextMonth = monthStart.AddMonths(1), PrevMonth = monthStart.AddMonths(-1)
            });
    }

    public IReadOnlyList<ChartPoint> GetTahapDistribution(int? dosenId = null)
    {
        using var conn = _factory.Open();
        var counts = conn.Query<(long Tahap, long Jumlah)>(
            $"SELECT t.Tahap, COUNT(*) FROM TugasAkhir t WHERE {TaScope} GROUP BY t.Tahap",
            new { DosenId = dosenId }).ToDictionary(r => (TahapTA)r.Tahap, r => (int)r.Jumlah);

        return Enum.GetValues<TahapTA>()
            .Select(t => new ChartPoint(EnumLabels.Of(t), counts.GetValueOrDefault(t)))
            .ToList();
    }

    public IReadOnlyList<ChartPoint> GetBimbinganPerBulan(DateTime today, int months, int? dosenId = null)
    {
        var start = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));
        using var conn = _factory.Open();
        var counts = conn.Query<(string Bulan, long Jumlah)>(
            $"""
            SELECT strftime('%Y-%m', b.Tanggal) AS Bulan, COUNT(*)
            FROM Bimbingan b JOIN TugasAkhir t ON t.Id = b.TugasAkhirId
            WHERE b.Tanggal >= @Start AND {TaScope}
            GROUP BY Bulan
            """,
            new { Start = start, DosenId = dosenId }).ToDictionary(r => r.Bulan, r => (int)r.Jumlah);

        return Enumerable.Range(0, months)
            .Select(i => start.AddMonths(i))
            .Select(m => new ChartPoint(
                Indonesian.DateTimeFormat.GetAbbreviatedMonthName(m.Month),
                counts.GetValueOrDefault(m.ToString("yyyy-MM", CultureInfo.InvariantCulture))))
            .ToList();
    }

    public IReadOnlyList<ChartPoint> GetBebanDosen(int top)
    {
        using var conn = _factory.Open();
        return conn.Query<(string Nama, long Jumlah)>(
            """
            SELECT d.Nama, COUNT(t.Id) AS Jumlah
            FROM Dosen d
            LEFT JOIN TugasAkhir t ON (t.Pembimbing1Id = d.Id OR t.Pembimbing2Id = d.Id) AND t.Tahap < 5
            GROUP BY d.Id
            ORDER BY Jumlah DESC, d.Nama
            LIMIT @Top
            """,
            new { Top = top }).Select(r => new ChartPoint(r.Nama, (int)r.Jumlah)).ToList();
    }

    public IReadOnlyList<JadwalSidang> GetUpcomingSidang(DateTime now, int limit, int? dosenId = null)
    {
        using var conn = _factory.Open();
        return conn.Query<JadwalSidang>(
            """
            SELECT s.*, m.Nim, m.Nama AS NamaMahasiswa, t.Judul AS JudulTA,
                   d1.Nama AS Penguji1Nama, d2.Nama AS Penguji2Nama
            FROM JadwalSidang s
            JOIN TugasAkhir t ON t.Id = s.TugasAkhirId
            JOIN Mahasiswa m ON m.Id = t.MahasiswaId
            JOIN Dosen d1 ON d1.Id = s.Penguji1Id
            JOIN Dosen d2 ON d2.Id = s.Penguji2Id
            WHERE s.Waktu >= @Today AND s.Hasil = 0
              AND (@DosenId IS NULL OR t.Pembimbing1Id = @DosenId OR t.Pembimbing2Id = @DosenId
                   OR s.Penguji1Id = @DosenId OR s.Penguji2Id = @DosenId)
            ORDER BY s.Waktu
            LIMIT @Limit
            """,
            new { Today = now.Date, Limit = limit, DosenId = dosenId }).AsList();
    }
}
