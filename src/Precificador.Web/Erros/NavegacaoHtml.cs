namespace Precificador.Web.Erros;

internal static class NavegacaoHtml
{
    public static bool AceitaHtml(HttpContext context)
    {
        // Assets não são destinos de navegação, mesmo com Accept de navegador.
        var caminho = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Path
            ?? context.Request.Path.Value;
        var extensao = Path.GetExtension(caminho ?? string.Empty).ToLowerInvariant();
        if (extensao is ".css" or ".js" or ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".avif" or ".svg" or ".ico" or ".woff" or ".woff2" or ".ttf" or ".eot" or ".map")
            return false;

        return context.Request.GetTypedHeaders().Accept?.Any(media =>
            string.Equals(media.MediaType.Value, "text/html", StringComparison.OrdinalIgnoreCase)
            && (media.Quality ?? 1) > 0) == true;
    }
}
