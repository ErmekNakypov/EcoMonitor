using EcoMonitor.Application.Common.Models;
using EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Tests;

public class SubmitDumpsiteReportCommandValidatorTests
{
    private static readonly SubmitDumpsiteReportCommandValidator Validator = new();

    private static UploadedPhotoDto Jpeg(int sizeBytes = 100)
        => new UploadedPhotoDto("p.jpg", "image/jpeg", new byte[sizeBytes]);

    private static SubmitDumpsiteReportCommand BaseCommand(
        string? description = null,
        double lat = 42.87,
        double lng = 74.60,
        IReadOnlyList<UploadedPhotoDto>? photos = null,
        ReportSource source = ReportSource.Web,
        IReadOnlyList<string>? preSavedPhotoPaths = null)
    {
        return new SubmitDumpsiteReportCommand(
            ReporterId: Guid.NewGuid(),
            Description: description ?? "A garbage pile near the school entrance.",
            Latitude: lat,
            Longitude: lng,
            Photos: photos ?? new[] { Jpeg() },
            Source: source,
            PreSavedPhotoPaths: preSavedPhotoPaths);
    }

    [Fact]
    public void HappyPath_WebReportWithOnePhoto_IsValid()
    {
        var result = Validator.Validate(BaseCommand());
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void DescriptionTooShort_IsInvalid()
    {
        var result = Validator.Validate(BaseCommand(description: "short"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SubmitDumpsiteReportCommand.Description));
    }

    [Theory]
    [InlineData(91.0)]
    [InlineData(-91.0)]
    public void LatitudeOutOfRange_IsInvalid(double lat)
    {
        var result = Validator.Validate(BaseCommand(lat: lat));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SubmitDumpsiteReportCommand.Latitude));
    }

    [Theory]
    [InlineData(180.5)]
    [InlineData(-181.0)]
    public void LongitudeOutOfRange_IsInvalid(double lng)
    {
        var result = Validator.Validate(BaseCommand(lng: lng));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SubmitDumpsiteReportCommand.Longitude));
    }

    [Fact]
    public void WebReportWithNoPhoto_IsInvalid()
    {
        var result = Validator.Validate(BaseCommand(
            photos: Array.Empty<UploadedPhotoDto>(),
            preSavedPhotoPaths: null,
            source: ReportSource.Web));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("photo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IotReportWithNoPhoto_IsValid()
    {
        // The IoT-source exemption is the central behaviour: container-fill
        // sensor tasks legitimately have no photo and must pass validation.
        var result = Validator.Validate(BaseCommand(
            photos: Array.Empty<UploadedPhotoDto>(),
            preSavedPhotoPaths: null,
            source: ReportSource.Iot));

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void MoreThanFivePhotos_IsInvalid()
    {
        var sixPhotos = Enumerable.Range(0, 6).Select(_ => Jpeg()).ToArray();
        var result = Validator.Validate(BaseCommand(photos: sixPhotos));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("5"));
    }

    [Fact]
    public void NonImageContentType_IsInvalid()
    {
        var bogus = new[] { new UploadedPhotoDto("doc.pdf", "application/pdf", new byte[10]) };
        var result = Validator.Validate(BaseCommand(photos: bogus));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("JPEG", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OversizedPhoto_IsInvalid()
    {
        // 5 MB cap; supply 5 MB + 1 byte.
        var huge = new[] { new UploadedPhotoDto("big.jpg", "image/jpeg", new byte[5 * 1024 * 1024 + 1]) };
        var result = Validator.Validate(BaseCommand(photos: huge));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("5 MB", StringComparison.OrdinalIgnoreCase));
    }
}
