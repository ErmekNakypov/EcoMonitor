namespace EcoMonitor.Application.Common.Interfaces;

public interface IRazorViewRenderer
{
    Task<string> RenderAsync<TModel>(string viewName, TModel model);
}
