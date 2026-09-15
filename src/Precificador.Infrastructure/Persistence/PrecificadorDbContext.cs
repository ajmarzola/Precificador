using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Autenticacao;

namespace Precificador.Infrastructure.Persistence;

public sealed class PrecificadorDbContext(
    DbContextOptions<PrecificadorDbContext> options,
    IEmpresaContext empresaContext)
    : IdentityDbContext<UsuarioAplicacao>(options)
{
    private readonly IEmpresaContext empresaContext = empresaContext;

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<ConfiguracaoPrecificacaoEmpresa> ConfiguracoesPrecificacaoEmpresas => Set<ConfiguracaoPrecificacaoEmpresa>();
    public DbSet<FichaTecnica> FichasTecnicas => Set<FichaTecnica>();
    public DbSet<ItemFichaTecnica> ItensFichaTecnica => Set<ItemFichaTecnica>();
    public DbSet<UsoEquipamentoFicha> UsosEquipamentosFicha => Set<UsoEquipamentoFicha>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<PrecoInsumo> PrecosInsumos => Set<PrecoInsumo>();
    public DbSet<RegistroPrecoProduto> RegistrosPrecosProdutos => Set<RegistroPrecoProduto>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<UsuarioEmpresa> UsuariosEmpresas => Set<UsuarioEmpresa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrecificadorDbContext).Assembly);
        modelBuilder.Entity<ConfiguracaoPrecificacaoEmpresa>().HasQueryFilter(configuracao =>
            configuracao.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<FichaTecnica>().HasQueryFilter(ficha =>
            ficha.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<ItemFichaTecnica>().HasQueryFilter(item =>
            item.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<UsoEquipamentoFicha>().HasQueryFilter(uso =>
            uso.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<Insumo>().HasQueryFilter(insumo =>
            insumo.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<PrecoInsumo>().HasQueryFilter(preco =>
            preco.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<RegistroPrecoProduto>().HasQueryFilter(registro =>
            registro.EmpresaId == empresaContext.EmpresaIdOuSentinela);
        modelBuilder.Entity<Produto>().HasQueryFilter(produto =>
            produto.EmpresaId == empresaContext.EmpresaIdOuSentinela);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarIsolamentoEmpresa();
        ValidarReferenciasDosItensFichaTecnica();
        ValidarReferenciasDosUsosEquipamentosFicha();
        ValidarReferenciaProdutoDasFichas();
        ValidarReferenciaInsumoDosPrecos();
        ValidarReferenciaProdutoDosRegistrosPrecos();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AplicarIsolamentoEmpresa();
        await ValidarReferenciasDosItensFichaTecnicaAsync(cancellationToken);
        await ValidarReferenciasDosUsosEquipamentosFichaAsync(cancellationToken);
        await ValidarReferenciaProdutoDasFichasAsync(cancellationToken);
        await ValidarReferenciaInsumoDosPrecosAsync(cancellationToken);
        await ValidarReferenciaProdutoDosRegistrosPrecosAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AplicarIsolamentoEmpresa()
    {
        var alteracoes = ChangeTracker.Entries<IEntidadeEmpresa>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var alteracao in alteracoes)
        {
            var empresaId = empresaContext.EmpresaId;
            if (!empresaId.HasValue)
            {
                throw new InvalidOperationException("Uma empresa ativa é necessária para alterar dados da empresa.");
            }

            if (alteracao.Entity.EmpresaId != empresaId.Value)
            {
                throw new InvalidOperationException("Não é permitido alterar dados de outra empresa.");
            }
        }
    }

    private void ValidarReferenciaInsumoDosPrecos()
    {
        foreach (var preco in PrecosAlterados())
        {
            var referenciaValida = Insumos.IgnoreQueryFilters()
                .Any(insumo => insumo.Id == preco.InsumoId && insumo.EmpresaId == preco.EmpresaId);

            if (!referenciaValida)
            {
                throw new InvalidOperationException("O insumo referenciado pelo preço não pertence à mesma empresa.");
            }
        }
    }

    private void ValidarReferenciaProdutoDasFichas()
    {
        foreach (var ficha in FichasAlteradas())
        {
            var referenciaValida = Produtos.IgnoreQueryFilters()
                .Any(produto => produto.Id == ficha.ProdutoId && produto.EmpresaId == ficha.EmpresaId);

            if (!referenciaValida)
            {
                throw new InvalidOperationException("O produto referenciado pela ficha técnica não pertence à mesma empresa.");
            }
        }
    }

    private void ValidarReferenciaProdutoDosRegistrosPrecos()
    {
        foreach (var registro in RegistrosPrecosAlterados())
        {
            if (!Produtos.IgnoreQueryFilters().Any(produto => produto.Id == registro.ProdutoId && produto.EmpresaId == registro.EmpresaId))
                throw new InvalidOperationException("O produto referenciado pelo registro de preco nao pertence a mesma empresa.");
        }
    }

    private void ValidarReferenciasDosItensFichaTecnica()
    {
        foreach (var item in ItensFichaTecnicaAlterados())
        {
            var fichaValida = FichasTecnicas.IgnoreQueryFilters()
                .Any(ficha => ficha.Id == item.FichaTecnicaId && ficha.EmpresaId == item.EmpresaId);
            var insumoValido = Insumos.IgnoreQueryFilters()
                .Any(insumo => insumo.Id == item.InsumoId && insumo.EmpresaId == item.EmpresaId);

            if (!fichaValida)
            {
                throw new InvalidOperationException("A ficha técnica referenciada pelo item não pertence à mesma empresa.");
            }

            if (!insumoValido)
            {
                throw new InvalidOperationException("O insumo referenciado pelo item da ficha técnica não pertence à mesma empresa.");
            }
        }
    }

    private void ValidarReferenciasDosUsosEquipamentosFicha()
    {
        foreach (var uso in UsosEquipamentosFichaAlterados())
        {
            var fichaValida = FichasTecnicas.IgnoreQueryFilters()
                .Any(ficha => ficha.Id == uso.FichaTecnicaId && ficha.EmpresaId == uso.EmpresaId);
            if (!fichaValida)
                throw new InvalidOperationException("A ficha técnica referenciada pelo uso de equipamento não pertence à mesma empresa.");
        }
    }

    private async Task ValidarReferenciaInsumoDosPrecosAsync(CancellationToken cancellationToken)
    {
        foreach (var preco in PrecosAlterados())
        {
            var referenciaValida = await Insumos.IgnoreQueryFilters()
                .AnyAsync(
                    insumo => insumo.Id == preco.InsumoId && insumo.EmpresaId == preco.EmpresaId,
                    cancellationToken);

            if (!referenciaValida)
            {
                throw new InvalidOperationException("O insumo referenciado pelo preço não pertence à mesma empresa.");
            }
        }
    }

    private async Task ValidarReferenciaProdutoDasFichasAsync(CancellationToken cancellationToken)
    {
        foreach (var ficha in FichasAlteradas())
        {
            var referenciaValida = await Produtos.IgnoreQueryFilters()
                .AnyAsync(
                    produto => produto.Id == ficha.ProdutoId && produto.EmpresaId == ficha.EmpresaId,
                    cancellationToken);

            if (!referenciaValida)
            {
                throw new InvalidOperationException("O produto referenciado pela ficha técnica não pertence à mesma empresa.");
            }
        }
    }

    private async Task ValidarReferenciaProdutoDosRegistrosPrecosAsync(CancellationToken cancellationToken)
    {
        foreach (var registro in RegistrosPrecosAlterados())
        {
            if (!await Produtos.IgnoreQueryFilters().AnyAsync(produto => produto.Id == registro.ProdutoId && produto.EmpresaId == registro.EmpresaId, cancellationToken))
                throw new InvalidOperationException("O produto referenciado pelo registro de preco nao pertence a mesma empresa.");
        }
    }

    private async Task ValidarReferenciasDosItensFichaTecnicaAsync(CancellationToken cancellationToken)
    {
        foreach (var item in ItensFichaTecnicaAlterados())
        {
            var fichaValida = await FichasTecnicas.IgnoreQueryFilters()
                .AnyAsync(
                    ficha => ficha.Id == item.FichaTecnicaId && ficha.EmpresaId == item.EmpresaId,
                    cancellationToken);
            var insumoValido = await Insumos.IgnoreQueryFilters()
                .AnyAsync(
                    insumo => insumo.Id == item.InsumoId && insumo.EmpresaId == item.EmpresaId,
                    cancellationToken);

            if (!fichaValida)
            {
                throw new InvalidOperationException("A ficha técnica referenciada pelo item não pertence à mesma empresa.");
            }

            if (!insumoValido)
            {
                throw new InvalidOperationException("O insumo referenciado pelo item da ficha técnica não pertence à mesma empresa.");
            }
        }
    }

    private async Task ValidarReferenciasDosUsosEquipamentosFichaAsync(CancellationToken cancellationToken)
    {
        foreach (var uso in UsosEquipamentosFichaAlterados())
        {
            var fichaValida = await FichasTecnicas.IgnoreQueryFilters().AnyAsync(
                ficha => ficha.Id == uso.FichaTecnicaId && ficha.EmpresaId == uso.EmpresaId, cancellationToken);
            if (!fichaValida)
                throw new InvalidOperationException("A ficha técnica referenciada pelo uso de equipamento não pertence à mesma empresa.");
        }
    }

    private IEnumerable<PrecoInsumo> PrecosAlterados() =>
        ChangeTracker.Entries<PrecoInsumo>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity);

    private IEnumerable<FichaTecnica> FichasAlteradas() =>
        ChangeTracker.Entries<FichaTecnica>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity);

    private IEnumerable<RegistroPrecoProduto> RegistrosPrecosAlterados() =>
        ChangeTracker.Entries<RegistroPrecoProduto>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity);

    private IEnumerable<ItemFichaTecnica> ItensFichaTecnicaAlterados() =>
        ChangeTracker.Entries<ItemFichaTecnica>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity);

    private IEnumerable<UsoEquipamentoFicha> UsosEquipamentosFichaAlterados() =>
        ChangeTracker.Entries<UsoEquipamentoFicha>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity);
}
