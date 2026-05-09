using EcoMonitor.Application.Common.Models;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(UploadedPhotoDto photo, string subfolder, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}
