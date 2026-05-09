using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Infrastructure.Persistence;
using EcoMonitor.Web.Models.Dashboard;
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

    public DashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
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

        var model = new AdministratorDashboardViewModel
        {
            FullName = user.FullName,
            TotalUsers = await _userManager.Users.CountAsync(),
            TotalReports = await _dbContext.DumpsiteReports.CountAsync(),
            TotalContainers = await _dbContext.WasteContainers.CountAsync()
        };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Inspector)]
    public async Task<IActionResult> Inspector()
    {
        var user = (await _userManager.GetUserAsync(User))!;

        var model = new InspectorDashboardViewModel
        {
            FullName = user.FullName,
            AssignedReports = await _dbContext.DumpsiteReports
                .CountAsync(r => r.AssignedInspectorId == user.Id),
            NewUnassignedReports = await _dbContext.DumpsiteReports
                .CountAsync(r => r.AssignedInspectorId == null && r.Status == DumpsiteStatus.New)
        };

        return View(model);
    }

    [Authorize(Roles = RoleNames.Citizen)]
    public async Task<IActionResult> Citizen()
    {
        var user = (await _userManager.GetUserAsync(User))!;

        var recent = await _dbContext.DumpsiteReports
            .Where(r => r.ReporterId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        var model = new CitizenDashboardViewModel
        {
            FullName = user.FullName,
            RecentReports = recent
        };

        return View(model);
    }
}
