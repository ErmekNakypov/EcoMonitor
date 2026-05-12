using EcoMonitor.Application.Features.Analytics.GetAppealStats;
using EcoMonitor.Application.Features.Analytics.GetAutoTriageStats;
using EcoMonitor.Application.Features.DumpsiteReports.Queries.GetMyReports;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Infrastructure.Persistence;
using EcoMonitor.Web.Models.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMediator _mediator;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IMediator mediator)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(RoleNames.Administrator))
        {
            return RedirectToAction(nameof(Administrator));
        }

        if (roles.Contains(RoleNames.Inspector))
        {
            return RedirectToAction(nameof(Inspector));
        }

        if (roles.Contains(RoleNames.Citizen))
        {
            return RedirectToAction(nameof(Citizen));
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Administrator()
    {
        var user = (await _userManager.GetUserAsync(User))!;

        var totalUsers = await _userManager.Users.CountAsync();
        var totalReports = await _dbContext.DumpsiteReports.CountAsync();
        var totalContainers = await _dbContext.WasteContainers.CountAsync();
        var triage = await _mediator.Send(new GetAutoTriageStatsQuery());
        var appeals = await _mediator.Send(new GetAppealStatsQuery());

        var model = new AdministratorDashboardViewModel
        {
            FullName = user.FullName,
            TotalUsers = totalUsers,
            TotalReports = totalReports,
            TotalContainers = totalContainers,
            AutoTriageTotal30d = triage.Total,
            AutoTriageAutoConfirmed30d = triage.AutoConfirmed,
            AutoTriageReviewed30d = triage.Reviewed,
            AutoTriagePercentage30d = triage.Percentage,
            AppealsTotalEligible30d = appeals.TotalEligible,
            AppealsAppealed30d = appeals.Appealed,
            AppealsPercentage30d = appeals.Percentage
        };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Inspector)]
    public async Task<IActionResult> Inspector()
    {
        var user = (await _userManager.GetUserAsync(User))!;

        var queueSize = await _dbContext.DumpsiteReports
            .CountAsync(r => r.Status == DumpsiteStatus.New && r.AssignedInspectorId == null);

        var myActive = await _dbContext.DumpsiteReports
            .CountAsync(r => r.AssignedInspectorId == user.Id
                && (r.Status == DumpsiteStatus.InReview || r.Status == DumpsiteStatus.Confirmed));

        var myResolved = await _dbContext.DumpsiteReports
            .CountAsync(r => r.AssignedInspectorId == user.Id && r.Status == DumpsiteStatus.Resolved);

        var model = new InspectorDashboardViewModel
        {
            FullName = user.FullName,
            QueueSize = queueSize,
            MyActiveReports = myActive,
            MyResolvedReports = myResolved
        };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Citizen)]
    public async Task<IActionResult> Citizen()
    {
        var user = (await _userManager.GetUserAsync(User))!;

        var recent = await _mediator.Send(new GetMyReportsQuery(user.Id, Tab: "all", Page: 1, PageSize: 5));

        var model = new CitizenDashboardViewModel
        {
            FullName = user.FullName,
            RecentReports = recent.Items,
            TotalReports = recent.TotalCount
        };

        return View(model);
    }
}
