using System.Security.Claims;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Infrastructure.Identity;
using EcoMonitor.Web.Models.Admin.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
public class UsersController : Controller
{
    private const int PageSize = 20;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    private async Task<IReadOnlyList<string>> GetAvailableRolesAsync()
    {
        var names = await _roleManager.Roles
            .Select(r => r.Name)
            .Where(n => n != null)
            .OrderBy(n => n)
            .ToListAsync();
        return names!.Cast<string>().ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role, bool? active, int page = 1)
    {
        if (page < 1) page = 1;

        IEnumerable<ApplicationUser> users;
        if (!string.IsNullOrWhiteSpace(role))
        {
            users = await _userManager.GetUsersInRoleAsync(role);
        }
        else
        {
            users = await _userManager.Users.ToListAsync();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            users = users.Where(u =>
                (u.Email is not null && u.Email.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                u.FullName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (active.HasValue)
        {
            users = users.Where(u => u.IsActive == active.Value);
        }

        var ordered = users.OrderByDescending(u => u.CreatedAt).ToList();
        var totalCount = ordered.Count;
        var totalPages = (int)Math.Max(1, Math.Ceiling(totalCount / (double)PageSize));
        if (page > totalPages) page = totalPages;

        var pageUsers = ordered
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var items = new List<UserListItemViewModel>(pageUsers.Count);
        foreach (var user in pageUsers)
        {
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = await _userManager.GetRolesAsync(user),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        var model = new UserListViewModel
        {
            SearchQuery = search,
            RoleFilter = role,
            ActiveFilter = active,
            Users = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = PageSize,
            TotalPages = totalPages
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new CreateUserViewModel
        {
            AvailableRoles = await GetAvailableRolesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Role) || !await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            IsActive = true,
            PreferredLanguage = "ru"
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        _logger.LogInformation(
            "Admin {AdminEmail} created user {UserEmail} with role {Role}",
            User.Identity?.Name, user.Email, model.Role);

        TempData["SuccessMessage"] = $"User {user.Email} created with role {model.Role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? string.Empty;

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = currentRole,
            CurrentRole = currentRole,
            IsActive = user.IsActive,
            AvailableRoles = await GetAvailableRolesAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(model.Role) || !await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();
        var roleChanged = !string.Equals(currentRole, model.Role, StringComparison.Ordinal);

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = string.Equals(currentUserId, user.Id.ToString(), StringComparison.Ordinal);

        if (roleChanged
            && currentRole == RoleNames.Administrator
            && model.Role != RoleNames.Administrator)
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync(RoleNames.Administrator)).Count;
            if (adminCount <= 1)
            {
                ModelState.AddModelError(nameof(model.Role), "Cannot remove the last administrator.");
                model.CurrentRole = currentRole ?? string.Empty;
                model.AvailableRoles = await GetAvailableRolesAsync();
                return View(model);
            }
        }

        if (!model.IsActive && isSelf)
        {
            ModelState.AddModelError(nameof(model.IsActive), "You cannot deactivate your own account.");
            model.CurrentRole = currentRole ?? string.Empty;
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        user.FullName = model.FullName;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.CurrentRole = currentRole ?? string.Empty;
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        if (roleChanged)
        {
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        _logger.LogInformation(
            "Admin {AdminEmail} updated user {UserEmail}; role={Role}, active={IsActive}",
            User.Identity?.Name, user.Email, model.Role, user.IsActive);

        TempData["SuccessMessage"] = $"User {user.Email} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var model = new ResetPasswordViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.UserEmail = user.Email ?? string.Empty;
            return View(model);
        }

        _logger.LogInformation(
            "Admin {AdminEmail} reset password for user {UserEmail}",
            User.Identity?.Name, user.Email);

        TempData["SuccessMessage"] = $"Password reset for {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = string.Equals(currentUserId, user.Id.ToString(), StringComparison.Ordinal);

        if (user.IsActive && isSelf)
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(' ', result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation(
            "Admin {AdminEmail} toggled active for user {UserEmail} to {IsActive}",
            User.Identity?.Name, user.Email, user.IsActive);

        TempData["SuccessMessage"] = user.IsActive
            ? $"User {user.Email} activated."
            : $"User {user.Email} deactivated.";

        return RedirectToAction(nameof(Index));
    }
}
