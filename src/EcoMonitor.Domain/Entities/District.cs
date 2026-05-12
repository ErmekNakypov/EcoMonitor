using EcoMonitor.Domain.Common;

namespace EcoMonitor.Domain.Entities;

// One of the four administrative districts of Bishkek. Boundaries are stored
// as an ordered list of (lat, lng) points forming a polygon; the runtime uses
// ray-casting (PointInPolygonChecker) to decide which district a report falls
// in. AssignedInspectorId is the inspector currently responsible for the
// district — used to auto-assign incoming InReview reports.
public class District : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // "SVERDLOV", "PERVOMAY", "LENIN", "OKTYABR"
    public string NameRu { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameKy { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public Guid? AssignedInspectorId { get; set; }

    public List<DistrictBoundaryPoint> Boundary { get; set; } = new();
}

public class DistrictBoundaryPoint : BaseEntity
{
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;

    public int SequenceNumber { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
