namespace EcoMonitor.Application.Common.Models;

public sealed record UploadedPhotoDto(string FileName, string ContentType, byte[] Content);
