using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Tests.Application.Services
{
    public class ProdutoServiceTests
    {
        private readonly Mock<IProdutoRepository> _repositoryMock;
        private readonly ProdutoService _service;

        public ProdutoServiceTests()
        {
            _repositoryMock = new Mock<IProdutoRepository>();
            _service = new ProdutoService(_repositoryMock.Object);
        }

        [Fact]
        public void ConvertToEntity_DeveConverterModelParaEntity()
        {
            var model = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Produto Teste",
                ColecaoId = Guid.NewGuid(),
                Margem = 0.25m,
                DataCalculoPreco = new DateTime(2024, 6, 1),
                PrecoCusto = 100m
            };

            var entity = _service.InvokeConvertToEntity(model);

            Assert.NotNull(entity);
            Assert.Equal(model.Id, entity!.Id);
            Assert.Equal(model.Nome, entity.Nome);
            Assert.Equal(model.Margem, entity.Margem);
            Assert.Equal(model.DataCalculoPreco, entity.DataCalculoPreco);
            Assert.Equal(model.PrecoCusto, entity.PrecoCusto);
        }

        [Fact]
        public void ConvertToModel_DeveConverterEntityParaModel()
        {
            var entity = new Domain.Entities.Produto("Produto Entity", 0.30m, new DateTime(2024, 5, 10), 200m, 400m, 500m, 450m);

            var model = _service.InvokeConvertToModel(entity);

            Assert.NotNull(model);
            Assert.Equal(entity.Id, model!.Id);
            Assert.Equal(entity.Nome, model.Nome);
            Assert.Equal(entity.Margem, model.Margem);
            Assert.Equal(entity.DataCalculoPreco, model.DataCalculoPreco);
            Assert.Equal(entity.PrecoCusto, model.PrecoCusto);
            Assert.Equal(entity.PrecoFinal, model.PrecoFinal);
            Assert.Equal(entity.PrecoCustoX3, model.PrecoCustoX3);
            Assert.Equal(entity.PrecoCustoX35, model.PrecoCustoX35);
            Assert.Equal(entity.PrecoCustoX4, model.PrecoCustoX4);
        }

        [Fact]
        public void UpdateEntityFromModel_DeveAtualizarEntityComDadosDoModel()
        {
            var entity = new Domain.Entities.Produto("Antigo", 0.10m, new DateTime(2020, 1, 1), 50m, 100m, 200m, 150m);

            var model = new Produto
            {
                Nome = "Novo",
                Margem = 0.50m,
                DataCalculoPreco = new DateTime(2025, 2, 2),
                PrecoCusto = 300m
            };

            _service.InvokeUpdateEntityFromModel(entity, model);

            Assert.Equal(model.Nome, entity.Nome);
            Assert.Equal(model.Margem, entity.Margem);
            Assert.Equal(model.DataCalculoPreco, entity.DataCalculoPreco);
            Assert.Equal(model.PrecoCusto, entity.PrecoCusto);
        }
    }

    public static class ProdutoServiceTestExtensions
    {
        public static Domain.Entities.Produto? InvokeConvertToEntity(this ProdutoService service, Produto model)
        {
            var method = typeof(ProdutoService).GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'ConvertToEntity' not found.");
            var result = method.Invoke(service, [model]);
            return result as Domain.Entities.Produto;
        }

        public static Produto? InvokeConvertToModel(this ProdutoService service, Domain.Entities.Produto entity)
        {
            var method = typeof(ProdutoService).GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'ConvertToModel' not found.");
            var result = method.Invoke(service, [entity]);
            return result as Produto;
        }

        public static void InvokeUpdateEntityFromModel(this ProdutoService service, Domain.Entities.Produto entity, Produto model)
        {
            var method = typeof(ProdutoService).GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'UpdateEntityFromModel' not found.");
            method.Invoke(service, [entity, model]);
        }

        public static Task<IEnumerable<Domain.Entities.Produto>> InvokeGetEntitiesByFilterAsync(this ProdutoService service, NomeFilter filter)
        {
            var method = typeof(ProdutoService).GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' not found.");
            var result = method.Invoke(service, [filter]);
            return result as Task<IEnumerable<Domain.Entities.Produto>> ?? Task.FromResult(Enumerable.Empty<Domain.Entities.Produto>());
        }
    }
}