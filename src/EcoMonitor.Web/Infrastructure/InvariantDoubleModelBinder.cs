using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EcoMonitor.Web.Infrastructure;

public sealed class InvariantDoubleModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var raw = valueProviderResult.FirstValue;
        var modelType = bindingContext.ModelType;
        var underlying = Nullable.GetUnderlyingType(modelType) ?? modelType;
        var isNullable = Nullable.GetUnderlyingType(modelType) is not null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            if (isNullable)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            return Task.CompletedTask;
        }

        var normalized = raw.Replace(',', '.');

        if (underlying == typeof(double))
        {
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                bindingContext.Result = ModelBindingResult.Success(d);
                return Task.CompletedTask;
            }
        }
        else if (underlying == typeof(float))
        {
            if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                bindingContext.Result = ModelBindingResult.Success(f);
                return Task.CompletedTask;
            }
        }
        else if (underlying == typeof(decimal))
        {
            if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var dec))
            {
                bindingContext.Result = ModelBindingResult.Success(dec);
                return Task.CompletedTask;
            }
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"The value '{raw}' is not a valid number.");
        return Task.CompletedTask;
    }
}
