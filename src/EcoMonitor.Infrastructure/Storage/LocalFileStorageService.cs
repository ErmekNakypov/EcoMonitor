using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using Microsoft.AspNetCore.Hosting;

namespace EcoMonitor.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveAsync(UploadedPhotoDto photo, string subfolder, CancellationToken ct = default)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(photo.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            ext = ContentTypeToExtension.TryGetValue(photo.ContentType, out var fromContentType)
                ? fromContentType
                : ".jpg";
        }

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(fullPath, photo.Content, ct);

        return $"/uploads/{subfolder}/{fileName}";
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var trimmed = relativePath.TrimStart('/');
        var fullPath = Path.Combine(_env.WebRootPath, trimmed);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
