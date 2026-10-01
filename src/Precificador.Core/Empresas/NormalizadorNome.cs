using System.Text.RegularExpressions;

namespace Precificador.Core.Empresas;

public static class NormalizadorNome
{
    public static string Normalizar(string? nome) => Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");
}
