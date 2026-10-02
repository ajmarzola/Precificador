using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Colecoes;

internal static class ColecaoProdutoFormulario
{
    public const string MensagemDuplicidade = "Já existe uma coleção com esse nome e data de lançamento.";
    public const string MensagemCategoria = "Selecione categorias de produto válidas.";

    public static bool TentarObterDados(ModelStateDictionary estado, ColecaoProdutoInputModel input, int empresaId, out ColecaoProduto? colecao)
    {
        colecao = null;
        if (input.DataLancamento is null)
        {
            estado.AddModelError("Input.DataLancamento", "A data de lançamento é obrigatória.");
            return false;
        }
        try
        {
            colecao = ColecaoProduto.Criar(empresaId, input.Nome!, input.DataLancamento.Value, input.DataFinalizacao);
            return estado.IsValid;
        }
        catch (ArgumentException erro)
        {
            var campo = erro.ParamName switch
            {
                "dataLancamento" => "Input.DataLancamento",
                "dataFinalizacao" => "Input.DataFinalizacao",
                _ => "Input.Nome"
            };
            estado.AddModelError(campo, erro.Message);
            return false;
        }
    }

    public static async Task<bool> CategoriasValidasAsync(PrecificadorDbContext contexto, ModelStateDictionary estado, IReadOnlyCollection<int> ids, IReadOnlyCollection<int> existentes)
    {
        var distintos = ids.Distinct().ToArray();
        if (distintos.Any(id => id <= 0))
        {
            estado.AddModelError("Input.CategoriaProdutoIds", MensagemCategoria);
            return false;
        }
        var validos = await contexto.CategoriasProdutos.AsNoTracking()
            .Where(c => distintos.Contains(c.Id) && (c.Ativo || existentes.Contains(c.Id)))
            .Select(c => c.Id).ToListAsync();
        if (validos.Count != distintos.Length)
        {
            estado.AddModelError("Input.CategoriaProdutoIds", MensagemCategoria);
            return false;
        }
        return true;
    }

    public static bool Duplicidade(DbUpdateException erro) =>
        erro.InnerException is SqlException sql && sql.Number is 2601 or 2627 &&
        sql.Message.Contains("IX_ColecoesProdutos_EmpresaId_NomeNormalizado_DataLancamento", StringComparison.Ordinal);
}
