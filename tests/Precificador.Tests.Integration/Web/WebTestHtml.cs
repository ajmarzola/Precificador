using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Precificador.Tests.Integration.Web;

internal static class WebTestHtml
{
    private static readonly Regex AntiforgeryRegex = new("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"", RegexOptions.Compiled);

    public static string ExtrairTokenAntiforgery(string html)
    {
        var match = AntiforgeryRegex.Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("Token antiforgery __RequestVerificationToken nao encontrado no HTML da pagina.");
        }

        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static async Task<string> ObterTokenAntiforgeryAsync(HttpClient client, string caminho)
    {
        var response = await client.GetAsync(caminho);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, html);
        return ExtrairTokenAntiforgery(html);
    }

    public static async Task<string> LerHtmlDecodificadoAsync(HttpResponseMessage response)
    {
        var html = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        return WebUtility.HtmlDecode(html);
    }
}
