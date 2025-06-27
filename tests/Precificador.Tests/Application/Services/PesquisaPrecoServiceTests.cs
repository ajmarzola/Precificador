using Moq;
using Precificador.Application.Model;
using Precificador.Application.Services;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Tests.Application.Services
{
    public class PesquisaPrecoServiceTests
    {
        private readonly Mock<IPesquisaPrecoRepository> _repositoryMock;
        private readonly PesquisaPrecoService _service;

        public PesquisaPrecoServiceTests()
        {
            _repositoryMock = new Mock<IPesquisaPrecoRepository>();
            _service = new PesquisaPrecoService(_repositoryMock.Object);
        }

        [Fact]
        public void ConvertToEntity_DeveConverterModelParaEntity()
        {
            var model = new PesquisaPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                Local = "Supermercado X",
                Valor = 15.5m,
                DataPesquisa = new DateTime(2024, 6, 1)
            };

            var entity = _service.InvokeConvertToEntity(model);

            Assert.NotNull(entity);
            Assert.Equal(model.Id, entity!.Id);
            Assert.Equal(model.ProdutoId, entity.ProdutoId);
            Assert.Equal(model.Local, entity.Local);
            Assert.Equal(model.Valor, entity.Valor);
            Assert.Equal(model.DataPesquisa, entity.DataPesquisa);
        }

        [Fact]
        public void ConvertToModel_DeveConverterEntityParaModel()
        {
            var entity = new Domain.Entities.PesquisaPreco(Guid.NewGuid(), "Mercado Y", 20.0m);

            var model = _service.InvokeConvertToModel(entity);

            Assert.NotNull(model);
            Assert.Equal(entity.Id, model!.Id);
            Assert.Equal(entity.ProdutoId, model.ProdutoId);
            Assert.Equal(entity.Local, model.Local);
            Assert.Equal(entity.Valor, model.Valor);
            Assert.Equal(entity.DataPesquisa, model.DataPesquisa);
        }

        [Fact]
        public void UpdateEntityFromModel_DeveAtualizarEntityComDadosDoModel()
        {
            var entity = new Domain.Entities.PesquisaPreco(Guid.NewGuid(), "Antigo", 10.0m);

            var model = new PesquisaPreco
            {
                ProdutoId = Guid.NewGuid(),
                Local = "Novo Local",
                Valor = 99.9m,
                DataPesquisa = new DateTime(2024, 6, 10)
            };

            _service.InvokeUpdateEntityFromModel(entity, model);

            Assert.Equal(model.ProdutoId, entity.ProdutoId);
            Assert.Equal(model.Local, entity.Local);
            Assert.Equal(model.Valor, entity.Valor);
            Assert.Equal(model.DataPesquisa, entity.DataPesquisa);
        }
    }

    public static class PesquisaPrecoServiceTestExtensions
    {
        public static Domain.Entities.PesquisaPreco? InvokeConvertToEntity(this PesquisaPrecoService service, PesquisaPreco model)
        {
            var methodInfo = typeof(PesquisaPrecoService).GetMethod("ConvertToEntity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return methodInfo == null
                ? throw new InvalidOperationException("Method 'ConvertToEntity' not found.")
                : (Domain.Entities.PesquisaPreco?)methodInfo.Invoke(service, [model]);
        }

        public static PesquisaPreco? InvokeConvertToModel(this PesquisaPrecoService service, Domain.Entities.PesquisaPreco entity)
        {
            var methodInfo = typeof(PesquisaPrecoService).GetMethod("ConvertToModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return methodInfo == null
                ? throw new InvalidOperationException("Method 'ConvertToModel' not found.")
                : (PesquisaPreco?)methodInfo.Invoke(service, [entity]);
        }

        public static void InvokeUpdateEntityFromModel(this PesquisaPrecoService service, Domain.Entities.PesquisaPreco entity, PesquisaPreco model)
        {
            var methodInfo = typeof(PesquisaPrecoService).GetMethod("UpdateEntityFromModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Method 'UpdateEntityFromModel' not found.");
            methodInfo.Invoke(service, [entity, model]);
        }

        public static Task<IEnumerable<Domain.Entities.PesquisaPreco>> InvokeGetEntitiesByFilterAsync(this PesquisaPrecoService service, PesquisaPrecoFilter filter)
        {
            var methodInfo = typeof(PesquisaPrecoService).GetMethod("GetEntitiesByFilterAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return methodInfo == null
                ? throw new InvalidOperationException("Method 'GetEntitiesByFilterAsync' not found.")
                : (Task<IEnumerable<Domain.Entities.PesquisaPreco>>)methodInfo.Invoke(service, [filter])!;
        }
    }
}