using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BarberNetBooking.Infrastructure;

/// <summary>
/// Atributo que protege páginas administrativas verificando o cookie de autenticação
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AdminAuthorizeAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
        // Não faz nada aqui
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var httpContext = context.HttpContext;
        
        // Verifica se o cookie de admin existe e está válido
        var isAdmin = httpContext.Request.Cookies["bn_admin"] == "ok";

        if (!isAdmin)
        {
            // Redireciona para a página de login se não estiver autenticado
            context.Result = new RedirectToPageResult("/Admin/Login", new { returnUrl = httpContext.Request.Path });
        }
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
        // Não faz nada aqui
    }
}