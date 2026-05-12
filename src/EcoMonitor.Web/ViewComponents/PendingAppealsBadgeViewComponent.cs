using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Web.ViewComponents;

public class PendingAppealsBadgeViewComponent : ViewComponent
{
    private readonly IApplicationDbContext _db;

    public PendingAppealsBadgeViewComponent(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await _db.DumpsiteReports
            .AsNoTracking()
            .CountAsync(r => r.Status == DumpsiteStatus.Appealed);
        return View(count);
    }
}
