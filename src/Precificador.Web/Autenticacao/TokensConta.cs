using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Precificador.Infrastructure.Autenticacao;

namespace Precificador.Web.Autenticacao;

public static class TokensConta
{
    public const string Ativacao = "ContaAtivacao";
    public const string PurposeAtivacao = "Precificador.AtivarConta";
    public const string Recuperacao = "ContaRecuperacaoSenha";
    public static string Codificar(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    public static string? Decodificar(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        try { return new UTF8Encoding(false, true).GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (Exception exception) when (exception is FormatException or ArgumentException) { return null; }
    }
}

public sealed class OpcoesTokenAtivacao : DataProtectionTokenProviderOptions
{
    public OpcoesTokenAtivacao() { Name = TokensConta.Ativacao; TokenLifespan = TimeSpan.FromHours(48); }
}
public sealed class OpcoesTokenRecuperacao : DataProtectionTokenProviderOptions
{
    public OpcoesTokenRecuperacao() { Name = TokensConta.Recuperacao; TokenLifespan = TimeSpan.FromHours(1); }
}
public sealed class TokenAtivacaoProvider(IDataProtectionProvider protection, IOptions<OpcoesTokenAtivacao> options,
    ILogger<DataProtectorTokenProvider<UsuarioAplicacao>> logger)
    : DataProtectorTokenProvider<UsuarioAplicacao>(protection, options, logger);
public sealed class TokenRecuperacaoProvider(IDataProtectionProvider protection, IOptions<OpcoesTokenRecuperacao> options,
    ILogger<DataProtectorTokenProvider<UsuarioAplicacao>> logger)
    : DataProtectorTokenProvider<UsuarioAplicacao>(protection, options, logger);
