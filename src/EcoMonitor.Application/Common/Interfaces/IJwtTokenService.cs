namespace EcoMonitor.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string IssueDeviceToken(string deviceId, Guid deviceGuid);
    string ComputeTokenHash(string token);
}
