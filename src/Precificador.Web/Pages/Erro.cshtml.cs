using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Web.Erros;

namespace Precificador.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public sealed class ErroModel : PageModel
{
    public string Titulo { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public string? Referencia { get; private set; }

    public IActionResult OnGet(int codigo) => Inicializar(codigo);
    public IActionResult OnPost(int codigo) => Inicializar(codigo);

    private IActionResult Inicializar(int codigo)
    {
        codigo = codigo is >= 400 and <= 599 ? codigo : 500;
        Response.StatusCode = codigo;
        if (!NavegacaoHtml.AceitaHtml(HttpContext))
            return StatusCode(codigo);

        (Titulo, Mensagem) = codigo switch
        {
            400 => ("Solicitação inválida", "Não foi possível processar a solicitação. Verifique os dados e tente novamente."),
            403 => ("Acesso não permitido", "Você não tem permissão para acessar esta página ou recurso."),
            404 => ("Página não encontrada", "A página ou o recurso solicitado não existe ou não está disponível."),
            405 => ("Operação não permitida", "Esta operação não está disponível para a página solicitada."),
            500 => ("Não foi possível concluir sua solicitação", "Ocorreu um erro inesperado. Tente novamente. Se o problema persistir, informe a referência exibida nesta página."),
            503 => ("Serviço temporariamente indisponível", "Tente novamente em alguns instantes. Se o problema persistir, informe a referência exibida nesta página."),
            _ => ("Não foi possível concluir a solicitação", "Tente novamente. Se o problema persistir, procure suporte.")
        };
        if (codigo >= 500)
            Referencia = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return Page();
    }
}
