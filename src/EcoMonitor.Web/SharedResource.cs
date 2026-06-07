namespace EcoMonitor.Web;

// Empty marker type used as the generic parameter for
// IStringLocalizer<SharedResource> / IHtmlLocalizer<SharedResource>.
// The shared .resx files live in Resources/SharedResource.{culture}.resx
// (the path matches this class's full name minus the assembly root,
// resolved against RequestLocalizationOptions.ResourcesPath = "Resources").
//
// Use this resource for strings that appear across many views (nav links,
// common labels, page chrome). Per-view strings should live in their own
// per-view resource files under Resources/Views/... once we start
// extracting them in step 2.
public class SharedResource
{
}
