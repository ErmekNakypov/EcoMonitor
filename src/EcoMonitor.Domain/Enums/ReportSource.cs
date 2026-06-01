namespace EcoMonitor.Domain.Enums;

// Numeric values are frozen — they live in dumpsite_reports.source. Append
// new sources at the end; never reorder.
public enum ReportSource
{
    Web = 0,
    Telegram = 1,
    // System-generated cleanup task created by IngestContainerFillReadingHandler
    // when a sensor reports a fill level >= FillThresholds.FullPercent.
    Iot = 2
}
