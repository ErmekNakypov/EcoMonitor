namespace EcoMonitor.Application.Common.Services.Sensors;

// Single source of truth for SERVER-SIDE fill-band cutoffs. Mirrors the
// FILL_FULL = 90 constant in Views/Live/Index.cshtml (THRESHOLDS block) so
// the client gauge tinting and the server-side "create cleanup task" edge
// trigger stay in lockstep. If you tune one, tune the other.
public static class FillThresholds
{
    // A reading at or above this percentage is treated as "full — needs
    // pickup" by the auto-task pipeline in IngestContainerFillReadingHandler.
    // Edge-triggered: a task is created only when the PREVIOUS reading was
    // below this value and the new one crosses up.
    public const double FullPercent = 90.0;
}
