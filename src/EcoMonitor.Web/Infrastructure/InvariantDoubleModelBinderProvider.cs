using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EcoMonitor.Web.Infrastructure;

public sealed class InvariantDoubleModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        var underlying = Nullable.GetUnderlyingType(modelType) ?? modelType;

        if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
        {
            return new InvariantDoubleModelBinder();
        }

        return null;
    }
}
