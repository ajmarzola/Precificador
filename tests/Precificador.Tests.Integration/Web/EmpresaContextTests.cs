using Microsoft.AspNetCore.Http;
using Precificador.Web.Empresas;

namespace Precificador.Tests.Integration.Web;

public sealed class EmpresaContextTests
{
    [Fact]
    public void CA01_Limpar_remove_id_nome_e_chaves_da_empresa_ativa()
    {
        var session = new SessaoEmMemoria();
        var httpContext = new DefaultHttpContext { Session = session };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var empresaContext = new EmpresaContext(httpContextAccessor);

        empresaContext.Definir(1, "Empresa teste");

        Assert.Equal(1, empresaContext.EmpresaId);
        Assert.Equal("Empresa teste", empresaContext.Nome);

        empresaContext.Limpar();

        Assert.Null(empresaContext.EmpresaId);
        Assert.Equal(-1, empresaContext.EmpresaIdOuSentinela);
        Assert.Null(empresaContext.Nome);
        Assert.False(session.TryGetValue(EmpresaContext.ChaveSession, out _));
        Assert.False(session.TryGetValue(EmpresaContext.ChaveNomeSession, out _));
    }

    private sealed class SessaoEmMemoria : ISession
    {
        private readonly Dictionary<string, byte[]> valores = [];

        public bool IsAvailable => true;
        public string Id => "sessao-em-memoria";
        public IEnumerable<string> Keys => valores.Keys;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Clear() => valores.Clear();
        public void Remove(string key) => valores.Remove(key);
        public void Set(string key, byte[] value) => valores[key] = value;
        public bool TryGetValue(string key, out byte[] value) => valores.TryGetValue(key, out value!);
    }
}
