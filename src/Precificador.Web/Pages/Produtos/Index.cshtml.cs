using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Produtos;

public sealed class IndexModel(PrecificadorDbContext context, ResumoPrecificacaoProdutosAtual resumoPrecificacao) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public IReadOnlyList<ProdutoListagem> Produtos { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Categorias { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Colecoes { get; private set; } = [];
    public string? Colecao { get; private set; }
    public string? Consulta { get; private set; }
    public string? Categoria { get; private set; }
    public string? Filtro { get; private set; }
    public bool FiltroInvalido { get; private set; }
    public bool TemFiltros => Colecao is not null || Consulta is not null || Categoria is not null || Filtro is not null || FiltroInvalido;
    public bool TemPesquisa => Consulta is not null;

    public async Task OnGetAsync(string? q, string? categoria = null, string? filtro = null, string? colecao = null)
    {
        Colecao = string.IsNullOrWhiteSpace(colecao) ? null : colecao.Trim();
        var colecoes = await context.ColecoesProdutos.AsNoTracking()
            .OrderByDescending(x => x.DataLancamento).ThenBy(x => x.NomeNormalizado).ToListAsync();
        Colecoes = [new SelectListItem("Todas as coleções", "", Colecao is null),
            new SelectListItem("Sem coleção", "sem-colecao", Colecao == "sem-colecao"),
            .. colecoes.Select(x => new SelectListItem(x.Nome, x.Id.ToString(), Colecao == x.Id.ToString()))];
        Consulta = NormalizarConsulta(q);
        Categoria = NormalizarCategoria(categoria);
        var filtroMargem = NormalizarFiltro(filtro);
        Filtro = filtroMargem switch
        {
            FiltroMargem.AbaixoDaMargem => "abaixo-da-margem",
            FiltroMargem.PrecificacaoIncompleta => "precificacao-incompleta",
            _ => null
        };
        FiltroInvalido = filtroMargem == FiltroMargem.Invalido;
        var categoriasCarregadas = await context.CategoriasProdutos.AsNoTracking()
            .OrderBy(item => item.NomeNormalizado)
            .Select(item => new { item.Id, item.Nome, item.Ativo })
            .ToListAsync();
        Categorias = [
            new SelectListItem("Todas as categorias", string.Empty, Categoria is null),
            new SelectListItem("Sem categoria", "sem-categoria", Categoria == "sem-categoria"),
            .. categoriasCarregadas.Select(item => new SelectListItem(
                item.Nome + (item.Ativo ? string.Empty : " (inativa)"),
                item.Id.ToString(),
                Categoria == item.Id.ToString()))
        ];
        if (FiltroInvalido)
        {
            Produtos = [];
            return;
        }
        var consulta = context.Produtos.AsNoTracking();

        if (Colecao == "sem-colecao")
            consulta = consulta.Where(x => !context.ProdutosColecoes.Any(v => v.ProdutoId == x.Id));
        else if (int.TryParse(Colecao, out var colecaoId) && colecoes.Any(x => x.Id == colecaoId))
            consulta = consulta.Where(x => context.ProdutosColecoes.Any(v => v.ProdutoId == x.Id && v.ColecaoProdutoId == colecaoId));
        else if (Colecao is not null)
            consulta = consulta.Where(_ => false);

        if (Consulta is not null)
        {
            consulta = consulta.Where(produto => produto.NomeNormalizado.Contains(Consulta));
        }

        if (Categoria == "sem-categoria")
        {
            consulta = consulta.Where(produto => produto.CategoriaProdutoId == null);
        }
        else if (int.TryParse(Categoria, out var categoriaId) && categoriaId > 0)
        {
            consulta = consulta.Where(produto => produto.CategoriaProdutoId == categoriaId);
        }
        else if (Categoria is not null)
        {
            consulta = consulta.Where(_ => false);
        }
        if (filtroMargem is FiltroMargem.AbaixoDaMargem or FiltroMargem.PrecificacaoIncompleta)
        {
            consulta = consulta.Where(produto => produto.Ativo);
        }

        var produtos = await consulta
            .OrderBy(produto => produto.NomeNormalizado)
            .Select(produto => new ProdutoListagem(
                produto.Id,
                produto.Nome,
                produto.CategoriaProdutoId == null
                    ? null
                    : context.CategoriasProdutos.Where(categoria => categoria.Id == produto.CategoriaProdutoId).Select(categoria => categoria.Nome).FirstOrDefault(),
                produto.MargemAlvo,
                produto.Ativo,
                null,
                null,
                null,
                SituacaoMargemProduto.Incompleto,
                false,
                Array.Empty<MotivoPrecificacaoIncompleta>()))
            .ToListAsync();
        var resumos = await resumoPrecificacao.CalcularAsync(produtos.Select(produto => produto.Id).ToArray());
        Produtos = produtos.Select(produto => resumos.TryGetValue(produto.Id, out var resumo)
            ? produto with { CustoUnitarioProduto = resumo.CustoUnitarioProduto, PrecoPrateleiraAtual = resumo.PrecoPrateleiraAtual, MargemAtual = resumo.MargemAtual, SituacaoMargem = resumo.SituacaoMargem, PrecificacaoCompleta = resumo.PrecificacaoCompleta, MotivosPrecificacaoIncompleta = resumo.MotivosPrecificacaoIncompleta }
            : produto).ToList();
        if (filtroMargem == FiltroMargem.AbaixoDaMargem)
        {
            Produtos = Produtos.Where(produto => produto.SituacaoMargem == SituacaoMargemProduto.AbaixoDaMargem).ToList();
        }
        else if (filtroMargem == FiltroMargem.PrecificacaoIncompleta)
        {
            Produtos = Produtos.Where(produto => !produto.PrecificacaoCompleta).ToList();
        }
    }

    public static string? NormalizarConsulta(string? consulta)
    {
        var normalizada = Regex.Replace(consulta?.Trim() ?? string.Empty, @"\s+", " ");
        return string.IsNullOrEmpty(normalizada) ? null : normalizada.ToUpperInvariant();
    }

    private static string? NormalizarCategoria(string? categoria) =>
        string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();

    private static FiltroMargem NormalizarFiltro(string? filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro)) return FiltroMargem.Todos;
        return string.Equals(filtro.Trim(), "abaixo-da-margem", StringComparison.OrdinalIgnoreCase)
            ? FiltroMargem.AbaixoDaMargem
            : string.Equals(filtro.Trim(), "precificacao-incompleta", StringComparison.OrdinalIgnoreCase)
                ? FiltroMargem.PrecificacaoIncompleta
                : FiltroMargem.Invalido;
    }

    public sealed record ProdutoListagem(int Id, string Nome, string? Categoria, decimal MargemAlvo, bool Ativo,
        decimal? CustoUnitarioProduto, decimal? PrecoPrateleiraAtual, decimal? MargemAtual,
        SituacaoMargemProduto SituacaoMargem = SituacaoMargemProduto.Incompleto,
        bool PrecificacaoCompleta = false,
        IReadOnlyList<MotivoPrecificacaoIncompleta>? MotivosPrecificacaoIncompleta = null);

    private enum FiltroMargem { Todos, AbaixoDaMargem, PrecificacaoIncompleta, Invalido }
}
