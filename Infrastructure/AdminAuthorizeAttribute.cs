using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BarberNetBooking.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminAuthorizeAttribute : Attribute, IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.Request.Cookies.TryGetValue("bn_admin", out var val) && val == "ok")
        {
            return next();
        }

        var returnUrl = http.Request.Path.Value ?? "/Admin";
        context.Result = new RedirectToPageResult("/Admin/Login", new { returnUrl });
        return Task.CompletedTask;
    }
}