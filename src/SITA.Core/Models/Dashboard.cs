namespace SITA.Core.Models;

public class DashboardSummary
{
    public int MahasiswaTaAktif { get; set; }
    public int TotalMahasiswa { get; set; }
    public int BimbinganBulanIni { get; set; }
    public int BimbinganBulanLalu { get; set; }
    public int SidangMendatang { get; set; }
    public int Lulus { get; set; }
    public int TotalDosen { get; set; }
}

public record ChartPoint(string Label, int Value);
