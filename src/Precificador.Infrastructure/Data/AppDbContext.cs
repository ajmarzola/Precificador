using Microsoft.EntityFrameworkCore;
using Precificador.Domain.Entities;

namespace Precificador.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<MateriaPrima> MateriasPrimas { get; set; }
        public DbSet<Colecao> Colecoes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<ProdutoMateriaPrima> ProdutoMateriaPrimas { get; set; }
        public DbSet<PesquisaPreco> PesquisasPrecos { get; set; }
        public DbSet<UnidadeMedida> UnidadesMedida { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnMateriaPrimaCreating(modelBuilder);
            OnProdutoMateriaPrimaCreating(modelBuilder);
            OnPesquisaPrecoCreating(modelBuilder);
            OnProdutoCreating(modelBuilder);
            OnGrupoCreating(modelBuilder);
            OnColecaoCreating(modelBuilder);
            OnUnidadeMedidaCreating(modelBuilder);
        }

        private static void OnUnidadeMedidaCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnidadeMedida>()
                .Property(um => um.Nome)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<UnidadeMedida>()
                .Property(um => um.Abrebiacao)
                .HasMaxLength(4)
                .IsRequired();
        }

        private static void OnColecaoCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Colecao>()
                .Property(c => c.Nome)
                .HasMaxLength(100)
                .IsRequired();
        }

        private static void OnGrupoCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Grupo>()
                .Property(g => g.Nome)
                .HasMaxLength(100)
                .IsRequired();
        }

        private static void OnProdutoCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Colecao)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.ColecaoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Produto>()
                .Property(p => p.Nome)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Produto>()
                .Property(p => p.Margem)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Produto>()
                .Property(p => p.PrecoCusto)
                .HasPrecision(18, 2);
        }

        private static void OnPesquisaPrecoCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PesquisaPreco>()
                .HasOne(pp => pp.Produto)
                .WithMany(p => p.Pesquisas)
                .HasForeignKey(pp => pp.ProdutoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PesquisaPreco>()
                .Property(pp => pp.Local)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<PesquisaPreco>()
                .Property(p => p.Valor)
                .HasPrecision(18, 2);
        }

        private static void OnProdutoMateriaPrimaCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProdutoMateriaPrima>()
                .HasOne(pmp => pmp.MateriaPrima)
                .WithMany(mp => mp.Produtos)
                .HasForeignKey(pmp => pmp.MateriaPrimaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProdutoMateriaPrima>()
                .HasOne(pmp => pmp.Produto)
                .WithMany(p => p.MateriasPrimas)
                .HasForeignKey(pmp => pmp.ProdutoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProdutoMateriaPrima>()
                .Property(p => p.Quantidade)
                .HasPrecision(18, 2);
        }

        private static void OnMateriaPrimaCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MateriaPrima>()
                .HasOne(mp => mp.Grupo)
                .WithMany(g => g.MateriasPrimas)
                .HasForeignKey(mp => mp.GrupoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MateriaPrima>()
                .HasOne(mp => mp.UnidadeMedida)
                .WithMany()
                .HasForeignKey(mp => mp.UnidadeMedidaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MateriaPrima>()
                .Property(mp => mp.Nome)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<MateriaPrima>()
                .Property(p => p.QtdPacote)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MateriaPrima>()
                .Property(p => p.VlrPacote)
                .HasPrecision(18, 2);
        }
    }
}