using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace BarberNetBooking.Infrastructure;

public class DateOnlyModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
            return Task.CompletedTask;

        // Tenta parsear em vários formatos
        DateOnly result;
        
        // Formato ISO (yyyy-MM-dd) - padrão HTML5
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        // Formato brasileiro (dd/MM/yyyy)
        if (DateOnly.TryParseExact(value, "dd/MM/yyyy", new CultureInfo("pt-BR"), DateTimeStyles.None, out result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        // Formato brasileiro alternativo (d/M/yyyy)
        if (DateOnly.TryParseExact(value, "d/M/yyyy", new CultureInfo("pt-BR"), DateTimeStyles.None, out result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        // Tenta parsing padrão da cultura atual
        if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        // Se nenhum formato funcionou, adiciona erro
        bindingContext.ModelState.TryAddModelError(
            modelName,
            $"A data '{value}' não está em um formato válido. Use dd/MM/yyyy.");

        return Task.CompletedTask;
    }
}

public class DateOnlyModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Metadata.ModelType == typeof(DateOnly) || 
            context.Metadata.ModelType == typeof(DateOnly?))
        {
            return new DateOnlyModelBinder();
        }

        return null;
    }
}