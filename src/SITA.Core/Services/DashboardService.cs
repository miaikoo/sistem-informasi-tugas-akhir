using SITA.Core.Models;
using SITA.Core.Repositories;

namespace SITA.Core.Services;

public class DashboardData
{
    public required DashboardSummary Summary { get; init; }
    public required IReadOnlyList<ChartPoint> TahapDistribution { get; init; }
    public required IReadOnlyList<ChartPoint> BimbinganPerBulan { get; init; }
    /// <summary>Empty for lecturers; workload comparison is an admin view.</summary>
    public required IReadOnlyList<ChartPoint> BebanDosen { get; init; }
    public required IReadOnlyList<JadwalSidang> UpcomingSidang { get; init; }
    public required IReadOnlyList<ActivityLog> RecentActivity { get; init; }
}

public class DashboardService : ServiceBase
{
    private readonly IDashboardRepository _repo;
    private readonly IActivityLogRepository _activity;

    public DashboardService(IDashboardRepository repo, IActivityLogRepository activity, UserSession session, ActivityLogger logger)
        : base(session, logger)
    {
        _repo = repo;
        _activity = activity;
    }

    public DashboardData Load()
    {
        RequireLogin();
        var now = DateTime.Now;
        int? scope = Session.DosenScope;
        return new DashboardData
        {
            Summary = _repo.GetSummary(now.Date, scope),
            TahapDistribution = _repo.GetTahapDistribution(scope),
            BimbinganPerBulan = _repo.GetBimbinganPerBulan(now.Date, 6, scope),
            BebanDosen = Session.IsAdmin ? _repo.GetBebanDosen(8) : [],
            UpcomingSidang = _repo.GetUpcomingSidang(now, 5, scope),
            RecentActivity = _activity.GetRecent(8, Session.IsAdmin ? null : Session.CurrentUser!.Id)
        };
    }
}
