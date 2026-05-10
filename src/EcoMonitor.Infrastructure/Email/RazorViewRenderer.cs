using EcoMonitor.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace EcoMonitor.Infrastructure.Email;

public sealed class RazorViewRenderer : IRazorViewRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public RazorViewRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderAsync<TModel>(string viewName, TModel model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        // Absolute paths (starting with "~/" or "/") need GetView; logical names use FindView.
        var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
        if (!viewResult.Success)
        {
            viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
        }
        if (!viewResult.Success)
        {
            var locations = viewResult.SearchedLocations is null
                ? string.Empty
                : string.Join(", ", viewResult.SearchedLocations);
            throw new InvalidOperationException(
                $"Razor view '{viewName}' not found. Searched: {locations}");
        }

        await using var sw = new StringWriter();
        var viewData = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model
        };
        var tempData = new TempDataDictionary(httpContext, _tempDataProvider);
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            tempData,
            sw,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
}
