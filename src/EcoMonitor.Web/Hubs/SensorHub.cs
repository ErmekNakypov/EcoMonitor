using Microsoft.AspNetCore.SignalR;

namespace EcoMonitor.Web.Hubs;

// Server-to-client only. Clients subscribe and receive "airReading" / "fillReading"
// events. No client-callable methods.
public class SensorHub : Hub
{
}
