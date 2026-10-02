using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Core.Empresas;
using Precificador.Web.Autorizacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Configuracoes;

[Authorize(Policy = NomesAutorizacao.AdministradorEmpresa)]
[RequestSizeLimit(1048576)]
[RequestFormLimits(MultipartBodyLengthLimit = 1048576, MemoryBufferThreshold = 1048576)]
public sealed class IdentidadeVisualModel(IdentidadeVisualEmpresaAtual atual, ServicoIdentidadeVisualEmpresa servico, EmpresaContext empresa) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [TempData] public string? Mensagem { get; set; }
    public IdentidadeVisualEfetiva Identidade { get; private set; } = new(IdentidadeVisualEmpresa.CorPadrao, false, true);
    public string NomeEmpresa => empresa.Nome!;
    public async Task OnGetAsync() { await CarregarAsync(); Input.CorPrimaria = Identidade.CorPrimaria; }
    private async Task CarregarAsync() => Identidade = (await atual.ObterAsync())!;
    public async Task<IActionResult> OnPostAsync()
    {
        byte[]? bytes = null;
        try
        {
            IdentidadeVisualEmpresa.NormalizarCor(Input.CorPrimaria);
            if (Input.Logo is not null)
            {
                if (Input.RemoverLogo) throw new ArgumentException("Envie um novo logo ou escolha remover o logo atual, não as duas opções.");
                if (Input.Logo.Length > IdentidadeVisualEmpresa.TamanhoMaximoLogo) throw new ArgumentException("O logo deve possuir no máximo 512 KiB.");
                // Limita também a leitura real, independentemente do Length informado.
                await using var origem = Input.Logo.OpenReadStream();
                using var destino = new MemoryStream();
                var buffer = new byte[8192];
                int lidos;
                while ((lidos = await origem.ReadAsync(buffer)) > 0)
                {
                    if (destino.Length + lidos > IdentidadeVisualEmpresa.TamanhoMaximoLogo) throw new ArgumentException("O logo deve possuir no máximo 512 KiB.");
                    destino.Write(buffer, 0, lidos);
                }
                bytes = destino.ToArray();
                IdentidadeVisualEmpresa.DetectarContentType(bytes);
            }
        }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        if (!ModelState.IsValid) { await CarregarAsync(); return Page(); }
        try
        {
            if (!await servico.SalvarAsync(Input.CorPrimaria, bytes, Input.RemoverLogo)) return Forbid();
        }
        catch (InvalidOperationException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException)
        { ModelState.AddModelError(string.Empty, ex.Message); await CarregarAsync(); return Page(); }
        Mensagem = "Identidade visual atualizada com sucesso.";
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostRestaurarPadraoAsync()
    {
        try { if (!await servico.RestaurarAsync()) return Forbid(); }
        catch (InvalidOperationException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException)
        { ModelState.AddModelError(string.Empty, ex.Message); await CarregarAsync(); return Page(); }
        Mensagem = "Identidade visual restaurada para o padrão.";
        return RedirectToPage();
    }
    public sealed class InputModel
    {
        public string CorPrimaria { get; set; } = string.Empty;
        public IFormFile? Logo { get; set; }
        public bool RemoverLogo { get; set; }
    }
}
