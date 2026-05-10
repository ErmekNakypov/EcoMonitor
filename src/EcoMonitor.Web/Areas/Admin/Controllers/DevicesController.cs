using System.Security.Cryptography;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Admin.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Devices")]
public class DevicesController : Controller
{
    private const string TokenSessionKey = "LastIssuedDeviceToken";

    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        IApplicationDbContext db,
        IJwtTokenService jwt,
        ILogger<DevicesController> logger)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await _db.IotDevices
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeviceRowViewModel
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                Name = d.Name,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                Status = d.Status,
                LastSeenAt = d.LastSeenAt,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(ct);

        return View(new DeviceListViewModel { Items = rows, TotalCount = rows.Count });
    }

    [HttpGet("Create")]
    public IActionResult Create() => View(new CreateDeviceViewModel());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDeviceViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var deviceId = await GenerateUniqueDeviceIdAsync(ct);
        var token = "";
        var device = new IotDevice
        {
            DeviceId = deviceId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Status = IotDeviceStatus.Active,
            TokenIssuedAt = DateTime.UtcNow
        };

        // Issue the JWT against the device's allocated Guid before saving so we can
        // store the hash atomically with the row.
        token = _jwt.IssueDeviceToken(device.DeviceId, device.Id);
        device.TokenHash = _jwt.ComputeTokenHash(token);

        _db.IotDevices.Add(device);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Admin {User} created IoT device {DeviceId} ({Guid})",
            User.Identity?.Name, device.DeviceId, device.Id);

        TempData[TokenSessionKey] = token;
        return RedirectToAction(nameof(TokenIssued), new { id = device.Id });
    }

    [HttpGet("TokenIssued/{id:guid}")]
    public async Task<IActionResult> TokenIssued(Guid id, CancellationToken ct)
    {
        var device = await _db.IotDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device is null) return NotFound();

        var token = TempData[TokenSessionKey] as string;
        if (string.IsNullOrEmpty(token))
        {
            // Token was already shown once; do not re-display it.
            TempData["ErrorMessage"] = "The token has already been displayed and is no longer available. Use \"Regenerate token\" if you need a new one.";
            return RedirectToAction(nameof(Index));
        }

        return View(new TokenIssuedViewModel
        {
            DeviceId = device.Id,
            DeviceCode = device.DeviceId,
            DeviceName = device.Name,
            Token = token
        });
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var device = await _db.IotDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device is null) return NotFound();

        return View(new EditDeviceViewModel
        {
            Id = device.Id,
            DeviceId = device.DeviceId,
            Name = device.Name,
            Description = device.Description,
            Latitude = device.Latitude,
            Longitude = device.Longitude,
            Status = device.Status
        });
    }

    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditDeviceViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var device = await _db.IotDevices.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device is null) return NotFound();

        device.Name = model.Name.Trim();
        device.Description = model.Description?.Trim();
        device.Latitude = model.Latitude;
        device.Longitude = model.Longitude;
        device.Status = model.Status;

        await _db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = $"Device {device.DeviceId} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("RegenerateToken/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateToken(Guid id, CancellationToken ct)
    {
        var device = await _db.IotDevices.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device is null) return NotFound();

        var token = _jwt.IssueDeviceToken(device.DeviceId, device.Id);
        device.TokenHash = _jwt.ComputeTokenHash(token);
        device.TokenIssuedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Admin {User} regenerated token for IoT device {DeviceId}",
            User.Identity?.Name, device.DeviceId);

        TempData[TokenSessionKey] = token;
        return RedirectToAction(nameof(TokenIssued), new { id = device.Id });
    }

    [HttpPost("Delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var device = await _db.IotDevices.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (device is null) return NotFound();

        // Soft delete: keep history. The device can no longer authenticate because
        // the ingestion handler checks Status == Active.
        device.Status = IotDeviceStatus.Decommissioned;
        await _db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = $"Device {device.DeviceId} decommissioned.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateUniqueDeviceIdAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var hex = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
            var candidate = $"ESP32-{hex}";
            var taken = await _db.IotDevices.AnyAsync(d => d.DeviceId == candidate, ct);
            if (!taken) return candidate;
        }
        // Astronomically unlikely; fall back to a longer suffix.
        return $"ESP32-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
    }
}
