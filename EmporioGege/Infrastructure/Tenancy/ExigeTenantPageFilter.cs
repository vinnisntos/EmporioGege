using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Infrastructure.Tenancy
{
    // Aplicado nas pastas Admin/ e Caixa/ (ver Program.cs): superadmin pode estar autenticado
    // sem nenhum tenant no contexto (perfil sem tenant_id e sem ter "entrado" numa loja via
    // SuperAdmin/Adegas). Toda página dessas pastas depende de ITenantProvider.RequireTenantId()
    // em algum ponto, que lança exceção nesse caso - sem este filtro isso vira erro 500 genérico
    // em vez de mandar o superadmin de volta pra escolher uma loja.
    public class ExigeTenantPageFilter(ITenantProvider tenantProvider, ITempDataDictionaryFactory tempDataFactory) : IAsyncPageFilter
    {
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (tenantProvider.TenantId is null)
            {
                var tempData = tempDataFactory.GetTempData(context.HttpContext);
                tempData["MensagemErro"] = "Selecione ou entre numa loja antes de continuar.";
                context.Result = new RedirectToPageResult("/SuperAdmin/Adegas/Index");
                return Task.CompletedTask;
            }

            return next();
        }
    }
}
