namespace EcoMonitor.Application.Common.Models;

public sealed record UserSummaryDto(Guid Id, string Email, string FullName, DateTime RegisteredAt);
