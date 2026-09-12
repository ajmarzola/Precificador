using Precificador.Core.Empresas;
using Precificador.Web.Empresas;

namespace Precificador.Tests.Integration.Web;

public sealed class DataOperacionalEmpresaTests
{
    [Fact]
    public void CA08_CA09_Mesmo_instante_UTC_respeita_timezone_da_empresa()
    {
        var timeProvider = new TimeProviderFixo(DateTimeOffset.Parse("2026-09-12T02:30:00Z"));
        var contexto = new ContextoEmpresa(Empresa.TimeZoneIdPadrao);
        var dataOperacional = new DataOperacionalEmpresa(contexto, timeProvider);

        Assert.Equal(new DateOnly(2026, 9, 11), dataOperacional.Hoje);

        contexto.TimeZoneId = "UTC";

        Assert.Equal(new DateOnly(2026, 9, 12), dataOperacional.Hoje);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "UTC")]
    public void CA10_Sem_contexto_ativo_data_operacional_falha_sem_fallback(int? empresaId, string? timeZoneId)
    {
        var dataOperacional = new DataOperacionalEmpresa(
            new ContextoEmpresa(empresaId, timeZoneId),
            new TimeProviderFixo(DateTimeOffset.Parse("2026-09-12T02:30:00Z")));

        Assert.Throws<InvalidOperationException>(() => dataOperacional.Hoje);
    }

    private sealed class ContextoEmpresa(int? empresaId, string? timeZoneId) : IEmpresaContext
    {
        public ContextoEmpresa(string timeZoneId)
            : this(1, timeZoneId)
        {
        }

        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => EmpresaId ?? -1;
        public string? TimeZoneId { get; set; } = timeZoneId;
    }

    private sealed class TimeProviderFixo(DateTimeOffset agoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agoraUtc;
    }
}
